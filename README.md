# Key Disabler

Key Disabler is a Windows standalone desktop utility for keyboard detection, saved device-specific key rules, tray behavior, startup restore, and installable packaging.

## Current safe scope

The current safe workflow is captured key rules only:

```text
Detect keyboard -> Capture Key -> Save Captured Rule
```

This means a selected key can be blocked on one physical keyboard while the same key still works on another keyboard.

## Safety changes

To prevent input lockout:

- full-keyboard disable is paused and is not enforced
- the blocker does not start automatically when there are no active per-key rules
- Raw Input devices are detection-only and cannot be saved as enforceable rules
- the installer does not install the experimental device-level driver by default
- the Windows startup shortcut is unchecked by default
- recovery shortcuts are added to the Start Menu

## Device-specific key behavior

Key rules are applied as:

```text
selected keyboard + selected key
```

Example:

```text
Keyboard 1 + Space = blocked
Keyboard 2 + Space = allowed
Keyboard 3 + Space = allowed
Keyboard 4 + Space = allowed
```

Rules are matched with device identity plus scan code and extended-key state.

## Paused feature

Full selected-keyboard disable is intentionally paused until a timed test and auto-restore workflow exists.

Do not re-enable full-keyboard enforcement without this safety flow:

```text
Select keyboard -> temporary test -> auto restore -> user confirms -> save rule
```

## Build artifacts

GitHub Actions publishes two artifacts:

```text
KeyDisabler-portable-win-x64
KeyDisabler-installer-win-x64
```

GitHub Actions artifact downloads are always zip-wrapped. The installer artifact contains:

```text
KeyDisablerSetup-win-x64.exe
```

Manual workflow runs also publish prerelease assets for direct downloads:

```text
KeyDisablerSetup-win-x64.exe
KeyDisabler-portable-win-x64.zip
```

## Installer features

The installer creates:

- Program Files installation
- Start Menu shortcut
- reset settings shortcut
- uninstall device driver shortcut
- optional Desktop shortcut
- optional startup shortcut, unchecked by default
- optional experimental device-level keyboard driver installation, unchecked by default
- Windows Apps & Features / Control Panel uninstall entry

## Emergency recovery

See [docs/emergency-recovery.md](docs/emergency-recovery.md).

Fast recovery command from an Administrator Command Prompt:

```bat
taskkill /IM KeyDisabler.exe /F
del "%APPDATA%\KeyDisabler\settings.json"
"%ProgramFiles%\Key Disabler\driver\install-interception.exe" /uninstall
shutdown /r /t 0
```

## Important note

After installing or uninstalling the device-level driver, Windows may require a restart before input behavior returns to normal.

## Local portable build

```bash
dotnet publish src/KeyDisabler.App/KeyDisabler.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Portable output:

```text
src/KeyDisabler.App/bin/Release/net10.0-windows/win-x64/publish/KeyDisabler.exe
```
