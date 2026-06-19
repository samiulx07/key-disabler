# Key Disabler

Key Disabler is a Windows standalone desktop utility for keyboard detection, saved key rules, tray behavior, startup restore, and installable packaging.

## Current phase

This repository currently contains:

- WPF desktop app
- Raw Input keyboard device detection
- Device + key rule management
- Local JSON settings storage
- System tray support
- Windows startup setting
- Active key blocking fallback while the app is running
- GitHub Actions build for portable app and installer
- Inno Setup installer

## Current limitation

The current blocker is a working global fallback. If you block Space, Space is blocked while the app is running. This does not yet keep the same key working on an external keyboard.

True per-device-only blocking, for example laptop Space blocked while external Space works, requires a low-level driver or service layer. That driver layer will be added in a later phase.

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
- Windows Apps & Features / Control Panel uninstall entry

Installer output:

```text
KeyDisablerSetup.exe
```

## Local portable build

```bash
dotnet publish src/KeyDisabler.App/KeyDisabler.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Portable output:

```text
src/KeyDisabler.App/bin/Release/net10.0-windows/win-x64/publish/KeyDisabler.exe
```
