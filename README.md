# Key Disabler

Key Disabler is a Windows standalone desktop utility for keyboard detection, full selected-keyboard disabling, saved device-specific key rules, tray behavior, startup restore, and installable packaging.

## Current phase

This repository currently contains:

- WPF desktop app
- device-specific keyboard listing through the Interception driver
- full selected-keyboard disable / enable workflow
- exact device + key rule management
- local JSON settings storage
- system tray support
- Windows startup setting
- persistent saved keyboard disables and key rules
- GitHub Actions build for portable app and installer
- Inno Setup installer

## Full keyboard disable behavior

Full keyboard disables are applied as:

```text
selected physical keyboard = disabled
```

Example:

```text
Keyboard 1 laptop keyboard = disabled
Keyboard 2 USB keyboard = allowed
Keyboard 3 Bluetooth keyboard = allowed
Keyboard 4 another USB keyboard = allowed
```

The selected keyboard stays disabled until it is enabled again in the app. Saved disabled-keyboard settings are restored after restart/login when the app starts with Windows.

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

Full-keyboard disable rules are checked before key rules. If a keyboard is fully disabled, all keys from that keyboard are blocked.

This requires the Interception driver and `interception.dll`, which are bundled into the build artifact by GitHub Actions.

## Build artifacts

GitHub Actions publishes two artifacts:

```text
KeyDisabler-portable-win-x64
KeyDisabler-installer-win-x64
```

Use the installer artifact for normal Windows installation.

## Installer features

The installer creates:

- Program Files installation
- Start Menu shortcut
- optional Desktop shortcut
- optional startup shortcut
- optional device-level keyboard driver installation
- Windows Apps & Features / Control Panel uninstall entry

Installer output:

```text
KeyDisablerSetup.exe
```

## Important note

After installing the device-level driver, Windows may require a restart before the blocker works correctly.

## Safety note

The app prevents disabling the last enabled detected keyboard. Keep at least one keyboard enabled so you can recover easily.

## Local portable build

```bash
dotnet publish src/KeyDisabler.App/KeyDisabler.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Portable output:

```text
src/KeyDisabler.App/bin/Release/net10.0-windows/win-x64/publish/KeyDisabler.exe
```
