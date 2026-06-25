# Installer

Key Disabler uses Inno Setup to generate a normal Windows installer.

## Installer output

GitHub Actions publishes two artifacts:

- `KeyDisabler-portable-win-x64`: portable app files
- `KeyDisabler-installer-win-x64`: installable setup file

GitHub Actions artifact downloads are always zip-wrapped. The installer artifact contains:

```text
KeyDisablerSetup.exe
```

## Installer features

The installer:

- installs Key Disabler into `Program Files`
- creates a Start Menu shortcut
- creates a Start Menu reset-settings shortcut
- creates a Start Menu device-driver uninstall shortcut
- optionally creates a Desktop shortcut
- optionally starts the app with Windows, unchecked by default
- optionally installs the experimental device-level keyboard driver, unchecked by default
- adds a normal uninstall entry in Windows Apps & Features / Control Panel
- launches the app after setup if selected

## Driver safety

The experimental device-level driver is not installed by default. When it is installed or uninstalled, Windows may need a restart before keyboard and mouse behavior fully returns to normal.

## Uninstall

Users can uninstall from:

```text
Windows Settings -> Apps -> Installed apps -> Key Disabler -> Uninstall
```

or:

```text
Control Panel -> Programs and Features -> Key Disabler
```

After uninstall, restart Windows if the driver was installed.

## Emergency recovery

See `docs/emergency-recovery.md` for the recovery commands.
