# Security and Privacy

SilkWheel is a system-level mouse wheel utility, so it is reasonable to ask what it can see and what it does.

## What SilkWheel Hooks

SilkWheel uses the Windows low-level mouse hook API:

```text
WH_MOUSE_LL
```

The hook is used to detect mouse wheel events (`WM_MOUSEWHEEL` and `WM_MOUSEHWHEEL`). When smoothing is enabled, SilkWheel can swallow the original wheel event and emit smaller wheel deltas with `SendInput`.

## What SilkWheel Does Not Do

- It does not record keyboard input.
- It does not upload mouse input, app usage, settings, or telemetry.
- It does not install a driver.
- It does not require a background service.
- It does not modify system files.

## Local Data

Settings are stored locally at:

```text
%AppData%\SilkWheel\settings.json
```

Start-with-Windows uses the current user's registry Run key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

## Network Access

The desktop app itself does not need network access for scrolling. Feedback is submitted only when you intentionally use the website feedback form or GitHub Issues.

## Unsigned Beta Warning

This early beta is not code-signed yet, so Windows or browsers may warn before download or launch. This is expected for new unsigned Windows desktop software. The source code is public so users can inspect the behavior before running it.

## Reporting Concerns

Please report security or privacy concerns through GitHub Issues or the website feedback form:

- https://github.com/RaymondGuoCGI/SilkWheel/issues
- https://silkwheel.raymondstudio.cn/#feedback
