using System.Collections.Concurrent;

namespace SilkWheel.Services;

public sealed class ScrollEngine : IDisposable
{
    private const int FrameMs = 7;
    private const int MaxCoalescedTailMs = 120;
    private const double TailSnapDelta = 1.25;
    private const int MaxAnimationsPerAxis = 18;

    private readonly AppSettings _settings;
    private readonly object _gate = new();
    private readonly System.Threading.Timer _timer;
    private readonly List<ScrollAnimation> _vertical = new();
    private readonly List<ScrollAnimation> _horizontal = new();
    private readonly ConcurrentQueue<PendingInjection> _pendingInjections = new();
    private double _verticalCarry;
    private double _horizontalCarry;
    private bool _verticalImmediateMode;
    private bool _horizontalImmediateMode;
    private long _verticalTailGeneration;
    private long _horizontalTailGeneration;
    private DateTime _lastInputUtc = DateTime.MinValue;
    private int _lastDirection;
    private double _acceleration = 1.0;
    private int _isTicking;
    private int _isDrainingInjections;
    private int _disposed;

    public ScrollEngine(AppSettings settings)
    {
        _settings = settings;
        _timer = new System.Threading.Timer(Tick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Enqueue(int wheelDelta, bool horizontal)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var direction = Math.Sign(wheelDelta);
        if (direction == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var injectionQueued = false;
        lock (_gate)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            if (direction != _lastDirection)
            {
                CancelAllAnimationTails();
                _acceleration = 1.0;
            }
            else if ((now - _lastInputUtc).TotalMilliseconds <= _settings.AccelerationDeltaMs)
            {
                _acceleration = Math.Min(_settings.AccelerationMax, _acceleration + 0.75);
            }
            else
            {
                _acceleration = 1.0;
            }

            _lastDirection = direction;
            _lastInputUtc = now;

            var sign = _settings.ReverseDirection ? -direction : direction;
            var amount = sign * _settings.StepSize * _settings.LinesToScroll * _acceleration;
            var duration = GetDurationMs();
            var list = horizontal ? _horizontal : _vertical;

            if (duration <= 0)
            {
                // A zero duration is an immediate physical wheel event. Cancel
                // the selected axis's old tail only when entering immediate
                // mode. Consecutive immediate events retain fractional carry.
                EnterImmediateMode(horizontal);

                var immediateInject = horizontal
                    ? Quantize(amount, ref _horizontalCarry)
                    : Quantize(amount, ref _verticalCarry);

                if (immediateInject != 0)
                {
                    _pendingInjections.Enqueue(PendingInjection.Immediate(immediateInject, horizontal));
                    injectionQueued = true;
                }

                if (_vertical.Count == 0 && _horizontal.Count == 0)
                {
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }
            else
            {
                SetImmediateMode(horizontal, enabled: false);
                list.Add(new ScrollAnimation(now, duration, amount));
                CoalesceOldAnimations(list, now, duration);
                _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(FrameMs));
            }
        }

        if (injectionQueued)
        {
            EnsureInjectionDrain();
        }
    }

    private void Tick(object? state)
    {
        if (Volatile.Read(ref _disposed) != 0 || Interlocked.Exchange(ref _isTicking, 1) == 1)
        {
            return;
        }

        var injectionQueued = false;
        try
        {
            var now = DateTime.UtcNow;

            lock (_gate)
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    return;
                }

                // Settings are shared live with the UI. If animation is turned
                // off while a tail is already running, cancel it on the very
                // next timer tick instead of letting the old duration finish.
                if (_settings.AnimationTimeMs <= 0)
                {
                    EnterImmediateMode(horizontal: false);
                    EnterImmediateMode(horizontal: true);
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    return;
                }

                var verticalInject = Quantize(StepAxis(_vertical, now), ref _verticalCarry);
                var horizontalInject = Quantize(StepAxis(_horizontal, now), ref _horizontalCarry);

                if (verticalInject != 0)
                {
                    _pendingInjections.Enqueue(PendingInjection.Animation(
                        verticalInject,
                        horizontal: false,
                        Volatile.Read(ref _verticalTailGeneration)));
                    injectionQueued = true;
                }

                if (horizontalInject != 0)
                {
                    _pendingInjections.Enqueue(PendingInjection.Animation(
                        horizontalInject,
                        horizontal: true,
                        Volatile.Read(ref _horizontalTailGeneration)));
                    injectionQueued = true;
                }

                if (_vertical.Count == 0 && _horizontal.Count == 0)
                {
                    _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isTicking, 0);
        }

        if (injectionQueued)
        {
            EnsureInjectionDrain();
        }
    }

    private void CancelAllAnimationTails()
    {
        CancelAnimationTail(horizontal: false);
        CancelAnimationTail(horizontal: true);
    }

    private void CancelAnimationTail(bool horizontal)
    {
        if (horizontal)
        {
            _horizontal.Clear();
            _horizontalCarry = 0.0;
            Interlocked.Increment(ref _horizontalTailGeneration);
        }
        else
        {
            _vertical.Clear();
            _verticalCarry = 0.0;
            Interlocked.Increment(ref _verticalTailGeneration);
        }
    }

    private bool IsImmediateMode(bool horizontal)
    {
        return horizontal ? _horizontalImmediateMode : _verticalImmediateMode;
    }

    private void EnterImmediateMode(bool horizontal)
    {
        if (IsImmediateMode(horizontal))
        {
            return;
        }

        CancelAnimationTail(horizontal);
        SetImmediateMode(horizontal, enabled: true);
    }

    private void SetImmediateMode(bool horizontal, bool enabled)
    {
        if (horizontal)
        {
            _horizontalImmediateMode = enabled;
        }
        else
        {
            _verticalImmediateMode = enabled;
        }
    }

    private void EnsureInjectionDrain()
    {
        if (Interlocked.CompareExchange(ref _isDrainingInjections, 1, 0) != 0)
        {
            return;
        }

        while (true)
        {
            while (_pendingInjections.TryDequeue(out var injection))
            {
                if (Volatile.Read(ref _disposed) != 0)
                {
                    continue;
                }

                if (injection.IsCancelable && injection.TailGeneration != GetTailGeneration(injection.Horizontal))
                {
                    continue;
                }

                // Generation validation is the commit point. From here this
                // single consumer sends the item before every later queue item.
                // No engine or hook-visible lock is held across SendInput.
                InputInjector.SendWheel(injection.Delta, injection.Horizontal);
            }

            Interlocked.Exchange(ref _isDrainingInjections, 0);

            // A producer can enqueue while the drain flag is still set. Recheck
            // after releasing ownership, then either reacquire it or leave the
            // new owner to finish the queue.
            if (_pendingInjections.IsEmpty
                || Interlocked.CompareExchange(ref _isDrainingInjections, 1, 0) != 0)
            {
                return;
            }
        }
    }

    private long GetTailGeneration(bool horizontal)
    {
        return horizontal
            ? Volatile.Read(ref _horizontalTailGeneration)
            : Volatile.Read(ref _verticalTailGeneration);
    }

    private int GetDurationMs()
    {
        if (_settings.AnimationTimeMs <= 0)
        {
            return 0;
        }

        var duration = Math.Clamp(
            _settings.AnimationTimeMs,
            AppSettings.MinAnimationTimeMs,
            AppSettings.MaxAnimationTimeMs);

        // Continuous wheel input should keep the previous version's quick feel,
        // while still using a finite curve so the tail does not wobble.
        if (_acceleration > 1.0)
        {
            duration = (int)Math.Max(
                AppSettings.MinAnimationTimeMs,
                duration - ((_acceleration - 1.0) * 18));
        }

        return duration;
    }

    private double StepAxis(List<ScrollAnimation> animations, DateTime now)
    {
        if (animations.Count == 0)
        {
            return 0.0;
        }

        var inject = 0.0;
        for (var index = animations.Count - 1; index >= 0; index--)
        {
            var animation = animations[index];
            var elapsed = (now - animation.StartUtc).TotalMilliseconds;
            var progress = Math.Clamp(elapsed / animation.DurationMs, 0.0, 1.0);
            var eased = PulseCurve.Transform(progress, _settings);
            var target = animation.Amount * eased;
            var frameDelta = target - animation.Output;
            var remaining = animation.Amount - target;

            if (progress >= 1.0 || Math.Abs(remaining) <= TailSnapDelta)
            {
                frameDelta += remaining;
                animations.RemoveAt(index);
            }
            else
            {
                animation.Output = target;
            }

            inject += frameDelta;
        }

        return inject;
    }

    private static int Quantize(double value, ref double carry)
    {
        var total = value + carry;
        if (Math.Abs(total) < 0.5)
        {
            carry = total;
            return 0;
        }

        var inject = (int)Math.Truncate(total);
        carry = total - inject;
        return inject;
    }

    private static void CoalesceOldAnimations(
        List<ScrollAnimation> animations,
        DateTime now,
        int effectiveDurationMs)
    {
        if (animations.Count <= MaxAnimationsPerAxis)
        {
            return;
        }

        var overflow = animations.Count - MaxAnimationsPerAxis;
        var remaining = 0.0;
        for (var i = 0; i < overflow; i++)
        {
            remaining += animations[i].Remaining(now);
        }

        animations.RemoveRange(0, overflow);
        if (Math.Abs(remaining) >= 0.5)
        {
            var tailDuration = Math.Min(MaxCoalescedTailMs, effectiveDurationMs);
            animations.Insert(0, new ScrollAnimation(now, tailDuration, remaining));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        lock (_gate)
        {
            CancelAllAnimationTails();
            _verticalImmediateMode = false;
            _horizontalImmediateMode = false;
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        _timer.Dispose();
        EnsureInjectionDrain();
    }

    private readonly record struct PendingInjection(
        int Delta,
        bool Horizontal,
        bool IsCancelable,
        long TailGeneration)
    {
        public static PendingInjection Immediate(int delta, bool horizontal)
        {
            return new PendingInjection(delta, horizontal, IsCancelable: false, TailGeneration: 0);
        }

        public static PendingInjection Animation(int delta, bool horizontal, long tailGeneration)
        {
            return new PendingInjection(delta, horizontal, IsCancelable: true, tailGeneration);
        }
    }

    private sealed class ScrollAnimation
    {
        public ScrollAnimation(DateTime startUtc, int durationMs, double amount)
        {
            StartUtc = startUtc;
            DurationMs = durationMs;
            Amount = amount;
        }

        public DateTime StartUtc { get; }
        public int DurationMs { get; }
        public double Amount { get; }
        public double Output { get; set; }

        public double Remaining(DateTime now)
        {
            var elapsed = (now - StartUtc).TotalMilliseconds;
            var progress = Math.Clamp(elapsed / DurationMs, 0.0, 1.0);
            return Amount - (Amount * progress);
        }
    }
}
