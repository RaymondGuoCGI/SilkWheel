namespace SilkWheel.Services;

public sealed class AppSettings
{
    public bool FirstRun { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public double StepSize { get; set; } = 120.0;
    public int AnimationTimeMs { get; set; } = 540;
    public int AccelerationDeltaMs { get; set; } = 50;
    public double AccelerationMax { get; set; } = 7.0;
    public bool PulseAlgorithm { get; set; } = true;
    public double PulseScale { get; set; } = 3.0;
    public double PulseNormalize { get; set; } = 1.0;
    public double LinesToScroll { get; set; } = 1.0;
    public bool ReverseDirection { get; set; }
    public bool HorizontalSmoothing { get; set; } = true;
    public bool HorizontalShiftKey { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public string Language { get; set; } = "zh-CN";
    public string Theme { get; set; } = "Light";
    public double ThemeHue { get; set; } = 215.0;
    public double ThemeSaturation { get; set; } = 18.0;
    public double ThemeLightness { get; set; } = 94.0;
    public double CustomThemeHue { get; set; } = 215.0;
    public double CustomThemeSaturation { get; set; } = 18.0;
    public double CustomThemeLightness { get; set; } = 94.0;
    public List<ThemeProfile> ThemeProfiles { get; set; } = new();
    public string ActiveProfileId { get; set; } = "smoothscroll-a";
    public List<ScrollProfile> Profiles { get; set; } = CreateDefaultProfiles();
    public List<string> ExcludedProcesses { get; set; } = new();

    public static AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        settings.ApplyProfile(settings.Profiles[0]);
        return settings;
    }

    public void EnsureProfiles()
    {
        if (Profiles.Count == 0)
        {
            Profiles = CreateDefaultProfiles();
        }

        foreach (var profile in Profiles)
        {
            if (profile.Id == "smoothscroll-a")
            {
                profile.NameZh = "A方案 - 快速滚动";
                profile.NameEn = "Profile A - Quick";
            }
            else if (profile.Id == "quick-b")
            {
                profile.NameZh = "B方案 - 中速滚动";
                profile.NameEn = "Profile B - Medium";
            }
        }

        if (Profiles.All(profile => profile.Id != ActiveProfileId))
        {
            ActiveProfileId = Profiles[0].Id;
        }
    }

    public bool EnsureExcludedProcesses()
    {
        var legacyDefaults = new[] { "ComfyUI.exe", "xtop.exe", "parametric.exe" };
        if (ExcludedProcesses.Count == legacyDefaults.Length
            && legacyDefaults.All(item => ExcludedProcesses.Contains(item, StringComparer.OrdinalIgnoreCase)))
        {
            ExcludedProcesses.Clear();
            return true;
        }

        return false;
    }

    public void ApplyProfile(ScrollProfile profile)
    {
        ActiveProfileId = profile.Id;
        Enabled = profile.Enabled;
        StepSize = profile.StepSize;
        AnimationTimeMs = profile.AnimationTimeMs;
        AccelerationDeltaMs = profile.AccelerationDeltaMs;
        AccelerationMax = profile.AccelerationMax;
        PulseAlgorithm = profile.PulseAlgorithm;
        PulseScale = profile.PulseScale;
        PulseNormalize = profile.PulseNormalize;
        LinesToScroll = profile.LinesToScroll;
        ReverseDirection = profile.ReverseDirection;
        HorizontalSmoothing = profile.HorizontalSmoothing;
        HorizontalShiftKey = profile.HorizontalShiftKey;
    }

    public void SaveCurrentToProfile(ScrollProfile profile)
    {
        profile.Enabled = Enabled;
        profile.StepSize = StepSize;
        profile.AnimationTimeMs = AnimationTimeMs;
        profile.AccelerationDeltaMs = AccelerationDeltaMs;
        profile.AccelerationMax = AccelerationMax;
        profile.PulseAlgorithm = PulseAlgorithm;
        profile.PulseScale = PulseScale;
        profile.PulseNormalize = PulseNormalize;
        profile.LinesToScroll = LinesToScroll;
        profile.ReverseDirection = ReverseDirection;
        profile.HorizontalSmoothing = HorizontalSmoothing;
        profile.HorizontalShiftKey = HorizontalShiftKey;
    }

    private static List<ScrollProfile> CreateDefaultProfiles() => new()
    {
        new ScrollProfile
        {
            Id = "smoothscroll-a",
            NameZh = "A方案 - 快速滚动",
            NameEn = "Profile A - Quick",
            Enabled = true,
            StepSize = 110.0,
            AnimationTimeMs = 360,
            AccelerationDeltaMs = 60,
            AccelerationMax = 5.0,
            PulseAlgorithm = true,
            PulseScale = 2.4,
            PulseNormalize = 1.0,
            LinesToScroll = 1.0,
            HorizontalSmoothing = true,
            HorizontalShiftKey = true
        },
        new ScrollProfile
        {
            Id = "quick-b",
            NameZh = "B方案 - 中速滚动",
            NameEn = "Profile B - Medium",
            Enabled = true,
            StepSize = 120.0,
            AnimationTimeMs = 540,
            AccelerationDeltaMs = 50,
            AccelerationMax = 7.0,
            PulseAlgorithm = true,
            PulseScale = 3.0,
            PulseNormalize = 1.0,
            LinesToScroll = 1.0,
            HorizontalSmoothing = true,
            HorizontalShiftKey = true
        },
        new ScrollProfile
        {
            Id = "native-zero",
            NameZh = "0作用 - 原生滚轮",
            NameEn = "Zero - Native wheel",
            Enabled = false,
            StepSize = 120.0,
            AnimationTimeMs = 540,
            AccelerationDeltaMs = 50,
            AccelerationMax = 7.0,
            PulseAlgorithm = true,
            PulseScale = 3.0,
            PulseNormalize = 1.0,
            LinesToScroll = 1.0,
            HorizontalSmoothing = true,
            HorizontalShiftKey = true
        }
    };

    public void CopyFrom(AppSettings other)
    {
        FirstRun = other.FirstRun;
        Enabled = other.Enabled;
        StepSize = other.StepSize;
        AnimationTimeMs = other.AnimationTimeMs;
        AccelerationDeltaMs = other.AccelerationDeltaMs;
        AccelerationMax = other.AccelerationMax;
        PulseAlgorithm = other.PulseAlgorithm;
        PulseScale = other.PulseScale;
        PulseNormalize = other.PulseNormalize;
        LinesToScroll = other.LinesToScroll;
        ReverseDirection = other.ReverseDirection;
        HorizontalSmoothing = other.HorizontalSmoothing;
        HorizontalShiftKey = other.HorizontalShiftKey;
        StartWithWindows = other.StartWithWindows;
        Language = other.Language;
        Theme = other.Theme;
        ThemeHue = other.ThemeHue;
        ThemeSaturation = other.ThemeSaturation;
        ThemeLightness = other.ThemeLightness;
        CustomThemeHue = other.CustomThemeHue;
        CustomThemeSaturation = other.CustomThemeSaturation;
        CustomThemeLightness = other.CustomThemeLightness;
        ThemeProfiles = other.ThemeProfiles.Select(profile => profile.Clone()).ToList();
        ActiveProfileId = other.ActiveProfileId;
        Profiles = other.Profiles.Select(profile => profile.Clone()).ToList();
        ExcludedProcesses = new List<string>(other.ExcludedProcesses);
    }
}

public sealed class ThemeProfile
{
    public string Id { get; set; } = $"theme-{Guid.NewGuid():N}";
    public string Name { get; set; } = "Custom theme";
    public double Hue { get; set; } = 215.0;
    public double Saturation { get; set; } = 18.0;
    public double Lightness { get; set; } = 94.0;

    public ThemeProfile Clone() => (ThemeProfile)MemberwiseClone();
}

public sealed class ScrollProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string NameZh { get; set; } = "方案";
    public string NameEn { get; set; } = "Profile";
    public bool Enabled { get; set; } = true;
    public double StepSize { get; set; } = 120.0;
    public int AnimationTimeMs { get; set; } = 540;
    public int AccelerationDeltaMs { get; set; } = 50;
    public double AccelerationMax { get; set; } = 7.0;
    public bool PulseAlgorithm { get; set; } = true;
    public double PulseScale { get; set; } = 3.0;
    public double PulseNormalize { get; set; } = 1.0;
    public double LinesToScroll { get; set; } = 1.0;
    public bool ReverseDirection { get; set; }
    public bool HorizontalSmoothing { get; set; } = true;
    public bool HorizontalShiftKey { get; set; } = true;

    public ScrollProfile Clone() => (ScrollProfile)MemberwiseClone();
}
