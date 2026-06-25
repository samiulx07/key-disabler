# Emergency Recovery

Use this when a local test build blocks input or the Interception driver remains active after uninstall.

## Fast recovery

1. Open the Windows On-Screen Keyboard:

```text
Settings -> Accessibility -> Keyboard -> On-screen keyboard
```

2. Open Command Prompt as Administrator.
3. Run:

```bat
taskkill /IM KeyDisabler.exe /F
del "%APPDATA%\KeyDisabler\settings.json"
"%ProgramFiles%\Key Disabler\driver\install-interception.exe" /uninstall
shutdown /r /t 0
```

## If the installed driver path is missing

Run this from an Administrator Command Prompt:

```bat
powershell -NoProfile -ExecutionPolicy Bypass -Command "$z='$env:TEMP\Interception.zip'; $d='$env:TEMP\InterceptionFix'; Invoke-WebRequest 'https://github.com/oblitum/Interception/releases/download/v1.0.1/Interception.zip' -OutFile $z; Expand-Archive $z $d -Force; $exe=(Get-ChildItem $d -Recurse -Filter 'install-interception.exe' | Select-Object -First 1).FullName; & $exe /uninstall"
shutdown /r /t 0
```

## Safety behavior in current build

- The installer does not install the experimental device-level driver by default.
- The Windows startup shortcut is not checked by default.
- Full-keyboard disable is paused and is not enforced.
- The blocker only starts when captured per-key rules or remap rules exist.
- The Start Menu contains recovery shortcuts for resetting settings and uninstalling the device driver.
