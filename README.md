# ⌨️ Key Disabler

**Block broken keys, remap healthy keys, and test each physical keyboard safely.**

Key Disabler is a Windows desktop utility for per-device keyboard management. It lets you disable specific keys on one physical keyboard while the same key works perfectly on another — ideal for laptops with stuck keys, split keyboards, or custom layouts.

![GitHub Actions](https://github.com/samiulx07/key-disabler/actions/workflows/build.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Size](https://img.shields.io/github/repo-size/samiulx07/key-disabler)
![License](https://img.shields.io/badge/license-MIT-green)

---

## ✨ Features

### 🚫 Device-Specific Key Blocking
Block a key on one keyboard without affecting others:

```text
Keyboard 1 + Space → ❌ Blocked
Keyboard 2 + Space → ✅ Allowed
```

Rules are matched using device identity + scan code + extended-key state, enforced at the driver level via the [Interception](https://github.com/oblitum/Interception) filter driver.

### 🔄 Key Remapping
Remap any key to any other key on a per-device basis:
- Capture the source key by pressing it physically
- Choose or capture the target key from a drop-down
- Rules persist across restarts

### ⌨️ Built-in Keyboard Tester
Test every key on any connected keyboard with a full visual layout. See real-time key press feedback including scan codes, extended-key state, and which keyboard the event came from.

### 🎨 Themes
Choose between **Light**, **Dark**, or **System** theme — follows Windows system setting automatically.

### ⚡ Auto-Updates
The app automatically checks for updates on startup via Velopack. When a new version is available, you'll be prompted to download and install with one click — no manual downloads needed. You can also check manually via the **"✓ Check for Updates"** button in the footer.

### 🖥️ System Tray
Minimizes to the system tray. Right-click for quick access: Open, Hide, or Exit.

### 🛠️ Driver Management
Install or uninstall the Interception driver directly from the app UI with admin elevation (UAC prompt).

---

## 🛡️ Safety Design

To prevent accidental input lockout, the app includes multiple safety layers:

| Safety Measure | Status |
|---------------|--------|
| Full-keyboard disable | ⏸️ Paused — requires timed-test safety flow |
| Blocker auto-start | ❌ Only runs when active rules exist |
| Raw Input devices | 🔍 Detection only — no enforceable rules |
| Driver auto-install | ❌ Unchecked by default in installer |
| Windows startup | ❌ Unchecked by default |
| Last keyboard disable | ❌ Blocked by safety check |
| Recovery shortcuts | ✅ Added to Start Menu |

---

## 🔄 Auto-Update System

Key Disabler uses **Velopack** for automatic updates:

1. **Launch the app** → Background check runs
2. **Update available?** → Popup asks to download & install
3. **Click Yes** → Downloads in background, applies, restarts

Updates are delivered via **GitHub Releases**. The app checks for prerelease versions by default.

---

## 📊 Code Audit Summary

**Total:** 28 C# files | ~4,100 LOC | .NET 10 WPF

### 🟢 Strengths

| Area | Finding |
|------|---------|
| **Architecture** | Clean separation — 4 models, 11 services, partial class MainWindow |
| **Memory safety** | `Marshal.FreeHGlobal` properly called in `finally` blocks |
| **Threading** | All UI updates dispatched via `Dispatcher`; debounced device refresh |
| **Single instance** | Global `Mutex` + `RegisterWindowMessage` for inter-process signaling |
| **Theme system** | Dynamic resource dictionary swap; follows Windows system theme |
| **Logging** | Failsafe startup logging to `%APPDATA%\KeyDisabler\logs\` |
| **Error resilience** | All external calls (file I/O, driver, registry) wrapped in try/catch |
| **Asset fallback** | Tray/Win icon degrades gracefully from .ico → .exe → system default |
| **CI/CD** | Full pipeline: build → Velopack pack → release to GitHub |

### 🟡 Minor Observations

| Issue | Severity | Suggestion |
|-------|----------|------------|
| `RawInputService` uses `GetHashCode()` for device ID (non-stable) | Low | Use a hash of `devicePath` with a stable algorithm (e.g. SHA256 truncated) |
| `KeyNameResolver` has limited key coverage | Low | Merge with the full catalog from `BuildStandardKeyboardCatalog()` |
| Phone number visible in Dev Options tab | Low | Consider making this configurable or conditional |
| `SettingsService.Reset()` doesn't clear `RemapRules` | Low | Add `RemapRules = new()` to the reset |
| `DeviceKeyboardBlockerService` worker has no graceful shutdown timeout | Low | Add `_workerTask.Wait(TimeSpan.FromSeconds(5))` in dispose |
| Version number in `.csproj` is `0.2.2` — consider semver alignment with CI | Low | Sync `Version` with CI release tags `v{run}.0.0` |

---

## 🚀 Local Build

```bash
dotnet publish src/KeyDisabler.App/KeyDisabler.App.csproj \
  -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:EnableCompressionInSingleFile=true \
  -o publish
```

Then copy the `driver/` folder (from the repo root) into `publish/` for in-app driver management.

---

## 🧪 How to Use

1. **Launch Key Disabler**
2. **Select a keyboard** from the Keyboards tab
3. **Capture a key** by pressing it physically (or pick from the dropdown)
4. **Save the rule** — that key is now blocked on that specific keyboard
5. **Optionally remap keys** in the Remap tab
6. **Test keys** in the Keyboard Tester tab with the full visual layout

---

## ⚠️ Emergency Recovery

If something goes wrong, run this from an **Administrator Command Prompt**:

```bat
taskkill /IM KeyDisabler.exe /F
del "%APPDATA%\KeyDisabler\settings.json"
"%ProgramFiles%\Key Disabler\driver\install-interception.exe" /uninstall
shutdown /r /t 0
```

See [docs/emergency-recovery.md](docs/emergency-recovery.md) for detailed instructions, including what to do if the driver path is missing.

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| **Language** | C# (.NET 10) |
| **UI Framework** | WPF (Windows Presentation Foundation) |
| **Keyboard driver** | [Interception](https://github.com/oblitum/Interception) (low-level keyboard filter) |
| **Auto-update** | [Velopack](https://velopack.io/) |
| **CI/CD** | GitHub Actions |
| **Packaging** | Velopack + standalone portable zip |

---

## 📦 Project Structure

```
src/KeyDisabler.App/
├── App.xaml / App.xaml.cs         # Application entry, Velopack bootstrap
├── MainWindow.xaml                # Full WPF UI (Dashboard, Keyboards, Rules, Remap, Tester, Settings, Dev Options)
├── MainWindow.xaml.cs             # Window logic: init, WndProc, device/rule management, exports
├── MainWindow.*.cs                # Partial classes: Branding, DeviceRefresh, HardRefresh, FooterDetection,
│                                  #   KeyboardTester, KeyboardTesterFullLayout, Remap, RuleListActions, UiFixes
├── Models/                        # AppSettings, KeyboardRule, KeyRemapRule, DisabledKeyboardRule, KeyboardDevice
├── Services/
│   ├── BrandAssetService.cs       # Icon and logo loading with fallbacks
│   ├── DeviceKeyboardBlockerService.cs  # Core: Interception driver blocking + remapping
│   ├── DeviceKeyEventArgs.cs      # Event args for Interception key events
│   ├── InterceptionNative.cs      # P/Invoke bindings for interception.dll
│   ├── KeyNameResolver.cs         # Scan code → human name resolver
│   ├── RawInputService.cs         # Win32 Raw Input API for keyboard detection
│   ├── RawKeyEventArgs.cs         # Event args for Raw Input key events
│   ├── SettingsService.cs         # JSON settings persistence in %APPDATA%
│   ├── SingleInstanceService.cs   # Global mutex + window message for single instance
│   ├── StartupLogService.cs       # File-based startup error logging
│   ├── ThemeService.cs            # WPF theme switching with Windows system detection
│   ├── TrayIconService.cs         # System tray NotifyIcon with context menu
│   └── UpdateService.cs           # Velopack update check, download, and apply
└── Themes/                        # DarkTheme.xaml, LightTheme.xaml
```

---

## 👨‍💻 Author

**Samslab (Samiul Hasan)**

[GitHub](https://github.com/SamiulxHasanx07) · [Email](mailto:samiulxhasan650@gmail.com)

---

## 📄 License

This project is open source. See the LICENSE file for details.
