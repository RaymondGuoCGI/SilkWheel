# SilkWheel

[English](#english) | [中文](#中文)

## English

SilkWheel is a Windows tray utility that turns stepped mouse wheel input into a calmer, smoother, more controllable glide.

The default feel is tuned from a SmoothScroll-style profile:

- Step size: 120
- Animation time: 540 ms
- Acceleration delta: 50 ms
- Max acceleration: 7x
- Pulse easing: enabled
- Pulse scale: 3
- Lines per notch: 1

### Free Beta

SilkWheel is currently a free beta. Use it free for 21 days. After that, share one real feedback note to keep using the current beta version.

- Website: https://silkwheel.raymondstudio.cn/
- Public feedback: https://github.com/RaymondGuoCGI/SilkWheel-Feedback/issues
- Website feedback form: saved by the SilkWheel VPS feedback API

Maintainer feedback panel:

`https://silkwheel.raymondstudio.cn/admin/feedback`

The admin panel is protected by HTTP Basic Auth. Credentials are configured outside the repository.

### Support Development

Optional support is welcome, but it is separate from beta access and never required.

- PayPal: https://paypal.me/raymondguocgi
- Quick support: [$5](https://paypal.me/raymondguocgi/5USD) / [$10](https://paypal.me/raymondguocgi/10USD) / [$20](https://paypal.me/raymondguocgi/20USD) / [As you like](https://paypal.me/raymondguocgi)

### Run

Published executable:

`bin\Release\net8.0-windows\win-x64\publish\SilkWheel.exe`

Double-clicking SilkWheel opens the settings window and keeps the app running from the system tray. Left-click the tray icon to open settings. Right-click it to enable, pause, or exit.

### Install Locally

```powershell
.\Install-SilkWheel.ps1
```

The installer copies SilkWheel to `%LocalAppData%\Programs\SilkWheel`, creates a Start Menu shortcut, and starts the app. Start-with-Windows uses `--background` so login launch goes straight to the tray without popping the settings window.

### Settings

Settings are stored at:

`%AppData%\SilkWheel\settings.json`

Excluded apps are empty by default. Add apps from Settings when a specific program should keep its native wheel behavior.

### Build

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

On the VPS, website feedback submissions are stored at:

`/var/lib/silkwheel-feedback/feedback.jsonl`

To inspect recent feedback:

```bash
tail -n 20 /var/lib/silkwheel-feedback/feedback.jsonl
```

---

## 中文

SilkWheel 是一款 Windows 托盘工具，用来把生硬的一格一格鼠标滚轮输入，变成更自然、更稳定、更可控的惯性滑动。

默认手感来自 SmoothScroll 风格参数调校：

- 单次滚动强度：120
- 动画时长：540 ms
- 加速窗口：50 ms
- 最大加速倍数：7x
- Pulse 缓动：开启
- Pulse 强度：3
- 每格滚动行数：1

### 免费 Beta

SilkWheel 目前处于免费公测阶段。你可以免费使用 21 天；到期后，只需要提交一次真实使用反馈，就可以继续使用当前 Beta 版本。

- 官网：https://silkwheel.raymondstudio.cn/
- 公开反馈区：https://github.com/RaymondGuoCGI/SilkWheel-Feedback/issues
- 官网反馈表单：会直接保存到 SilkWheel 的 VPS 反馈接口

维护者反馈面板：

`https://silkwheel.raymondstudio.cn/admin/feedback`

管理面板受 HTTP Basic Auth 保护，账号信息不提交到仓库。

### 支持开发

如果 SilkWheel 改善了你的日常滚动体验，欢迎通过微信自愿支持开发。打赏和 Beta 使用、解锁完全分开，不是强制要求。

![微信收款码](website/assets/wechat-support-qr.png)

### 运行

发布后的可执行文件：

`bin\Release\net8.0-windows\win-x64\publish\SilkWheel.exe`

双击 SilkWheel 会打开设置窗口，同时程序会常驻系统托盘。左键点击托盘图标打开设置；右键可以启用、暂停或退出。

### 本地安装

```powershell
.\Install-SilkWheel.ps1
```

安装脚本会把 SilkWheel 复制到 `%LocalAppData%\Programs\SilkWheel`，创建开始菜单快捷方式并启动程序。开机启动会使用 `--background`，登录后直接进入托盘，不会自动弹出设置窗口。

### 设置

设置文件保存位置：

`%AppData%\SilkWheel\settings.json`

排除应用默认为空。遇到某个软件需要保留原生滚轮行为时，可以在设置里手动添加。

### 构建

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

VPS 上的官网反馈数据保存位置：

`/var/lib/silkwheel-feedback/feedback.jsonl`

查看最近反馈：

```bash
tail -n 20 /var/lib/silkwheel-feedback/feedback.jsonl
```
