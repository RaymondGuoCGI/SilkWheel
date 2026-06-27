using SilkWheel.Services;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace SilkWheel;

public partial class MainWindow : Window
{
    private const double WindowCornerRadius = 8.0;

    private readonly AppSettings _settings;
    private readonly Action _save;
    private bool _loading = true;

    public MainWindow(AppSettings settings, Action save)
    {
        _settings = settings;
        _save = save;
        SourceInitialized += Window_SourceInitialized;
        InitializeComponent();
        LoadLogo();
        LoadSettings();
        ApplyLanguage();
        ApplyTheme();
        UpdateWindowChromeShape();
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyDwmFrame();
        UpdateWindowChromeShape();
    }

    private void LoadLogo()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
        {
            return;
        }

        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
        if (icon == null)
        {
            return;
        }

        var source = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromWidthAndHeight(64, 64));
        Icon = source;
        LogoImage.Source = source;
        TitleLogoImage.Source = source;
    }

    private void LoadSettings()
    {
        _loading = true;
        _settings.EnsureProfiles();
        _settings.EnsureExcludedProcesses();
        var themeMigrated = EnsureThemeValues();
        PopulateProfiles();
        PopulateThemes();
        SelectLanguage(_settings.Language);
        SelectTheme(_settings.Theme);
        ThemeHueSlider.Value = _settings.ThemeHue;
        ThemeSaturationSlider.Value = _settings.ThemeSaturation;
        ThemeLightnessSlider.Value = _settings.ThemeLightness;
        SelectProfile(_settings.ActiveProfileId);
        EnabledBox.IsChecked = _settings.Enabled;
        StepSizeSlider.Value = _settings.StepSize;
        AnimationSlider.Value = _settings.AnimationTimeMs;
        AccelerationDeltaSlider.Value = _settings.AccelerationDeltaMs;
        AccelerationMaxSlider.Value = _settings.AccelerationMax;
        PulseScaleSlider.Value = _settings.PulseScale;
        LinesSlider.Value = _settings.LinesToScroll;
        PulseBox.IsChecked = _settings.PulseAlgorithm;
        ReverseBox.IsChecked = _settings.ReverseDirection;
        HorizontalBox.IsChecked = _settings.HorizontalSmoothing;
        ShiftHorizontalBox.IsChecked = _settings.HorizontalShiftKey;
        StartupBox.IsChecked = _settings.StartWithWindows;
        RefreshExcludedList();
        _loading = false;
        UpdateValueLabels();
        if (themeMigrated)
        {
            _save();
        }
    }

    private void PopulateProfiles()
    {
        ProfileBox.Items.Clear();
        var zh = _settings.Language == "zh-CN";
        foreach (var profile in _settings.Profiles)
        {
            ProfileBox.Items.Add(new ComboBoxItem
            {
                Tag = profile.Id,
                Content = zh ? profile.NameZh : profile.NameEn
            });
        }
    }

    private void PopulateThemes()
    {
        ThemeBox.Items.Clear();
        var zh = _settings.Language == "zh-CN";
        ThemeBox.Items.Add(new ComboBoxItem { Tag = "Light", Content = zh ? "亮色" : "Light" });
        ThemeBox.Items.Add(new ComboBoxItem { Tag = "Gray", Content = zh ? "灰色" : "Gray" });
        ThemeBox.Items.Add(new ComboBoxItem { Tag = "Dark", Content = zh ? "暗色" : "Dark" });
        ThemeBox.Items.Add(new ComboBoxItem { Tag = "Custom", Content = zh ? "自定义" : "Custom" });

        foreach (var theme in _settings.ThemeProfiles)
        {
            ThemeBox.Items.Add(new ComboBoxItem
            {
                Tag = theme.Id,
                Content = theme.Name
            });
        }
    }

    private void SelectLanguage(string language)
    {
        foreach (ComboBoxItem item in LanguageBox.Items)
        {
            if ((string)item.Tag == language)
            {
                LanguageBox.SelectedItem = item;
                LanguageBox.Text = item.Content?.ToString() ?? string.Empty;
                return;
            }
        }

        LanguageBox.SelectedIndex = 0;
    }

    private void SelectTheme(string theme)
    {
        foreach (ComboBoxItem item in ThemeBox.Items)
        {
            if (string.Equals((string)item.Tag, theme, StringComparison.OrdinalIgnoreCase))
            {
                ThemeBox.SelectedItem = item;
                ThemeBox.Text = item.Content?.ToString() ?? string.Empty;
                return;
            }
        }

        ThemeBox.SelectedIndex = 0;
    }

    private void SelectProfile(string profileId)
    {
        foreach (ComboBoxItem item in ProfileBox.Items)
        {
            if (string.Equals((string)item.Tag, profileId, StringComparison.OrdinalIgnoreCase))
            {
                ProfileBox.SelectedItem = item;
                ProfileBox.Text = item.Content?.ToString() ?? string.Empty;
                return;
            }
        }

        if (ProfileBox.Items.Count > 0)
        {
            ProfileBox.SelectedIndex = 0;
        }
    }

    private void ApplyLanguage()
    {
        var zh = _settings.Language == "zh-CN";
        Title = zh ? "SilkWheel 设置" : "SilkWheel Settings";
        SubtitleText.Text = zh ? "让你的鼠标滚轮，纵享丝滑。" : "Let your mouse wheel glide smoothly.";
        PageTitleText.Text = zh ? "滚轮体验" : "Wheel Experience";
        PageHelpText.Text = zh ? "参数会自动保存，托盘程序立即使用新的手感。" : "Changes are saved automatically and applied to the tray app immediately.";
        EnabledTitleText.Text = zh ? "全局启用" : "Global enable";
        EnabledHelpText.Text = zh ? "关闭后 SilkWheel 会放行原始滚轮事件。" : "When disabled, SilkWheel passes wheel input through unchanged.";
        StatusTitleText.Text = _settings.Enabled
            ? (zh ? "正在运行" : "Running")
            : (zh ? "已暂停" : "Paused");
        StatusHelpText.Text = _settings.Enabled
            ? (zh ? "托盘常驻，滚轮已接管" : "Tray resident, smoothing active")
            : (zh ? "原生滚轮直通" : "Native wheel passthrough");
        StatusDot.Fill = _settings.Enabled
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 201, 179))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
        ProfileTitleText.Text = zh ? "当前手感" : "Current profile";
        ProfileHelpText.Text = zh ? "当前滚动方案参数" : "Current scroll profile";
        ProfileSelectTitleText.Text = zh ? "优化方案" : "Tuning profiles";
        ProfileSelectHelpText.Text = zh ? "切换、管理并保存滚动手感。" : "Switch, manage, and save scroll tuning.";
        SetIconButton(ResetButton, "\uE72C", zh ? "还原当前手感" : "Reset current feel");
        SetIconButton(NewProfileButton, "\uE710", zh ? "新建方案" : "New profile");
        SetIconButton(RenameProfileButton, "\uE70F", zh ? "重命名方案" : "Rename profile");
        SetIconButton(DeleteProfileButton, "\uE738", zh ? "删除方案" : "Delete profile");
        RenameProfileButton.IsEnabled = _settings.ActiveProfileId != "native-zero";
        DeleteProfileButton.IsEnabled = !IsBuiltInProfile(_settings.ActiveProfileId);
        SaveProfileButton.IsEnabled = _settings.ActiveProfileId != "native-zero";
        SetIconButton(SaveProfileButton, "\uE74E", _settings.ActiveProfileId == "native-zero"
            ? (zh ? "对照组不可保存" : "Baseline cannot be saved")
            : (zh ? "保存到方案" : "Save profile"));
        RefreshProfileLabels();
        LanguageText.Text = zh ? "界面语言" : "Language";
        SetComboItemText(LanguageBox, "zh-CN", "中文");
        SetComboItemText(LanguageBox, "en-US", "English");
        ThemeText.Text = zh ? "主题" : "Theme";
        var wasLoading = _loading;
        _loading = true;
        PopulateThemes();
        SelectTheme(_settings.Theme);
        _loading = wasLoading;
        ThemeHueText.Text = zh ? "色相" : "Hue";
        ThemeSaturationText.Text = zh ? "饱和度" : "Saturation";
        ThemeLightnessText.Text = zh ? "明度" : "Lightness";
        SaveThemeButton.Content = zh ? "保存主题" : "Save";
        DeleteThemeButton.Content = zh ? "删除" : "Delete";
        DeleteThemeButton.IsEnabled = IsUserTheme(_settings.Theme);
        FeelTitleText.Text = zh ? "滚动手感" : "Scroll feel";
        FeelHelpText.Text = zh ? "保留原 SmoothScroll 长尾感，同时可以快速微调。" : "Keeps the SmoothScroll-like long tail while staying easy to tune.";
        StepSizeText.Text = zh ? "单次滚动强度" : "Step size";
        AnimationText.Text = zh ? "动画时长" : "Animation time";
        AccelerationDeltaText.Text = zh ? "加速度窗口" : "Acceleration window";
        AccelerationMaxText.Text = zh ? "最大加速倍数" : "Max acceleration";
        PulseScaleText.Text = zh ? "Pulse 曲线强度" : "Pulse scale";
        LinesText.Text = zh ? "每格滚动行数" : "Lines per notch";
        StepSizeHintText.Text = zh ? "越大每次滚动越远" : "Higher moves farther per notch";
        AnimationHintText.Text = zh ? "越大惯性尾巴越长" : "Higher creates a longer glide";
        AccelerationDeltaHintText.Text = zh ? "连续滚动判定时间" : "Rapid scroll detection window";
        AccelerationMaxHintText.Text = zh ? "快速滚动的速度上限" : "Speed cap for rapid scrolling";
        PulseScaleHintText.Text = zh ? "影响前段响应和尾部缓动" : "Shapes the response and easing tail";
        LinesHintText.Text = zh ? "保留为 1 最接近原配置" : "Keep at 1 for the original profile";
        PulseBox.Content = zh ? "启用 Pulse 缓动" : "Use pulse easing";
        ReverseBox.Content = zh ? "反向滚动" : "Reverse direction";
        HorizontalBox.Content = zh ? "启用横向平滑" : "Smooth horizontal wheel";
        ShiftHorizontalBox.Content = zh ? "Shift + 滚轮转横向" : "Shift + wheel scrolls horizontally";
        GeneralTitleText.Text = zh ? "常规" : "General";
        StartupBox.Content = zh ? "开机启动" : "Start with Windows";
        ExcludedText.Text = zh ? "排除应用进程名" : "Excluded process names";
        SetPlainIconButton(AddExcludedButton, "+", zh ? "添加排除应用" : "Add excluded app");
        SetPlainIconButton(DeleteExcludedButton, "−", zh ? "删除排除应用" : "Delete excluded app");
        UpdateExcludedDeleteState();
        ExcludedHelpText.Text = zh ? "添加进程名或完整路径。命中后该应用使用原生滚轮。" : "Add a process name or full path. Matched apps use native wheel input.";
        RefreshExcludedList();
        UpdateValueLabels();
    }

    private static void SetIconButton(System.Windows.Controls.Button button, string glyph, string tooltip, double fontSize = 15)
    {
        button.Content = new TextBlock
        {
            Text = glyph,
            FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize = fontSize,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        button.ToolTip = tooltip;
    }

    private static void SetPlainIconButton(System.Windows.Controls.Button button, string glyph, string tooltip)
    {
        button.Content = new TextBlock
        {
            Text = glyph,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 16,
            FontWeight = FontWeights.Normal,
            Foreground = (System.Windows.Media.Brush)button.FindResource("SidebarTextPrimary"),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        button.ToolTip = tooltip;
    }

    private void SetComboItemText(System.Windows.Controls.ComboBox comboBox, string tag, string text)
    {
        foreach (ComboBoxItem item in comboBox.Items)
        {
            if ((string)item.Tag == tag)
            {
                item.Content = text;
                if (comboBox.SelectedItem == item)
                {
                    comboBox.Text = text;
                }
                return;
            }
        }
    }

    private void RefreshProfileLabels()
    {
        var zh = _settings.Language == "zh-CN";
        foreach (ComboBoxItem item in ProfileBox.Items)
        {
            var profile = _settings.Profiles.FirstOrDefault(candidate => candidate.Id == (string)item.Tag);
            if (profile == null)
            {
                continue;
            }

            item.Content = zh ? profile.NameZh : profile.NameEn;
            if (ProfileBox.SelectedItem == item)
            {
                ProfileBox.Text = item.Content?.ToString() ?? string.Empty;
            }
        }
    }

    private void ApplyTheme()
    {
        var theme = _settings.Theme;
        ThemeTuningPanel.Visibility = Visibility.Visible;

        if (string.Equals(theme, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            ApplyHslTheme(_settings.ThemeHue, _settings.ThemeSaturation, _settings.ThemeLightness);
            return;
        }

        var savedTheme = _settings.ThemeProfiles.FirstOrDefault(candidate => string.Equals(candidate.Id, theme, StringComparison.OrdinalIgnoreCase));
        if (savedTheme != null)
        {
            ApplyHslTheme(savedTheme.Hue, savedTheme.Saturation, savedTheme.Lightness);
            return;
        }

        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            SetBrush("TextPrimary", "#E7E7E7");
            SetBrush("TextSecondary", "#A0A4AA");
            SetBrush("PanelBorder", "#32363D");
            SetBrush("CardBackground", "#121417");
            SetBrush("ControlBackground", "#1B1E23");
            SetBrush("ControlBorder", "#3B4048");
            SetBrush("ChromeBackground", "#080A0D");
            SetBrush("SidebarTextPrimary", "#F0F0F0");
            SetBrush("SidebarTextSecondary", "#A7ABB1");
            SetBrush("SidebarCardBackground", "#181B20");
            SetBrush("SidebarCardBorder", "#30343B");
            SetBrush("LogoTileBackground", "#202329");
            SetBrush("Accent", "#19B8A8");
            Background = BrushFrom("#0D0F12");
            SidebarBorder.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
            TitleBar.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
            WindowOutline.Background = System.Windows.Media.Brushes.Transparent;
            WindowFrame.Background = BrushFrom("#0D0F12");
            return;
        }

        if (string.Equals(theme, "Gray", StringComparison.OrdinalIgnoreCase))
        {
            SetBrush("TextPrimary", "#1D232B");
            SetBrush("TextSecondary", "#535C67");
            SetBrush("PanelBorder", "#9EA8B4");
            SetBrush("CardBackground", "#DDE2E7");
            SetBrush("ControlBackground", "#CCD3DB");
            SetBrush("ControlBorder", "#98A4B1");
            SetBrush("ChromeBackground", "#B8C1CB");
            SetBrush("SidebarTextPrimary", "#1D232B");
            SetBrush("SidebarTextSecondary", "#4D5661");
            SetBrush("SidebarCardBackground", "#C9D1DA");
            SetBrush("SidebarCardBorder", "#98A4B1");
            SetBrush("LogoTileBackground", "#D9DFE5");
            SetBrush("Accent", "#18BFAF");
            Background = BrushFrom("#C4CCD5");
            SidebarBorder.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
            TitleBar.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
            WindowOutline.Background = System.Windows.Media.Brushes.Transparent;
            WindowFrame.Background = BrushFrom("#C4CCD5");
            return;
        }

        SetBrush("TextPrimary", "#111827");
        SetBrush("TextSecondary", "#64748B");
        SetBrush("PanelBorder", "#DDE5EF");
        SetBrush("CardBackground", "#FFFFFF");
        SetBrush("ControlBackground", "#F8FAFC");
        SetBrush("ControlBorder", "#CBD5E1");
        SetBrush("ChromeBackground", "#F8FAFC");
        SetBrush("SidebarTextPrimary", "#111827");
        SetBrush("SidebarTextSecondary", "#64748B");
        SetBrush("SidebarCardBackground", "#FFFFFF");
        SetBrush("SidebarCardBorder", "#DDE5EF");
        SetBrush("LogoTileBackground", "#EEF2F7");
        SetBrush("Accent", "#20C9B3");
        Background = BrushFrom("#EEF2F7");
        SidebarBorder.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
        TitleBar.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
        WindowOutline.Background = System.Windows.Media.Brushes.Transparent;
        WindowFrame.Background = BrushFrom("#EEF2F7");
    }

    private void ApplyHslTheme(double hue, double saturation, double lightness)
    {
        var isDark = lightness < 48;
        var sat = Math.Clamp(saturation, 0, 80);
        var l = Math.Clamp(lightness, 6, 96);

        if (isDark)
        {
            SetBrush("TextPrimary", ToHex(hue, Math.Min(sat, 18), 88));
            SetBrush("TextSecondary", ToHex(hue, Math.Min(sat, 14), 66));
            SetBrush("PanelBorder", ToHex(hue, Math.Min(sat, 16), Math.Min(34, l + 16)));
            SetBrush("CardBackground", ToHex(hue, sat, Math.Max(10, l + 3)));
            SetBrush("ControlBackground", ToHex(hue, sat, Math.Min(30, l + 10)));
            SetBrush("ControlBorder", ToHex(hue, Math.Min(sat, 20), Math.Min(40, l + 18)));
            SetBrush("ChromeBackground", ToHex(hue, sat, Math.Max(5, l - 4)));
            SetBrush("SidebarTextPrimary", ToHex(hue, Math.Min(sat, 14), 92));
            SetBrush("SidebarTextSecondary", ToHex(hue, Math.Min(sat, 13), 68));
            SetBrush("SidebarCardBackground", ToHex(hue, sat, Math.Min(26, l + 7)));
            SetBrush("SidebarCardBorder", ToHex(hue, Math.Min(sat, 18), Math.Min(38, l + 16)));
            SetBrush("LogoTileBackground", ToHex(hue, sat, Math.Min(32, l + 12)));
            SetBrush("Accent", ToHex(174, 74, 43));
        }
        else
        {
            SetBrush("TextPrimary", ToHex(hue, Math.Min(sat, 18), 14));
            SetBrush("TextSecondary", ToHex(hue, Math.Min(sat, 16), 42));
            SetBrush("PanelBorder", ToHex(hue, Math.Min(sat, 20), Math.Max(64, l - 18)));
            SetBrush("CardBackground", ToHex(hue, sat, Math.Min(98, l + 4)));
            SetBrush("ControlBackground", ToHex(hue, sat, Math.Max(74, l - 4)));
            SetBrush("ControlBorder", ToHex(hue, Math.Min(sat, 22), Math.Max(60, l - 20)));
            SetBrush("ChromeBackground", ToHex(hue, sat, Math.Max(62, l - 12)));
            SetBrush("SidebarTextPrimary", ToHex(hue, Math.Min(sat, 18), 15));
            SetBrush("SidebarTextSecondary", ToHex(hue, Math.Min(sat, 16), 39));
            SetBrush("SidebarCardBackground", ToHex(hue, sat, Math.Max(72, l - 6)));
            SetBrush("SidebarCardBorder", ToHex(hue, Math.Min(sat, 22), Math.Max(58, l - 20)));
            SetBrush("LogoTileBackground", ToHex(hue, sat, Math.Min(96, l + 2)));
            SetBrush("Accent", ToHex(174, 76, 42));
        }

        var windowColor = ToHex(hue, sat, l);
        Background = BrushFrom(windowColor);
        SidebarBorder.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
        TitleBar.Background = (System.Windows.Media.Brush)Resources["ChromeBackground"];
        WindowOutline.Background = System.Windows.Media.Brushes.Transparent;
        WindowFrame.Background = BrushFrom(windowColor);
    }

    private void SetBrush(string key, string color)
    {
        Resources[key] = BrushFrom(color);
    }

    private static SolidColorBrush BrushFrom(string color) => new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));

    private bool EnsureThemeValues()
    {
        var migrated = false;
        if (string.Equals(_settings.Theme, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            if (IsLegacyCustomTheme())
            {
                _settings.CustomThemeHue = _settings.ThemeHue;
                _settings.CustomThemeSaturation = _settings.ThemeSaturation;
                _settings.CustomThemeLightness = _settings.ThemeLightness;
                migrated = true;
            }

            _settings.ThemeHue = _settings.CustomThemeHue;
            _settings.ThemeSaturation = _settings.CustomThemeSaturation;
            _settings.ThemeLightness = _settings.CustomThemeLightness;
            return migrated;
        }

        var savedTheme = _settings.ThemeProfiles.FirstOrDefault(candidate => string.Equals(candidate.Id, _settings.Theme, StringComparison.OrdinalIgnoreCase));
        if (savedTheme != null)
        {
            _settings.ThemeHue = savedTheme.Hue;
            _settings.ThemeSaturation = savedTheme.Saturation;
            _settings.ThemeLightness = savedTheme.Lightness;
            return migrated;
        }

        var preset = ThemePreset(_settings.Theme);
        _settings.ThemeHue = preset.Hue;
        _settings.ThemeSaturation = preset.Saturation;
        _settings.ThemeLightness = preset.Lightness;
        return migrated;
    }

    private bool IsLegacyCustomTheme()
    {
        return Math.Abs(_settings.CustomThemeHue - 215.0) < 0.001
            && Math.Abs(_settings.CustomThemeSaturation - 18.0) < 0.001
            && Math.Abs(_settings.CustomThemeLightness - 94.0) < 0.001
            && (Math.Abs(_settings.ThemeHue - 215.0) > 0.001
                || Math.Abs(_settings.ThemeSaturation - 18.0) > 0.001
                || Math.Abs(_settings.ThemeLightness - 94.0) > 0.001);
    }

    private static (double Hue, double Saturation, double Lightness) ThemePreset(string theme)
    {
        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            return (215, 10, 13);
        }

        if (string.Equals(theme, "Gray", StringComparison.OrdinalIgnoreCase))
        {
            return (215, 12, 74);
        }

        return (215, 18, 94);
    }

    private bool IsUserTheme(string theme)
    {
        return _settings.ThemeProfiles.Any(candidate => string.Equals(candidate.Id, theme, StringComparison.OrdinalIgnoreCase));
    }

    private void SetThemeSliders(double hue, double saturation, double lightness)
    {
        _loading = true;
        ThemeHueSlider.Value = hue;
        ThemeSaturationSlider.Value = saturation;
        ThemeLightnessSlider.Value = lightness;
        _loading = false;
        UpdateValueLabels();
    }

    private static string ToHex(double hue, double saturation, double lightness)
    {
        var color = HslToRgb(hue, saturation / 100.0, lightness / 100.0);
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static System.Windows.Media.Color HslToRgb(double hue, double saturation, double lightness)
    {
        hue = ((hue % 360) + 360) % 360 / 360.0;
        saturation = Math.Clamp(saturation, 0.0, 1.0);
        lightness = Math.Clamp(lightness, 0.0, 1.0);

        if (saturation <= 0.0001)
        {
            var gray = (byte)Math.Round(lightness * 255);
            return System.Windows.Media.Color.FromRgb(gray, gray, gray);
        }

        var q = lightness < 0.5
            ? lightness * (1 + saturation)
            : lightness + saturation - (lightness * saturation);
        var p = (2 * lightness) - q;
        var r = HueToRgb(p, q, hue + (1.0 / 3.0));
        var g = HueToRgb(p, q, hue);
        var b = HueToRgb(p, q, hue - (1.0 / 3.0));
        return System.Windows.Media.Color.FromRgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }

        if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6.0)
        {
            return p + ((q - p) * 6 * t);
        }

        if (t < 1.0 / 2.0)
        {
            return q;
        }

        if (t < 2.0 / 3.0)
        {
            return p + ((q - p) * ((2.0 / 3.0) - t) * 6);
        }

        return p;
    }

    private void SaveFromControls()
    {
        if (_loading)
        {
            return;
        }

        _settings.Enabled = EnabledBox.IsChecked == true;
        _settings.StepSize = StepSizeSlider.Value;
        _settings.AnimationTimeMs = (int)AnimationSlider.Value;
        _settings.AccelerationDeltaMs = (int)AccelerationDeltaSlider.Value;
        _settings.AccelerationMax = AccelerationMaxSlider.Value;
        _settings.PulseScale = PulseScaleSlider.Value;
        _settings.LinesToScroll = LinesSlider.Value;
        _settings.PulseAlgorithm = PulseBox.IsChecked == true;
        _settings.ReverseDirection = ReverseBox.IsChecked == true;
        _settings.HorizontalSmoothing = HorizontalBox.IsChecked == true;
        _settings.HorizontalShiftKey = ShiftHorizontalBox.IsChecked == true;
        _settings.StartWithWindows = StartupBox.IsChecked == true;
        _settings.Theme = ThemeBox.SelectedItem is ComboBoxItem themeItem ? (string)themeItem.Tag : _settings.Theme;
        _settings.ThemeHue = ThemeHueSlider.Value;
        _settings.ThemeSaturation = ThemeSaturationSlider.Value;
        _settings.ThemeLightness = ThemeLightnessSlider.Value;
        if (string.Equals(_settings.Theme, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            _settings.CustomThemeHue = _settings.ThemeHue;
            _settings.CustomThemeSaturation = _settings.ThemeSaturation;
            _settings.CustomThemeLightness = _settings.ThemeLightness;
        }
        _save();
        ApplyLanguage();
        ApplyTheme();
        UpdateValueLabels();
    }

    private void UpdateValueLabels()
    {
        if (StepSizeValue == null)
        {
            return;
        }

        StepSizeValue.Text = $"{StepSizeSlider.Value:0}";
        AnimationValue.Text = $"{AnimationSlider.Value:0} ms";
        AccelerationDeltaValue.Text = $"{AccelerationDeltaSlider.Value:0} ms";
        AccelerationMaxValue.Text = $"{AccelerationMaxSlider.Value:0.0}x";
        PulseScaleValue.Text = $"{PulseScaleSlider.Value:0.00}";
        LinesValue.Text = $"{LinesSlider.Value:0}";
        ThemeHueValue.Text = $"{ThemeHueSlider.Value:0}°";
        ThemeSaturationValue.Text = $"{ThemeSaturationSlider.Value:0}%";
        ThemeLightnessValue.Text = $"{ThemeLightnessSlider.Value:0}%";
    }

    private void AnySettingChanged(object sender, RoutedEventArgs e) => SaveFromControls();

    private void AnySliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => SaveFromControls();

    private void ThemeSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Theme = "Custom";
        _loading = true;
        SelectTheme("Custom");
        _loading = false;
        _settings.ThemeHue = ThemeHueSlider.Value;
        _settings.ThemeSaturation = ThemeSaturationSlider.Value;
        _settings.ThemeLightness = ThemeLightnessSlider.Value;
        _settings.CustomThemeHue = _settings.ThemeHue;
        _settings.CustomThemeSaturation = _settings.ThemeSaturation;
        _settings.CustomThemeLightness = _settings.ThemeLightness;
        _save();
        ApplyTheme();
        ApplyLanguage();
        UpdateValueLabels();
    }

    private void RefreshExcludedList()
    {
        if (ExcludedList == null)
        {
            return;
        }

        ExcludedList.Items.Clear();
        if (_settings.ExcludedProcesses.Count == 0)
        {
            ExcludedList.IsHitTestVisible = true;
            UpdateExcludedDeleteState();
            return;
        }

        ExcludedList.IsHitTestVisible = true;
        foreach (var process in _settings.ExcludedProcesses)
        {
            ExcludedList.Items.Add(process);
        }
        UpdateExcludedDeleteState();
    }

    private void AddExcludedButton_Click(object sender, RoutedEventArgs e)
    {
        var zh = _settings.Language == "zh-CN";
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = zh ? "选择要排除的应用" : "Select an app to exclude",
            Filter = zh ? "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*" : "Applications (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog(this) != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return;
        }

        var rule = dialog.FileName.Trim();
        if (!_settings.ExcludedProcesses.Contains(rule, StringComparer.OrdinalIgnoreCase))
        {
            _settings.ExcludedProcesses.Add(rule);
            _settings.ExcludedProcesses = _settings.ExcludedProcesses
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _save();
            RefreshExcludedList();
            ApplyLanguage();
        }
    }

    private void DeleteExcludedButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelectedExcludedProcess(out var selected))
        {
            return;
        }

        _settings.ExcludedProcesses.RemoveAll(item => string.Equals(item, selected, StringComparison.OrdinalIgnoreCase));
        _save();
        RefreshExcludedList();
        ApplyLanguage();
    }

    private void ExcludedList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateExcludedDeleteState();
    }

    private void UpdateExcludedDeleteState()
    {
        if (DeleteExcludedButton == null)
        {
            return;
        }

        DeleteExcludedButton.IsEnabled = TryGetSelectedExcludedProcess(out _);
    }

    private bool TryGetSelectedExcludedProcess(out string selected)
    {
        selected = string.Empty;
        if (ExcludedList?.SelectedItem is not string value || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        selected = value;
        return _settings.ExcludedProcesses.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LanguageBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _settings.Language = (string)item.Tag;
        _save();
        ApplyLanguage();
        ApplyTheme();
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ThemeBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _settings.Theme = (string)item.Tag;
        if (string.Equals(_settings.Theme, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            _settings.ThemeHue = _settings.CustomThemeHue;
            _settings.ThemeSaturation = _settings.CustomThemeSaturation;
            _settings.ThemeLightness = _settings.CustomThemeLightness;
            SetThemeSliders(_settings.ThemeHue, _settings.ThemeSaturation, _settings.ThemeLightness);
        }
        else if (_settings.ThemeProfiles.FirstOrDefault(candidate => string.Equals(candidate.Id, _settings.Theme, StringComparison.OrdinalIgnoreCase)) is { } savedTheme)
        {
            _settings.ThemeHue = savedTheme.Hue;
            _settings.ThemeSaturation = savedTheme.Saturation;
            _settings.ThemeLightness = savedTheme.Lightness;
            SetThemeSliders(savedTheme.Hue, savedTheme.Saturation, savedTheme.Lightness);
        }
        else
        {
            var preset = ThemePreset(_settings.Theme);
            _settings.ThemeHue = preset.Hue;
            _settings.ThemeSaturation = preset.Saturation;
            _settings.ThemeLightness = preset.Lightness;
            SetThemeSliders(preset.Hue, preset.Saturation, preset.Lightness);
        }

        _save();
        ApplyTheme();
        ApplyLanguage();
    }

    private void SaveThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var zh = _settings.Language == "zh-CN";
        var defaultName = zh
            ? $"自定义主题 {_settings.ThemeProfiles.Count + 1}"
            : $"Custom theme {_settings.ThemeProfiles.Count + 1}";
        var name = ThemedDialog.ShowInput(
            this,
            zh ? "保存主题" : "Save Theme",
            zh ? "输入主题方案名称：" : "Enter a theme name:",
            defaultName,
            zh ? "确定" : "OK",
            zh ? "取消" : "Cancel");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var theme = new ThemeProfile
        {
            Id = $"theme-{Guid.NewGuid():N}",
            Name = name.Trim(),
            Hue = ThemeHueSlider.Value,
            Saturation = ThemeSaturationSlider.Value,
            Lightness = ThemeLightnessSlider.Value
        };

        _settings.ThemeProfiles.Add(theme);
        _settings.Theme = theme.Id;
        _settings.ThemeHue = theme.Hue;
        _settings.ThemeSaturation = theme.Saturation;
        _settings.ThemeLightness = theme.Lightness;
        _save();

        var wasLoading = _loading;
        _loading = true;
        PopulateThemes();
        SelectTheme(theme.Id);
        _loading = wasLoading;
        ApplyTheme();
        ApplyLanguage();
        UpdateValueLabels();
    }

    private void DeleteThemeButton_Click(object sender, RoutedEventArgs e)
    {
        var theme = _settings.ThemeProfiles.FirstOrDefault(candidate => string.Equals(candidate.Id, _settings.Theme, StringComparison.OrdinalIgnoreCase));
        if (theme == null)
        {
            return;
        }

        var zh = _settings.Language == "zh-CN";
        var confirmed = ThemedDialog.Confirm(
            this,
            zh ? "删除主题" : "Delete Theme",
            zh ? $"删除主题“{theme.Name}”？" : $"Delete theme \"{theme.Name}\"?",
            zh ? "删除" : "Delete",
            zh ? "取消" : "Cancel");
        if (!confirmed)
        {
            return;
        }

        _settings.CustomThemeHue = theme.Hue;
        _settings.CustomThemeSaturation = theme.Saturation;
        _settings.CustomThemeLightness = theme.Lightness;
        _settings.ThemeHue = theme.Hue;
        _settings.ThemeSaturation = theme.Saturation;
        _settings.ThemeLightness = theme.Lightness;
        _settings.ThemeProfiles.Remove(theme);
        _settings.Theme = "Custom";
        _save();

        var wasLoading = _loading;
        _loading = true;
        PopulateThemes();
        SelectTheme("Custom");
        SetThemeSliders(_settings.ThemeHue, _settings.ThemeSaturation, _settings.ThemeLightness);
        _loading = wasLoading;
        ApplyTheme();
        ApplyLanguage();
        UpdateValueLabels();
    }

    private void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ProfileBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        var profile = _settings.Profiles.FirstOrDefault(candidate => candidate.Id == (string)item.Tag);
        if (profile == null)
        {
            return;
        }

        _settings.ApplyProfile(profile);
        LoadSettings();
        ApplyLanguage();
        ApplyTheme();
        _save();
    }

    private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = _settings.Profiles.FirstOrDefault(candidate => candidate.Id == _settings.ActiveProfileId);
        if (profile == null || profile.Id == "native-zero")
        {
            return;
        }

        SaveFromControls();
        _settings.SaveCurrentToProfile(profile);
        _save();
        ApplyLanguage();
    }

    private void NewProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var zh = _settings.Language == "zh-CN";
        var name = ThemedDialog.ShowInput(
            this,
            zh ? "新建方案" : "New Profile",
            zh ? "输入新方案名称：" : "Enter a new profile name:",
            zh ? $"自定义方案 {_settings.Profiles.Count - 1}" : $"Custom profile {_settings.Profiles.Count - 1}",
            zh ? "确定" : "OK",
            zh ? "取消" : "Cancel");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        SaveFromControls();
        var profile = new ScrollProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            NameZh = name.Trim(),
            NameEn = name.Trim()
        };
        _settings.SaveCurrentToProfile(profile);
        _settings.Profiles.Insert(Math.Max(0, _settings.Profiles.Count - 1), profile);
        _settings.ActiveProfileId = profile.Id;
        LoadSettings();
        ApplyLanguage();
        ApplyTheme();
        _save();
    }

    private void RenameProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = _settings.Profiles.FirstOrDefault(candidate => candidate.Id == _settings.ActiveProfileId);
        if (profile == null || profile.Id == "native-zero")
        {
            return;
        }

        var zh = _settings.Language == "zh-CN";
        var current = zh ? profile.NameZh : profile.NameEn;
        var name = ThemedDialog.ShowInput(
            this,
            zh ? "重命名方案" : "Rename Profile",
            zh ? "输入新的方案名称：" : "Enter the new profile name:",
            current,
            zh ? "确定" : "OK",
            zh ? "取消" : "Cancel");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        profile.NameZh = name.Trim();
        profile.NameEn = name.Trim();
        RefreshProfileLabels();
        _save();
    }

    private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsBuiltInProfile(_settings.ActiveProfileId))
        {
            return;
        }

        var profile = _settings.Profiles.FirstOrDefault(candidate => candidate.Id == _settings.ActiveProfileId);
        if (profile == null)
        {
            return;
        }

        var zh = _settings.Language == "zh-CN";
        var confirmed = ThemedDialog.Confirm(
            this,
            zh ? "删除方案" : "Delete Profile",
            zh ? $"删除“{profile.NameZh}”？" : $"Delete \"{profile.NameEn}\"?",
            zh ? "删除" : "Delete",
            zh ? "取消" : "Cancel");
        if (!confirmed)
        {
            return;
        }

        _settings.Profiles.Remove(profile);
        var next = _settings.Profiles.First(candidate => candidate.Id != "native-zero");
        _settings.ApplyProfile(next);
        LoadSettings();
        ApplyLanguage();
        ApplyTheme();
        _save();
    }

    private static bool IsBuiltInProfile(string profileId) =>
        profileId is "smoothscroll-a" or "quick-b" or "native-zero";

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var defaults = AppSettings.CreateDefault();
        _settings.StepSize = defaults.StepSize;
        _settings.AnimationTimeMs = defaults.AnimationTimeMs;
        _settings.AccelerationDeltaMs = defaults.AccelerationDeltaMs;
        _settings.AccelerationMax = defaults.AccelerationMax;
        _settings.PulseAlgorithm = defaults.PulseAlgorithm;
        _settings.PulseScale = defaults.PulseScale;
        _settings.PulseNormalize = defaults.PulseNormalize;
        _settings.LinesToScroll = defaults.LinesToScroll;
        _settings.ReverseDirection = defaults.ReverseDirection;
        _settings.HorizontalSmoothing = defaults.HorizontalSmoothing;
        _settings.HorizontalShiftKey = defaults.HorizontalShiftKey;
        LoadSettings();
        ApplyLanguage();
        ApplyTheme();
        _save();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateWindowChromeShape();

    private void WindowOutline_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateWindowChromeShape();

    private void WindowFrame_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateWindowChromeShape();

    private void Window_StateChanged(object? sender, EventArgs e) => UpdateWindowChromeShape();

    private void UpdateWindowChromeShape()
    {
        if (WindowOutline is null || WindowFrame is null || WindowStroke is null)
        {
            return;
        }

        var radius = WindowState == WindowState.Maximized ? 0 : WindowCornerRadius;
        var strokeRadius = Math.Max(0, radius - 0.5);

        WindowOutline.CornerRadius = new CornerRadius(radius);
        WindowOutline.Clip = null;
        WindowFrame.CornerRadius = new CornerRadius(radius);
        WindowFrame.Clip = null;
        WindowStroke.RadiusX = strokeRadius;
        WindowStroke.RadiusY = strokeRadius;
        WindowStroke.Visibility = WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
        ApplyDwmFrame();
        ClearNativeWindowRegion();
    }

    private void ApplyDwmFrame()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var enabled = 1;
            var corner = WindowState == WindowState.Maximized ? 1 : 2;
            DwmSetWindowAttribute(helper.Handle, DwmwaNcRenderingPolicy, ref enabled, sizeof(int));
            DwmSetWindowAttribute(helper.Handle, DwmwaWindowCornerPreference, ref corner, sizeof(int));

            var margins = new Margins();
            DwmExtendFrameIntoClientArea(helper.Handle, ref margins);
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private void ClearNativeWindowRegion()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            SetWindowRgn(helper.Handle, IntPtr.Zero, true);
        }
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);

    private const int DwmwaNcRenderingPolicy = 2;
    private const int DwmwaWindowCornerPreference = 33;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }
}

