namespace SilkWheel.Services;

public sealed class ScrollEngine : IDisposable
{
    private const int FrameMs = 7;
    private const double TailSnapDelta = 1.25;
    private const int MaxAnimationsPerAxis = 18;

    private readonly AppSettings _settings;
    private readonly object _gate = new();
    private readonly System.Threading.Timer _timer;
    private readonly List<ScrollAnimation> _vertical = new();
    private readonly List<ScrollAnimation> _horizontal = new();
    private double _verticalCarry;
    private double _horizontalCarry;
    private DateTime _lastInputUtc = DateTime.MinValue;
    private int _lastDirection;
    private double _acceleration = 1.0;
    private int _isTicking;
    private bool _disposed;

    public ScrollEngine(AppSettings settings)
    {
        _settings = settings;
        _timer = new System.Threading.Timer(Tick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Enqueue(int wheelDelta, bool horizontal)
    {
        var direction = Math.Sign(wheelDelta);
        if (direction == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        lock (_gate)
        {
            if (direction != _lastDirection)
            {
                _vertical.Clear();
                _horizontal.Clear();
                _verticalCarry = 0.0;
                _horizontalCarry = 0.0;
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

            list.Add(new ScrollAnimation(now, duration, amount));
            CoalesceOldAnimations(list, now);
        }

        _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(FrameMs));
    }

    private void Tick(object? state)
    {
        if (_disposed || Interlocked.Exchange(ref _isTicking, 1) == 1)
        {
            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            int verticalInject;
            int horizontalInject;
            bool active;

            lock (_gate)
            {
                verticalInject = Quantize(StepAxis(_vertical, now), ref _verticalCarry);
                horizontalInject = Quantize(StepAxis(_horizontal, now), ref _horizontalCarry);
                active = _vertical.Count > 0 || _horizontal.Count > 0;
            }

            if (verticalInject != 0)
            {
                InputInjector.SendWheel(verticalInject, horizontal: false);
            }

            if (horizontalInject != 0)
            {
                InputInjector.SendWheel(horizontalInject, horizontal: true);
            }

            if (!active)
            {
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isTicking, 0);
        }
    }

    private int GetDurationMs()
    {
        var duration = Math.Clamp(_settings.AnimationTimeMs, 100, 900);

        // Continuous wheel input should keep the previous version's quick feel,
        // while still using a finite curve so the tail does not wobble.
        if (_acceleration > 1.0)
        {
            duration = (int)Math.Max(120, duration - ((_acceleration - 1.0) * 18));
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

    private static void CoalesceOldAnimations(List<ScrollAnimation> animations, DateTime now)
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
            animations.Insert(0, new ScrollAnimation(now, 120, remaining));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _timer.Dispose();
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
