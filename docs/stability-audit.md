# Key Disabler Stability Audit

## Current safe scope

The current safe scope is captured key rules only:

```text
Detect keyboard -> Capture Key -> Save Captured Rule
```

Full keyboard control is paused in the UI and enforcement service until a timed verification workflow is implemented.

## Fixes applied

- Removed the unsafe global keyboard hook fallback.
- Prevented Raw Input devices from being saved as enforceable rule targets.
- Hardened the Interception worker loop so errors are captured instead of silently breaking the rule engine.
- De-duplicated saved key rules before loading them into the active rule table.
- Paused full keyboard rule enforcement in the device blocker. Existing full-keyboard rules may remain in settings for display/removal, but they are not applied by the service.
- Changed the UI full-keyboard disable action into a safety message instead of saving an active disable rule.
- Kept rule matching limited to exact device slot + scan code + extended-key flag.
- Paused the blocker when no active per-key or remap rules exist.
- Kept Raw Input detection available while the blocker is paused.
- Changed installer defaults so the experimental driver and Windows startup shortcut are unchecked.
- Added Start Menu recovery shortcuts for settings reset and driver uninstall.
- Added emergency recovery documentation.

## Required test workflow

1. Delete old settings once: `%APPDATA%\KeyDisabler\settings.json`.
2. Install the latest build.
3. Keep the experimental driver unchecked unless a driver test is needed.
4. Open Key Disabler.
5. Click `Detect by key press` and press a key from the target keyboard.
6. Go to Rules.
7. Click `Capture Key`.
8. Press the exact key from the selected keyboard.
9. Click `Save Captured Rule` or `Add Key Rule`.
10. Test the selected keyboard and another keyboard separately.

## Known paused feature

Full keyboard control needs a safe timed test flow before reactivation:

```text
Select keyboard -> temporary test -> auto restore -> user confirms -> save rule
```

Do not re-enable full keyboard enforcement until this flow exists.

## Emergency recovery

Use `docs/emergency-recovery.md` if a local build blocks input or a driver uninstall requires manual cleanup.

## Next improvement areas

- Add a driver status page.
- Add a visible logs page with device id, hardware id, scan code, extended flag, and rule result.
- Add pause/resume all rules in the tray menu.
- Split UI and enforcement into a background service for true restart persistence.
- Add a timed full-keyboard disable test flow with automatic restore.
