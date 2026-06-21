# Key Disabler - Complete Project Documentation

## 1. Project Overview

Key Disabler is a Windows desktop utility for controlling keyboard input at a device-specific level. The main goal is to let a user select a physical keyboard, capture a specific key from that keyboard, and block only that key from only that keyboard while allowing the same key on other keyboards to continue working.

The project was created because normal Windows key remapping tools usually work globally. They can disable Space, Enter, or another key everywhere, but they do not reliably distinguish between a laptop keyboard and an external keyboard. This application attempts to solve that by using device-level input interception.

Current safe scope:

```text
Detect keyboard -> Capture Key -> Save Captured Rule -> Block that key on that keyboard only
```

Full-keyboard disabling is visible in the UI and settings model, but enforcement is intentionally paused in the current service implementation because it previously caused unsafe lockout behavior. It should only be re-enabled after a timed verification and auto-restore workflow is implemented.

## 2. Main User Requirements

The project is being designed around these user requirements:

1. Disable a selected key only on a selected physical keyboard.
2. Keep the same key working on other keyboards.
3. Support laptop built-in keyboard and external USB/Bluetooth keyboards.
4. Never affect mouse, touchpad, or external mouse devices.
5. Persist saved rules after restart/login when startup is enabled.
6. Avoid multiple app instances and duplicate tray icons.
7. Provide a tray application experience.
8. Provide a safe recovery/reset flow.
9. Use the provided Key Disabler logo and icon assets.
10. Eventually support full keyboard disabling safely.

## 3. Current Tech Stack

### 3.1 Application Framework

- Language: C#
- Runtime: .NET 10 Windows target
- UI Framework: WPF
- Tray integration: Windows Forms NotifyIcon inside a WPF application
- Platform: Windows x64
- App type: Desktop application / WinExe

The project file targets `net10.0-windows`, enables WPF, enables Windows Forms for the tray icon, sets the app manifest, and defines the application icon path.

### 3.2 Input and Device Layer

The app uses two different input-related Windows approaches:

1. Interception driver wrapper
   - Used for real device-level keyboard interception.
   - Used to receive scan codes from a specific Interception keyboard slot.
   - Used to block or forward key strokes.

2. Raw Input
   - Used only as a fallback detection/diagnostic path when the driver is not ready.
   - It does not create enforceable saved rule targets.
   - It can detect key events, but it does not block keys.

### 3.3 Persistence

- Storage format: JSON
- Location: `%APPDATA%\KeyDisabler\settings.json`
- Stores:
  - Start with Windows setting
  - Minimize to tray setting
  - Captured key rules
  - Full keyboard rules, currently not enforced

### 3.4 Build and Installer

- CI/CD: GitHub Actions
- Build runner: `windows-latest`
- Build command: `dotnet publish`
- Output: self-contained win-x64 single-file app
- Installer: Inno Setup
- Driver package: Interception package downloaded in CI
- Artifacts:
  - Portable app folder
  - Inno Setup installer EXE

## 4. Repository Structure

```text
.github/workflows/build.yml
src/KeyDisabler.App/
  App.xaml
  App.xaml.cs
  MainWindow.xaml
  MainWindow.xaml.cs
  MainWindow.DeviceRefresh.cs
  MainWindow.HardRefresh.cs
  MainWindow.Branding.cs
  Models/
    AppSettings.cs
    KeyboardDevice.cs
    KeyboardRule.cs
    DisabledKeyboardRule.cs
  Services/
    DeviceKeyboardBlockerService.cs
    InterceptionNative.cs
    RawInputService.cs
    RawKeyEventArgs.cs
    DeviceKeyEventArgs.cs
    KeyNameResolver.cs
    SettingsService.cs
    TrayIconService.cs
    SingleInstanceService.cs
    BrandAssetService.cs
  Assets/
    AppIcon.ico.b64
src/KeyDisabler.Installer/
  KeyDisabler.iss
docs/
  stability-audit.md
  complete-project-documentation.md
```

## 5. Main Application Flow

### 5.1 Startup Flow

When the application starts:

1. `App.xaml.cs` runs first.
2. It checks whether the user passed `--clear-settings`.
3. If `--clear-settings` exists, the settings file is reset.
4. It creates a `SingleInstanceService`.
5. If another instance is already running, the new instance signals the old one and exits.
6. If it is the first instance, the normal WPF startup continues.

This prevents the bug where opening the app again from the Start Menu creates a second tray icon.

### 5.2 Main Window Load Flow

When `MainWindow` loads:

1. Settings are loaded from `%APPDATA%\KeyDisabler\settings.json`.
2. Saved key rules are loaded into memory.
3. Saved disabled keyboard rules are loaded into memory.
4. Default key options are added.
5. UI collections are bound to WPF controls.
6. Start with Windows and minimize to tray settings are restored.
7. The device blocker service starts.
8. Keyboard devices are refreshed.
9. Rules are pushed into the blocker service.
10. Raw Input is registered as fallback detection.
11. The tray icon service starts.

### 5.3 Device Detection Flow

The app supports several device detection paths:

1. Normal startup refresh
2. Manual Refresh button
3. Delayed startup refresh
4. Windows device-change events
5. Detect by key press

The user-facing detection flow is:

```text
Open app -> Keyboards tab -> Detect by key press -> press any key on target keyboard
```

When a device-level key event arrives, the app ensures that device exists in the UI list, selects it, rebinds saved rules to the current device identity, and updates the blocker service.

### 5.4 Key Capture Flow

The safest rule creation flow is:

```text
Select/detect keyboard -> Capture Key -> Press target key -> Save Captured Rule
```

When capture mode is active:

1. The user presses a key.
2. The device blocker receives the Interception key event.
3. The app checks whether the pressed key came from the selected keyboard.
4. If it came from another keyboard, it ignores the key.
5. If it came from the selected keyboard, it captures:
   - Device ID
   - Device hardware ID
   - Device display name
   - Scan code
   - Extended-key flag
   - Key display name
6. The captured key becomes selected in the key dropdown.
7. The user saves the rule.

### 5.5 Rule Enforcement Flow

The real enforcement happens inside `DeviceKeyboardBlockerService`:

1. The service creates an Interception context.
2. It filters only Interception keyboard slots 1 to 10.
3. It waits for keyboard events.
4. It receives the key stroke.
5. It resolves:
   - Interception device ID
   - Hardware ID
   - Scan code
   - Extended-key flag
   - Key name
6. It builds a rule key:

```text
DeviceId | ScanCode | IsExtendedKey
```

7. If a saved rule matches that exact key, the event is blocked.
8. If no rule matches, the event is forwarded back to Windows.

This exact-match model is the core behavior that makes device-specific key blocking possible.

## 6. Current Features Implemented

### 6.1 Device-Specific Captured Key Blocking

The main implemented feature is captured-key blocking. Rules are based on exact device and scan-code information, not just a normal virtual key name.

Saved key rule fields:

- Rule ID
- Device ID
- Device hardware ID
- Device display name
- Virtual key
- Scan code
- Extended-key flag
- Key name
- Enabled status
- Creation time

### 6.2 Keyboard Detection

The app lists Interception keyboard devices by scanning Interception keyboard slots 1 to 10. It reads hardware IDs and creates display names such as:

- Built-in / Laptop Keyboard
- USB/HID Keyboard
- Bluetooth Keyboard
- Raw hardware ID fallback

### 6.3 Hard Refresh

A hard refresh was added because the keyboard list sometimes appeared only after unplugging and replugging the keyboard. The hard refresh flow:

1. Stops the current worker.
2. Destroys the old Interception context.
3. Clears the cached device list.
4. Creates a fresh context.
5. Scans keyboard devices again.
6. Restarts the blocker.
7. Updates the UI.

### 6.4 Automatic Device Refresh

The app listens for Windows device-change events and schedules delayed refreshes. It also performs delayed startup scans to handle cases where the app starts before the driver/device list is fully ready.

### 6.5 Auto-Learn Key Catalog

The app preloads a standard keyboard catalog including:

- A-Z
- 0-9
- Escape
- Backspace
- Tab
- Enter
- Space
- Ctrl / Alt / Shift
- Caps Lock
- F1-F12
- Numpad keys
- Arrow keys
- Insert / Delete
- Home / End
- Page Up / Page Down
- Symbols like comma, period, slash, brackets, semicolon, quote, backslash, minus, equal

The app also auto-learns keys when it sees device-level events. This is important for special keys that cannot be known until they are pressed.

### 6.6 Startup with Windows

The app supports a Start with Windows checkbox. When enabled, it writes the application path into:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

This starts the app after user login.

### 6.7 Minimize to Tray

The app supports minimize-to-tray behavior. When enabled, closing the window hides it instead of fully exiting. A tray icon remains active with menu options.

### 6.8 Tray Icon

The tray icon provides:

- Open
- Hide
- Exit
- Double-click to show the app
- Balloon notifications for selected actions

### 6.9 Single Instance Protection

A mutex prevents multiple app instances. When the app is already running and a second instance starts, the second instance sends a broadcast message to the existing instance and exits. The existing instance then shows/activates the window.

### 6.10 Branding

Branding support includes:

- App icon path configured in the project file.
- Tray icon loaded through `BrandAssetService`.
- Window icon loaded through `BrandAssetService`.
- About/Dev Options logo loaded through `BrandAssetService`.

Important: the code is prepared to load exact branding files from `Assets/AppIcon.ico` and `Assets/AboutLogo.png`. The current repository still needs the actual binary assets committed in those paths to fully match the provided logo.

### 6.11 Dev Options Page

The Dev Options tab includes developer information:

- Developed by Samslab (Samiul Hasan)
- GitHub: `https://github.com/SamiulxHasanx07`
- Email: `samiulxhasan650@gmail.com`
- Phone: `+8801788058690`

The current logic attempts to show the full horizontal logo in the Dev Options page.

### 6.12 Settings Reset

The application supports a `--clear-settings` startup argument. When used, it resets the settings file to defaults:

- Start with Windows: false
- Minimize to tray: true
- Rules: empty
- Disabled keyboards: empty

This is the current emergency reset foundation.

## 7. Safety Work Already Done

Several safety changes were implemented after testing revealed lockout risks:

1. Removed unsafe global keyboard hook fallback.
2. Prevented Raw Input devices from becoming enforceable rule targets.
3. Added exact device + scan-code + extended-key matching.
4. Added forwarding behavior so unmatched keys are sent back to Windows.
5. Added worker-loop error handling.
6. Added duplicate-rule grouping inside the active rule table.
7. Added single-instance behavior to prevent duplicate tray apps.
8. Added hard refresh and device-change refresh.
9. Set Start with Windows default to false in the settings model.
10. Documented that full keyboard enforcement should stay paused until safe timed testing exists.

## 8. Known Limitations and Current Issues

### 8.1 Full Keyboard Disable Is Not Safe Yet

The UI and settings model still include disabled keyboard rules, but the current enforcement service ignores full-keyboard rules. This is intentional because full-keyboard disable previously caused serious device lockout risk.

Required future safe flow:

```text
Select keyboard -> temporary disable for a few seconds -> auto restore -> user confirms -> save permanent rule
```

### 8.2 Exact Branding Assets Are Not Fully Committed Yet

The code is prepared to use:

```text
Assets/AppIcon.ico
Assets/AboutLogo.png
```

But the exact binary files still need to exist in the repository output. There is currently an old Base64 icon decode flow in the project file, and the About logo file is not yet fully handled by the project build configuration.

### 8.3 Raw Input Is Detection Only

Raw Input can detect keys but cannot block them. The app correctly returns an empty enforceable device list from Raw Input, but some UI messaging still says detection only when the driver is missing.

### 8.4 Interception Driver Dependency

The app depends on the Interception driver and `interception.dll`. Without driver installation and a Windows restart, device-level blocking will not work.

### 8.5 No Background Windows Service Yet

The app is still a tray application, not a true background service. Rules apply only while the app process is running. A future service architecture would be stronger.

### 8.6 No Visual Log Page Yet

There is no full UI log table showing:

- Device ID
- Hardware ID
- Scan code
- Extended flag
- Blocked/allowed result
- Timestamp

This should be added for debugging.

### 8.7 Installer Startup Task Is Still Risky

The installer currently includes a startup task checked by default. This should be changed to unchecked by default because a keyboard-control app should not auto-start until rules are tested safely.

## 9. Recommended Final Architecture

For a professional production version, the project should eventually be split into:

```text
KeyDisabler.App       -> WPF / WinUI UI
KeyDisabler.Service   -> Windows Service for always-on rule enforcement
KeyDisabler.Driver    -> Proper keyboard filter driver layer
KeyDisabler.Shared    -> Shared models and settings contracts
```

The current app is a strong prototype/tray utility, but the safest production direction is a Windows Service + proper driver-based design.

## 10. Testing Checklist

### 10.1 Fresh Install Test

1. Uninstall old app.
2. Delete `%APPDATA%\KeyDisabler\settings.json`.
3. Install latest build.
4. Install driver.
5. Restart Windows.
6. Open app.
7. Press Refresh.
8. Verify keyboards appear.

### 10.2 Captured Key Rule Test

1. Open Keyboards tab.
2. Click Detect by key press.
3. Press a key on laptop keyboard.
4. Open Rules tab.
5. Click Capture Key.
6. Press the broken key on the selected keyboard.
7. Click Add Key Rule.
8. Test selected keyboard key: should block.
9. Test same key on external keyboard: should work.

### 10.3 Tray and Single Instance Test

1. Open app.
2. Close window while minimize-to-tray is enabled.
3. Confirm tray icon remains.
4. Launch app again from Start Menu.
5. Confirm no second tray icon appears.
6. Confirm original window opens.

### 10.4 Recovery Test

1. Exit the app from tray.
2. Run app with `--clear-settings`.
3. Confirm settings reset.
4. Confirm rules list is empty.

## 11. Improvement Roadmap

### Phase 1 - Current Build Stabilization

- Fix all compile errors.
- Ensure exact binary assets are committed correctly.
- Ensure installer copies branding assets.
- Make startup task unchecked by default.
- Add reset shortcut in Start Menu.
- Add tray Pause All Rules.
- Add tray Clear Rules.

### Phase 2 - Debugging and UX

- Add driver status page.
- Add event log page.
- Add keyboard profile labels.
- Add better empty-state messages.
- Add export/import settings.

### Phase 3 - Full Keyboard Safe Mode

- Add temporary full keyboard disable test.
- Auto-restore after timeout.
- Require user confirmation before saving full-keyboard rule.
- Never allow disabling the last usable keyboard.

### Phase 4 - Production Architecture

- Move rule enforcement into Windows Service.
- Keep UI as controller only.
- Add service health monitoring.
- Consider proper KMDF keyboard filter driver for long-term reliability.

## 12. Current Project Status

The project is currently best described as:

```text
Functional prototype with device-specific captured-key blocking, active safety improvements, tray UX, branding work in progress, and paused full-keyboard enforcement.
```

The safest feature to test right now is captured-key blocking. Full keyboard disabling should remain disabled or treated as not active until the timed safety workflow is implemented.
