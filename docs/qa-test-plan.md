# Key Disabler QA Test Plan

Use this checklist before treating a build as release-ready.

## Install and startup

1. Install using `KeyDisablerSetup.exe`.
2. Keep `Install device-level keyboard driver` checked.
3. Restart Windows if the app says the driver is not ready.
4. Open Key Disabler from the Desktop shortcut.
5. Confirm it appears in Windows Apps & Features / Control Panel uninstall list.
6. Enable `Start with Windows` in Settings.
7. Restart Windows and confirm the app starts again.

## Device detection

1. Open the Keyboards tab.
2. Click Refresh.
3. Click Detect by key press.
4. Press a key on the laptop keyboard.
5. Confirm the detected keyboard is selected.
6. Repeat with each external keyboard.
7. Confirm each keyboard appears as a separate device.

## Device-specific key rule

1. Detect the laptop keyboard.
2. Add a `Space` key rule for the laptop keyboard.
3. Open Notepad.
4. Press laptop Space. Expected: blocked.
5. Press external keyboard Space. Expected: allowed.
6. Remove the rule.
7. Confirm laptop Space works again.

## Full keyboard disable

1. Detect the laptop keyboard.
2. Click `Disable Selected Keyboard`.
3. Press multiple keys on the laptop keyboard. Expected: blocked.
4. Press keys on the external keyboard. Expected: allowed.
5. Click `Enable Selected Keyboard`.
6. Confirm the laptop keyboard works again.

## Restart persistence

1. Create one key rule and one full keyboard disable.
2. Confirm both work before restart.
3. Restart Windows.
4. Confirm the app starts with Windows.
5. Confirm the same key rule and disabled keyboard still work.

## Safety recovery

1. Try to disable the only detected enabled keyboard.
2. Expected: app shows safety block and does not disable it.
3. Confirm at least one keyboard always remains usable.

## Troubleshooting data to capture

If a rule does not work, capture:

- screenshot of the Keyboards tab
- screenshot of the saved rules list
- screenshot of the full disabled keyboards list
- app status message
- whether the driver was installed
- whether Windows was restarted after driver installation
- whether the rule was created using Detect by key press
