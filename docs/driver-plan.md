# Driver Plan

## Why a driver is needed

Raw Input can identify which physical keyboard produced a key event. That is useful for detection and rule creation.

However, true system-wide filtering of one key from one physical keyboard is not reliable from a normal desktop app alone. For production behavior, the application needs a low-level keyboard filter layer.

## Planned approach

### Phase 1

Keep the WPF app as the control panel:

- list keyboards
- identify keyboard by key press
- save rules
- manage startup behavior

### Phase 2

Add a background service:

- starts with Windows
- loads `%APPDATA%\KeyDisabler\settings.json`
- communicates rule changes to the enforcement layer

### Phase 3

Add a KMDF keyboard filter driver:

- receives keyboard packets
- identifies source device
- checks saved rules
- suppresses matching key events
- passes all other input normally

## Safety rules

The final enforcement layer must include:

- emergency reset shortcut
- never block every keyboard without confirmation
- tray pause option
- safe mode fallback
- rule validation before saving
- restore defaults option

## Suggested driver reference

Use Microsoft's keyboard filter sample architecture as the technical reference for the production driver implementation.
