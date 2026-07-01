# ⌨️ Key Disabler

**Block broken keys, remap healthy keys, and test each physical keyboard safely.**

Key Disabler is a Windows desktop utility for per-device keyboard management. It lets you disable specific keys on one physical keyboard while the same key works perfectly on another — ideal for laptops with stuck keys, split keyboards, or custom layouts.

![GitHub Actions](https://github.com/samiulx07/key-disabler/actions/workflows/build.yml/badge.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows-blue)

---

## ✨ Features

### 🚫 Device-Specific Key Blocking
Block a key on one keyboard without affecting others:

```text
Keyboard 1 + Space → ❌ Blocked
Keyboard 2 + Space → ✅ Allowed
Keyboard 3 + Space → ✅ Allowed
```

Rules are matched using device identity + scan code + extended-key state.

### 🔄 Key Remapping
Remap any key to any other key on a per-device basis:
- Capture the source key by pressing it physically
- Choose or capture the target key
- Rules persist across restarts

### ⌨️ Built-in Keyboard Tester
Test every key on any connected keyboard with a full visual layout. See real-time key press feedback including scan codes and extended-key state.

### 🎨 Themes
Choose between **Light**, **Dark**, or **System** theme.

### ⚡ Auto-Updates (Velopack)
The app automatically checks for updates on startup. When a new version is available, you'll be prompted to download and install with one click — no manual downloads needed.

### 🖥️ System Tray
Minimizes to the system tray. Quick access to toggle blocking, check status, or exit.

### 🛠️ Driver Management
Install or uninstall the Interception driver directly from the app UI with admin elevation.

---

## 🛡️ Safety Design

To prevent accidental input lockout:

| Safety Measure | Status |
|---------------|--------|
| Full-keyboard disable | ⏸️ Paused — requires timed-test safety flow |
| Blocker auto-start | ❌ Only when active rules exist |
| Raw Input devices | 🔍 Detection only — no enforceable rules |
| Driver auto-install | ❌ Unchecked by default |
| Windows startup | ❌ Unchecked by default |
| Recovery shortcuts | ✅ Added to Start Menu |

---

## 🔄 Auto-Update System

Key Disabler uses **Velopack** for automatic updates:

1. **You launch the app** → Background check runs
2. **Update available?** → Popup asks to download & install
3. **Click Yes** → Downloads in background, applies, restarts

You can also manually check at any time via the **"✓ Check for Updates"** button in the app footer.

---

## 📦 Build Artifacts

GitHub Actions produces these on every push:

| Artifact | Description |
|----------|-------------|
| `KeyDisabler-portable-win-x64.zip` | Standalone portable app (no install needed) |
| `RELEASES/*` | Velopack auto-update packages |

Manual workflow runs also publish to **GitHub Releases** with prerelease tags.

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

Portable output: `publish/KeyDisabler.exe`

Copy the `driver/` folder from the repo into `publish/` for in-app driver management.

---

## 🧪 How to Use

1. **Launch Key Disabler**
2. **Select a keyboard** from the dashboard
3. **Capture a key** by pressing it physically
4. **Save the rule** — that key is now blocked on that specific keyboard
5. **Optionally remap keys** in the Remap tab
6. **Test keys** in the Keyboard Tester tab

---

## ⚠️ Emergency Recovery

If something goes wrong, run this from an **Administrator Command Prompt**:

```bat
taskkill /IM KeyDisabler.exe /F
del "%APPDATA%\KeyDisabler\settings.json"
"%ProgramFiles%\Key Disabler\driver\install-interception.exe" /uninstall
shutdown /r /t 0
```

See [docs/emergency-recovery.md](docs/emergency-recovery.md) for more details.

---

## 🛠️ Tech Stack

- **Language:** C# (.NET 10)
- **Framework:** WPF (Windows Presentation Foundation)
- **Driver:** [Interception](https://github.com/oblitum/Interception) (low-level keyboard filter)
- **Auto-Update:** [Velopack](https://velopack.io/)
- **Packaging:** Velopack + GitHub Actions

---

## 👨‍💻 Author

**Samslab (Samiul Hasan)**

---

## 📄 License

This project is open source. See the LICENSE file for details.
