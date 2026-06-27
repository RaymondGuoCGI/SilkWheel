namespace SilkWheel.Services;

public static class PulseCurve
{
    public static double Transform(double progress, AppSettings settings)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);
        if (!settings.PulseAlgorithm)
        {
            return 1.0 - Math.Pow(1.0 - progress, 3.0);
        }

        var normalized = Pulse(progress, settings.PulseScale);
        var divisor = Pulse(1.0, settings.PulseScale);
        if (divisor <= 0.0001)
        {
            return progress;
        }

        return Math.Clamp((normalized / divisor) * settings.PulseNormalize, 0.0, 1.0);
    }

    private static double Pulse(double x, double scale)
    {
        x *= scale;
        if (x < 1.0)
        {
            return x - (1.0 - Math.Exp(-x));
        }

        const double start = 0.36787944117;
        x -= 1.0;
        return start + ((1.0 - Math.Exp(-x)) * (1.0 - start));
    }
}
