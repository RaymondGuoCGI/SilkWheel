using SilkWheel.Services;
using System.Windows;

namespace SilkWheel;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceName = "SilkWheel.SingleInstance";
    private const string ShowSettingsEventName = "SilkWheel.ShowSettings";

    private Mutex? _mutex;
    private EventWaitHandle? _showSettingsEvent;
    private RegisteredWaitHandle? _showSettingsWait;
    private SettingsStore? _settingsStore;
    private AppSettings? _settings;
    private ScrollEngine? _scrollEngine;
    private MouseHookService? _mouseHook;
    private TrayService? _tray;
    private MainWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, SingleInstanceName, out var createdNew);
        if (!createdNew)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(
            _showSettingsEvent,
            (_, _) => Dispatcher.Invoke(() =>
            {
                ShowSettingsWindow();
                _tray?.ShowLaunchTip();
            }),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        StartupService.SetEnabled(_settings.StartWithWindows);

        _scrollEngine = new ScrollEngine(_settings);
        _mouseHook = new MouseHookService(_settings, _scrollEngine);
        _mouseHook.Start();

        _tray = new TrayService(_settings, ShowSettingsWindow, ToggleEnabled, ExitApplication);
        _tray.Update();

        var startInBackground = e.Args.Any(arg => string.Equals(arg, "--background", StringComparison.OrdinalIgnoreCase));
        if (_settings.FirstRun || !startInBackground)
        {
            _settings.FirstRun = false;
            SaveSettings();
            ShowSettingsWindow();
            _tray.ShowLaunchTip();
        }
        else
        {
            _tray.ShowBackgroundTip();
        }
    }

    public void SaveSettings()
    {
        if (_settingsStore == null || _settings == null)
        {
            return;
        }

        StartupService.SetEnabled(_settings.StartWithWindows);
        _settingsStore.Save(_settings);
        _tray?.Update();
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var showEvent = EventWaitHandle.OpenExisting(ShowSettingsEventName);
            showEvent.Set();
        }
        catch
        {
        }
    }

    private void ShowSettingsWindow()
    {
        if (_settings == null)
        {
            return;
        }

        if (_settingsWindow == null)
        {
            _settingsWindow = new MainWindow(_settings, SaveSettings);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ToggleEnabled()
    {
        if (_settings == null)
        {
            return;
        }

        _settings.Enabled = !_settings.Enabled;
        SaveSettings();
        _tray?.ShowStateTip();
    }

    private void ExitApplication()
    {
        _mouseHook?.Dispose();
        _scrollEngine?.Dispose();
        _tray?.Dispose();
        _showSettingsWait?.Unregister(null);
        _showSettingsEvent?.Dispose();
        _mutex?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mouseHook?.Dispose();
        _scrollEngine?.Dispose();
        _tray?.Dispose();
        _showSettingsWait?.Unregister(null);
        _showSettingsEvent?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
