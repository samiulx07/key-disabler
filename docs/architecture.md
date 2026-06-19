# Architecture

## Goal

Key Disabler is a Windows standalone desktop app for managing keyboard-specific key rules.

The target behavior is:

```text
Laptop keyboard + Space = handled by the app rule
External keyboard + Space = still works normally
```

## Current implementation

Version 0.1 contains the desktop foundation:

- WPF app shell
- Raw Input keyboard enumeration
- Raw Input key press detection
- Local JSON settings file
- Rule list management
- System tray integration
- Windows startup setting using the current user registry Run key
- GitHub Actions Windows build

## Current limitation

The desktop app can detect which keyboard produced a key and can save rules. It does not yet enforce rules system-wide.

Windows user-mode apps can listen to Raw Input device events, but reliable per-device keyboard filtering requires a lower-level component.

## Final architecture

```text
KeyDisabler.App
  WPF UI, tray, settings, device detection

KeyDisabler.Service
  Runs in background, loads saved rules, communicates with driver

KeyDisabler.Driver
  KMDF keyboard filter driver, enforces per-device key rules
```

## Settings path

```text
%APPDATA%\KeyDisabler\settings.json
```

Example:

```json
{
  "startWithWindows": true,
  "minimizeToTray": true,
  "rules": [
    {
      "deviceId": "...",
      "deviceName": "Built-in / Laptop Keyboard",
      "virtualKey": 32,
      "keyName": "Space",
      "isEnabled": true
    }
  ]
}
```
