# Release Checklist

Run this before sharing a new installer build.

## Installer checks

- Startup shortcut is unchecked by default.
- Experimental low-level input option is unchecked by default.
- Start Menu contains the main app, reset settings shortcut, cleanup shortcut, and app uninstall shortcut.

## App checks

- First launch shows the blocker as paused when no active key rules exist.
- Full device control shows a safety message and does not save an active full-device rule.
- Detection still works while the blocker is paused.
- Detection-only devices cannot be saved as enforceable rules.

## Per-key rule checks

- Detect the target keyboard.
- Capture the broken key.
- Save the captured key rule.
- Confirm that the selected keyboard key is blocked.
- Confirm that the same key on a second keyboard still works.
- Remove the rule.
- Confirm that the key works again.

## Cleanup check

- Run the cleanup shortcut as Administrator.
- Restart Windows.
- Confirm input behavior is normal.
