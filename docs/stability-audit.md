# Key Disabler Stability Audit

## Current safe scope

The current safe scope is captured key rules only:

```text
Detect keyboard -> Capture Key -> Save Captured Rule
```

Full keyboard control is paused in the enforcement service until a timed verification workflow is implemented.

## Fixes applied

- Removed the unsafe global keyboard hook fallback.
- Prevented Raw Input devices from becoming saved/enforceable rule targets.
- Hardened the Interception worker loop so errors are captured instead of silently breaking the rule engine.
- De-duplicated saved key rules before loading them into the active rule table.
- Paused full keyboard rule enforcement in the device blocker. Existing full-keyboard rules may remain in settings for display/removal, but they are not applied by the service.
- Kept rule matching limited to exact device slot + scan code + extended-key flag.

## Required test workflow

1. Delete old settings once: `%APPDATA%\KeyDisabler\settings.json`.
2. Install the latest build.
3. Restart Windows after driver installation.
4. Open Key Disabler.
5. Click `Detect by key press` and press a key from the target keyboard.
6. Go to Rules.
7. Click `Capture Key`.
8. Press the exact key from the selected keyboard.
9. Click `Save Captured Rule`.
10. Test the selected keyboard and another keyboard separately.

## Known paused feature

Full keyboard control needs a safe timed test flow before reactivation:

```text
Select keyboard -> temporary test -> auto restore -> user confirms -> save rule
```

Do not re-enable full keyboard enforcement until this flow exists.

## Next improvement areas

- Add a driver status page.
- Add a visible logs page with device id, hardware id, scan code, extended flag, and rule result.
- Add reset settings shortcut in the installer.
- Add pause/resume all rules in the tray menu.
- Split UI and enforcement into a background service for true restart persistence.
