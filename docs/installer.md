# Installer

Key Disabler uses Inno Setup to generate a normal Windows installer.

## Installer output

GitHub Actions publishes two artifacts:

- `KeyDisabler-portable-win-x64`: portable app files
- `KeyDisabler-installer-win-x64`: installable setup file

The installer artifact contains:

```text
KeyDisablerSetup.exe
```

## Installer features

The installer:

- installs Key Disabler into `Program Files`
- creates a Start Menu shortcut
- optionally creates a Desktop shortcut
- optionally starts the app with Windows
- adds a normal uninstall entry in Windows Apps & Features / Control Panel
- launches the app after setup if selected

## Uninstall

Users can uninstall from:

```text
Windows Settings → Apps → Installed apps → Key Disabler → Uninstall
```

or:

```text
Control Panel → Programs and Features → Key Disabler → Uninstall
```
