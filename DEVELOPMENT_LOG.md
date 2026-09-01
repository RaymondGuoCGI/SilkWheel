# SilkWheel Development Log

This file keeps product ideas, tuning notes, and future work that should not be lost while SilkWheel is still in beta.

## 2026-09-01 - Website release center

- Added a bilingual Releases page backed by `website/releases.json`.
- Homepage download links now resolve to the latest release package.
- Release history records date, package type, size, SHA256, notes, and GitHub Release URL.
- The private feedback dashboard now groups estimated downloads by package filename and keeps raw request diagnostics separate.
- Deployment keeps versioned installers in the VPS `download/` directory and never deletes feedback data or authentication configuration.

## 2026-09-01 - Formal Windows installer

- Bumped the beta version to `0.1.0-beta.2` / file version `0.1.0.2`.
- Added a reproducible self-contained publish and Inno Setup build script.
- Added a stable installer AppId so future packages upgrade the same installation.
- The installer uses the existing per-user install directory, preserves settings under `%APPDATA%\SilkWheel`, and removes the startup entry on uninstall.
- Validated legacy in-place upgrade, repeated upgrade, uninstall, and reinstall on Windows: the running copy closed normally, settings hashes stayed unchanged, the uninstall entry was registered, and the new build restarted successfully.

## Backlog

### Per-App Profiles

Status: scoped follow-up from GitHub Issue #2
Priority: high for precision and app compatibility

Goal:

Let a foreground application automatically use a saved scroll profile while unmatched applications continue to use the current global settings.

Safety requirements:

- Keep the existing exclusion list higher priority than profile assignments.
- Match an exact executable path first, with executable-name fallback when the path cannot be read.
- Snapshot the selected profile when a wheel event is queued so an active animation cannot change parameters mid-tail.
- Reset acceleration and cancel the previous tail when the foreground target changes, preventing one application's scroll from leaking into another.
- Fall back to the global settings when an assignment references a missing profile.

Validation plan:

- Bind two applications to visibly different saved profiles and verify automatic switching.
- Confirm excluded applications still receive native wheel input.
- Switch applications during an active tail and verify the old tail is cancelled.
- Restart SilkWheel and verify assignments persist without changing existing settings files.

### Smart Brake / Precision Mode

Status: idea captured  
Priority: high for scroll-feel refinement

Problem:

SilkWheel currently focuses on making mouse wheel scrolling smooth and inertial. This feels good when browsing long pages, lists, and documents, but sometimes the user needs to slow down quickly and stop precisely while reading detailed content. In those moments, too much glide can feel like overshooting.

Goal:

Keep the smooth long-tail feel for normal browsing, while giving the user an easy way to "brake" and enter a slower, more precise scroll state when needed.

Possible interaction designs:

- Hold a modifier key such as `Ctrl`, `Alt`, or `Shift` to temporarily reduce momentum and animation duration.
- Detect small reverse wheel input during an active scroll tail and treat it as a brake instead of immediately starting a strong opposite-direction glide.
- Add a "Precision while reading" profile option that automatically shortens the tail after low-speed wheel input.
- Add a tray/settings toggle for "Smart brake" with adjustable brake strength.
- Support a hotkey for instant native-wheel mode while held.

Initial implementation direction:

- Add new settings:
  - `SmartBrakeEnabled`
  - `BrakeModifierKey`
  - `BrakeStrength`
  - `ReverseBrakeThreshold`
- In `ScrollEngine`, when a brake condition is active, reduce existing velocity/momentum instead of adding a new full animation.
- When direction reverses during the tail, decay or cancel the current tail more aggressively.
- Keep Profile A/B/Zero behavior unchanged unless Smart Brake is enabled.

UX notes:

- This should not make normal browsing slower.
- The feature should feel like "I can stop exactly where I want", not like the app randomly loses smoothness.
- The settings UI should explain the behavior with short labels, not long instructional text.

Validation plan:

- Test on long webpages, GitHub pages, docs, file explorer, and text editors.
- Compare normal browsing against detailed reading sections where the user wants to stop on a specific paragraph.
- Verify that quick repeated scrolling still accelerates naturally.
- Verify that a small reverse wheel input near the end of a glide stops the movement cleanly without wobble.
