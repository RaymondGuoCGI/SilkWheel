# SilkWheel Development Log

This file keeps product ideas, tuning notes, and future work that should not be lost while SilkWheel is still in beta.

## Backlog

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
