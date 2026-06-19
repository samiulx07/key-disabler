# Key Disabler

Key Disabler is a Windows standalone desktop utility for detecting multiple keyboards, saving per-device key disable rules, and preparing the foundation for real per-device key blocking.

## Current phase

This repository currently contains the desktop application foundation:

- WPF desktop app
- Raw Input keyboard device detection
- Device + key rule management
- Local JSON settings storage
- System tray support
- GitHub Actions build that publishes a self-contained Windows executable

## Important limitation

The first version focuses on UI, keyboard detection, and saved rules. True system-wide per-device key blocking requires a low-level driver or service layer. That driver layer will be added in a later phase.

## Build

The GitHub Actions workflow publishes a self-contained Windows x64 build artifact.

Local build command:

```bash
dotnet publish src/KeyDisabler.App/KeyDisabler.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Output:

```text
src/KeyDisabler.App/bin/Release/net10.0-windows/win-x64/publish/KeyDisabler.exe
```
