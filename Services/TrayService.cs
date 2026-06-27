using Forms = System.Windows.Forms;

namespace SilkWheel.Services;

public sealed class TrayService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Action _showSettings;
    private readonly Action _toggleEnabled;
    private readonly Action _exit;
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _toggleItem = new();
    private readonly Forms.ToolStripMenuItem _settingsItem = new();
    private readonly Forms.ToolStripMenuItem _exitItem = new();

    public TrayService(AppSettings settings, Action showSettings, Action toggleEnabled, Action exit)
    {
        _settings = settings;
        _showSettings = showSettings;
        _toggleEnabled = toggleEnabled;
        _exit = exit;

        var menu = new Forms.ContextMenuStrip();
        _toggleItem.Click += (_, _) => _toggleEnabled();
        _settingsItem.Click += (_, _) => _showSettings();
        _exitItem.Click += (_, _) => _exit();
        menu.Items.AddRange(new Forms.ToolStripItem[] { _toggleItem, _settingsItem, new Forms.ToolStripSeparator(), _exitItem });

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                _showSettings();
            }
        };
    }

    public void Update()
    {
        var zh = _settings.Language == "zh-CN";
        _icon.Text = _settings.Enabled ? "SilkWheel - On" : "SilkWheel - Off";
        _toggleItem.Text = _settings.Enabled
            ? (zh ? "暂停" : "Pause")
            : (zh ? "启用" : "Enable");
        _settingsItem.Text = zh ? "设置" : "Settings";
        _exitItem.Text = zh ? "退出" : "Exit";
    }

    public void ShowLaunchTip()
    {
        var zh = _settings.Language == "zh-CN";
        _icon.BalloonTipTitle = "SilkWheel";
        _icon.BalloonTipText = zh
            ? "设置窗口已打开。关闭窗口后程序仍会在托盘运行，左键托盘图标可再次打开。"
            : "Settings are open. Closing the window keeps SilkWheel running in the tray; left-click the tray icon to reopen.";
        _icon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _icon.ShowBalloonTip(4500);
    }

    public void ShowBackgroundTip()
    {
        var zh = _settings.Language == "zh-CN";
        _icon.BalloonTipTitle = "SilkWheel";
        _icon.BalloonTipText = zh
            ? "已在托盘后台运行。左键图标打开设置，右键可暂停或退出。"
            : "Running in the tray. Left-click to open settings; right-click to pause or exit.";
        _icon.BalloonTipIcon = Forms.ToolTipIcon.Info;
        _icon.ShowBalloonTip(3500);
    }

    public void ShowStateTip()
    {
        var zh = _settings.Language == "zh-CN";
        _icon.BalloonTipTitle = "SilkWheel";
        _icon.BalloonTipText = _settings.Enabled
            ? (zh ? "已启用丝滑滚轮。" : "Smooth scrolling enabled.")
            : (zh ? "已暂停，当前使用系统原生滚轮。" : "Paused. Native wheel scrolling is active.");
        _icon.BalloonTipIcon = _settings.Enabled ? Forms.ToolTipIcon.Info : Forms.ToolTipIcon.Warning;
        _icon.ShowBalloonTip(2500);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _toggleItem.Dispose();
        _settingsItem.Dispose();
        _exitItem.Dispose();
    }

    private static System.Drawing.Icon LoadIcon()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe))
        {
            var icon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
            if (icon != null)
            {
                return icon;
            }
        }

        return System.Drawing.SystemIcons.Application;
    }
}
