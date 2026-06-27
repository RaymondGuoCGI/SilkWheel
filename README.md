# SilkWheel

SilkWheel is a Windows tray utility that adds SmoothScroll-style smooth mouse wheel scrolling system-wide.

The default feel is based on the SmoothScroll 1.2.4 settings found on this machine:

- Step size: 120
- Animation time: 540 ms
- Acceleration delta: 50 ms
- Max acceleration: 7x
- Pulse easing: enabled
- Pulse scale: 3
- Lines per notch: 1

## Run

Published executable:

`bin\Release\net8.0-windows\win-x64\publish\SilkWheel.exe`

Double-clicking SilkWheel opens the settings window and keeps the app running from the system tray. Left-click the tray icon to open settings. Right-click it to enable, pause, or exit.

## Beta, Feedback, and Support

SilkWheel is currently a free beta. Use it for 21 days; after that, share one real feedback note to keep using the current beta.

- Website: https://silkwheel.raymondstudio.cn/
- Feedback: https://github.com/RaymondGuoCGI/SilkWheel-Feedback/issues
- Website feedback form: saved by the SilkWheel VPS feedback API

On the VPS, feedback submissions are stored at:

`/var/lib/silkwheel-feedback/feedback.jsonl`

To inspect recent feedback:

```bash
tail -n 20 /var/lib/silkwheel-feedback/feedback.jsonl
```

Optional support is welcome, but it is separate from beta access and never required.

- PayPal: https://paypal.me/raymondguocgi
- PayPal quick tips: [$5](https://paypal.me/raymondguocgi/5USD) / [$10](https://paypal.me/raymondguocgi/10USD) / [$20](https://paypal.me/raymondguocgi/20USD)

For WeChat support, scan the QR code:

![WeChat support QR](website/assets/wechat-support-qr.png)

## Install Locally

```powershell
.\Install-SilkWheel.ps1
```

The installer copies SilkWheel to `%LocalAppData%\Programs\SilkWheel`, creates a Start Menu shortcut, and starts the app. Start-with-Windows uses `--background` so login launch goes straight to the tray without popping the settings window.

## Settings

Settings are stored at:

`%AppData%\SilkWheel\settings.json`

Excluded apps are empty by default. Add apps from Settings when a specific program should keep its native wheel behavior.

## Build

```powershell
dotnet build
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
