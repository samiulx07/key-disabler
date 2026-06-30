using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using KeyDisabler.App.Models;
using KeyDisabler.App.Services;
using Microsoft.Win32;

namespace KeyDisabler.App;

public partial class MainWindow : Window
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegistryValueName = "KeyDisabler";

    private readonly SettingsService _settingsService = new();
    private readonly RawInputService _rawInputService = new();
    private readonly DeviceKeyboardBlockerService _deviceBlockerService = new();
    private readonly ObservableCollection<KeyboardDevice> _devices = new();
    private readonly ObservableCollection<KeyboardRule> _rules = new();
    private readonly ObservableCollection<DisabledKeyboardRule> _disabledKeyboards = new();
    private readonly ObservableCollection<KeyOption> _keyOptions = new();

    private AppSettings _settings = new();
    private TrayIconService? _trayIconService;
    private HwndSource? _hwndSource;
    private bool _isDetectingDevice;
    private bool _isCapturingRuleKey;
    private bool _isDashboardDetecting;
    private bool _isLoading;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        _rawInputService.KeyPressed += RawInputService_KeyPressed;
        _deviceBlockerService.KeyReceived += DeviceBlockerService_KeyReceived;
    }

    private bool _isInitialized;

    public void EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _isLoading = true;

        // 1. Apply branding first
        ApplyBranding();

        // 2. Load settings and rules
        _settings = _settingsService.Load();

        foreach (var rule in _settings.Rules ?? new List<KeyboardRule>())
        {
            EnsureRuleCompatibility(rule);
            _rules.Add(rule);
            AddKeyOptionIfMissing(new KeyOption(rule.KeyName, rule.VirtualKey, rule.ScanCode, rule.IsExtendedKey));
        }

        foreach (var disabledKeyboard in _settings.DisabledKeyboards ?? new List<DisabledKeyboardRule>())
        {
            _disabledKeyboards.Add(disabledKeyboard);
        }

        foreach (var option in BuildKeyOptions())
        {
            AddKeyOptionIfMissing(option);
        }

        // 3. Initialize separate modules
        InitializeRemapRules();
        InitializeRuleListActions();
        InitializeKeyboardTester();
        ScheduleKeyboardTesterFullLayout();

        // 4. Bind ItemsSources
        KeyboardList.ItemsSource = _devices;
        RuleDeviceCombo.ItemsSource = _devices;
        RulesList.ItemsSource = _rules;
        DisabledKeyboardsList.ItemsSource = _disabledKeyboards;
        KeyCombo.ItemsSource = _keyOptions;
        KeyCombo.SelectedIndex = _keyOptions.Count > 0 ? 0 : -1;

        StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
        MinimizeToTrayCheck.IsChecked = _settings.MinimizeToTray;
        SettingsPathText.Text = _settingsService.SettingsPath;

        ThemeService.Initialize(_settings.Theme);
        ThemeComboBox.SelectedIndex = _settings.Theme switch
        {
            AppThemeMode.System => 0,
            AppThemeMode.Light => 1,
            AppThemeMode.Dark => 2,
            _ => 0
        };

        // 5. Force HWND handle creation for Win32 features
        var handle = new WindowInteropHelper(this).EnsureHandle();
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);
        _rawInputService.RegisterKeyboardInput(handle);

        // 6. Initialize device refresh and auto-learning
        AttachDeviceRefreshHooks();
        InstallKeyCatalogAndLearning();

        // 7. Initialize and start blockers
        RefreshDevices();
        UpdateRuleCount();
        UpdateDeviceBlocker();
        UpdateStatus(_deviceBlockerService.IsRunning ? "Device key rules active" : "Device blocker paused for safety");

        // 8. Load tray icon
        _trayIconService = new TrayIconService(this);

        _isLoading = false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureInitialized();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == SingleInstanceService.ShowMessageId)
        {
            Show();
            if (WindowState == System.Windows.WindowState.Minimized)
            {
                WindowState = System.Windows.WindowState.Normal;
            }
            Activate();
            handled = true;
            return IntPtr.Zero;
        }

        if (!_deviceBlockerService.IsRunning)
        {
            _rawInputService.ProcessMessage(new IntPtr(msg), lParam);
        }

        return IntPtr.Zero;
    }

    private void DeviceBlockerService_KeyReceived(object? sender, DeviceKeyEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var device = EnsureDeviceFromEvent(e);

            if (_isDashboardDetecting)
            {
                LastKeyText.Text = $"{e.KeyName} from {device.DisplayName}";
            }

            FooterText.Text = e.WasBlocked
                ? $"Blocked: {e.KeyName} from {device.DisplayName}"
                : $"Allowed: {e.KeyName} from {device.DisplayName}";

            if (_isCapturingRuleKey)
            {
                CaptureKeyFromEvent(device, e);
                return;
            }

            if (_isDetectingDevice)
            {
                KeyboardList.SelectedItem = device;
                RuleDeviceCombo.SelectedItem = device;
                _isDetectingDevice = false;
                RebindSavedRulesToCurrentDevices(saveAfterRebind: true);
                UpdateDeviceBlocker();
                UpdateStatus($"Keyboard detected and selected: {device.DisplayName}");
                _trayIconService?.ShowBalloon("Keyboard detected", device.DisplayName);
            }
        });
    }

    private void RawInputService_KeyPressed(object? sender, RawKeyEventArgs e)
    {
        if (_deviceBlockerService.IsRunning)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            var device = FindDeviceForSavedRule(e.DeviceId, e.DeviceId);
            
            if (device is null)
            {
                var displayName = $"Keyboard - Detection-only ({_devices.Count + 1})";
                device = new KeyboardDevice
                {
                    Id = $"rawinput:{e.DeviceId.ToUpperInvariant().Trim().GetHashCode():X8}",
                    DevicePath = e.DeviceId,
                    DisplayName = displayName,
                    DeviceType = "Raw Input Keyboard (detection only)"
                };
                _devices.Add(device);
            }

            var deviceName = device.DisplayName;

            if (_isDashboardDetecting)
            {
                LastKeyText.Text = $"{e.KeyName} from {deviceName}";
            }

            FooterText.Text = $"Detection only: {e.KeyName} from {deviceName}";

            if (_isDetectingDevice)
            {
                KeyboardList.SelectedItem = device;
                RuleDeviceCombo.SelectedItem = device;
                _isDetectingDevice = false;
                UpdateStatus(device.Id.StartsWith("interception:", StringComparison.OrdinalIgnoreCase)
                    ? $"Keyboard detected and selected: {device.DisplayName}"
                    : $"Keyboard detected: {device.DisplayName} (detection-only)");
                _trayIconService?.ShowBalloon("Keyboard detected", device.DisplayName);
            }
        });
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        RefreshDevices();
        UpdateSelectedDeviceStatus();
    }

    private void DetectDevice_Click(object sender, RoutedEventArgs e)
    {
        _isDetectingDevice = true;
        _isCapturingRuleKey = false;
        UpdateStatus("Press a key on the target keyboard");
        FooterText.Text = "Detection mode is active. Press any key from the exact keyboard you want to control.";
    }

    private void ToggleDashboardDetection_Click(object sender, RoutedEventArgs e)
    {
        _isDashboardDetecting = !_isDashboardDetecting;

        if (_isDashboardDetecting)
        {
            ToggleDetectKeysButton.Content = "Stop Detect Keys";
            LastKeyText.Text = "Listening for key presses...";
            UpdateStatus("Dashboard key detection active");
        }
        else
        {
            ToggleDetectKeysButton.Content = "Start Detect Keys";
            LastKeyText.Text = "Press 'Start Detect Keys' to begin";
            UpdateStatus("Dashboard key detection stopped");
        }
    }

    private void CaptureRuleKey_Click(object sender, RoutedEventArgs e)
    {
        if (RuleDeviceCombo.SelectedItem is not KeyboardDevice)
        {
            UpdateStatus("Select or detect a keyboard first");
            return;
        }

        _isCapturingRuleKey = true;
        _isDetectingDevice = false;
        CapturedKeyText.Text = "Capture mode active: press the exact key/button from the selected keyboard.";
        UpdateStatus("Press the exact key to capture");
    }

    private void KeyboardList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (KeyboardList.SelectedItem is not KeyboardDevice device)
        {
            SelectedDeviceText.Text = "No keyboard selected";
            SelectedDeviceStatusText.Text = "Status: not selected";
            DevicePathText.Text = string.Empty;
            return;
        }

        SelectedDeviceText.Text = device.DisplayName;
        DevicePathText.Text = device.DevicePath;
        RuleDeviceCombo.SelectedItem = device;
        UpdateSelectedDeviceStatus();
    }

    private void DisableSelectedKeyboard_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatus("Full keyboard disable is paused for safety. Use captured key rules instead.");
        System.Windows.MessageBox.Show(
            "Full keyboard disable is currently paused to prevent input lockout. Use Detect by key press, Capture Key, then Add Key Rule to block only the broken key on the selected keyboard.",
            "Safety protection",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void EnableSelectedKeyboard_Click(object sender, RoutedEventArgs e)
    {
        DisabledKeyboardRule? disabledRule = null;

        if (KeyboardList.SelectedItem is KeyboardDevice device)
        {
            disabledRule = _disabledKeyboards.FirstOrDefault(rule => IsSameKeyboard(rule, device));
        }

        disabledRule ??= DisabledKeyboardsList.SelectedItem as DisabledKeyboardRule;

        if (disabledRule is null)
        {
            UpdateStatus("Select a disabled keyboard to enable");
            return;
        }

        _disabledKeyboards.Remove(disabledRule);
        SaveSettingsFromUi();
        UpdateDeviceBlocker();
        UpdateRuleCount();
        UpdateSelectedDeviceStatus();
        UpdateStatus("Saved full-keyboard disable entry removed");
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        if (RuleDeviceCombo.SelectedItem is not KeyboardDevice device)
        {
            UpdateStatus("Select a keyboard first");
            return;
        }

        if (!device.Id.StartsWith("interception:", StringComparison.OrdinalIgnoreCase))
        {
            UpdateStatus("This device is detection-only. Install the driver, restart Windows, then detect the keyboard again.");
            return;
        }

        if (KeyCombo.SelectedItem is not KeyOption key)
        {
            UpdateStatus("Capture or select a key first");
            return;
        }

        var deviceHardwareId = NormalizeHardwareId(device.DevicePath);
        var exists = _rules.Any(rule =>
            IsSameKeyboard(rule, device) &&
            rule.ScanCode == key.ScanCode &&
            rule.IsExtendedKey == key.IsExtendedKey);

        if (exists)
        {
            UpdateStatus("Rule already exists for this keyboard and key");
            return;
        }

        var rule = new KeyboardRule
        {
            DeviceId = device.Id,
            DeviceHardwareId = deviceHardwareId,
            DeviceName = device.DisplayName,
            VirtualKey = key.VirtualKey,
            ScanCode = key.ScanCode,
            IsExtendedKey = key.IsExtendedKey,
            KeyName = key.Name,
            IsEnabled = true
        };

        _rules.Add(rule);
        SaveSettingsFromUi();
        UpdateDeviceBlocker();
        UpdateRuleCount();
        UpdateStatus("Device-specific key rule saved and active");
    }

    private void RemoveRule_Click(object sender, RoutedEventArgs e)
    {
        if (RulesList.SelectedItem is not KeyboardRule rule)
        {
            UpdateStatus("Select a rule to remove");
            return;
        }

        _rules.Remove(rule);
        SaveSettingsFromUi();
        UpdateDeviceBlocker();
        UpdateRuleCount();
        UpdateStatus("Key rule removed");
    }

    private void OpenRulesTab_Click(object sender, RoutedEventArgs e)
    {
        RulesTab.IsSelected = true;
    }

    private void OpenKeyboardsTab_Click(object sender, RoutedEventArgs e)
    {
        KeyboardsTab.IsSelected = true;
    }

    private void SettingsCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        SaveSettingsFromUi();
        ApplyStartupSetting(_settings.StartWithWindows);
        UpdateStatus("Settings saved");
    }

    private void ThemeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isLoading || ThemeComboBox.SelectedItem is not System.Windows.Controls.ComboBoxItem item)
        {
            return;
        }

        var tag = item.Tag?.ToString();
        var mode = tag switch
        {
            "Light" => AppThemeMode.Light,
            "Dark" => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };

        if (_settings.Theme != mode)
        {
            _settings.Theme = mode;
            ThemeService.ApplyTheme(mode);
            _settingsService.Save(_settings);
            UpdateStatus($"Theme set to {tag}");
        }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
        {
            _hwndSource?.RemoveHook(WndProc);
            _deviceBlockerService.Dispose();
            _trayIconService?.Dispose();
            return;
        }

        if (_settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            _trayIconService?.ShowBalloon("Key Disabler is still running", "Saved rules stay active while the tray app is running.");
        }
        else
        {
            _allowClose = true;
            _hwndSource?.RemoveHook(WndProc);
            _deviceBlockerService.Dispose();
            _trayIconService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void RefreshDevices()
    {
        var selectedHardwareId = (KeyboardList.SelectedItem as KeyboardDevice)?.DevicePath;
        var selectedId = (KeyboardList.SelectedItem as KeyboardDevice)?.Id;
        _devices.Clear();

        var usedFallback = false;
        var devices = _deviceBlockerService.GetKeyboardDevices();
        if (devices.Count == 0)
        {
            devices = _rawInputService.GetKeyboardDevices();
            usedFallback = devices.Count > 0;
        }

        foreach (var device in devices)
        {
            _devices.Add(device);
        }

        RebindSavedRulesToCurrentDevices(saveAfterRebind: true);

        if (!string.IsNullOrWhiteSpace(selectedHardwareId))
        {
            KeyboardList.SelectedItem = _devices.FirstOrDefault(device => SameHardwareId(device.DevicePath, selectedHardwareId));
        }

        if (KeyboardList.SelectedItem is null && !string.IsNullOrWhiteSpace(selectedId))
        {
            KeyboardList.SelectedItem = _devices.FirstOrDefault(device => string.Equals(device.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        }

        if (KeyboardList.SelectedItem is null && _devices.Count > 0)
        {
            KeyboardList.SelectedIndex = 0;
        }

        UpdateSelectedDeviceStatus();

        if (usedFallback)
        {
            FooterText.Text = $"Detected {_devices.Count} keyboard(s) via Windows. Restart PC after driver install to enable blocking.";
        }
        else
        {
            FooterText.Text = $"Detected {_devices.Count} keyboard device(s).";
        }
    }

    private KeyboardDevice EnsureDeviceFromEvent(DeviceKeyEventArgs e)
    {
        var device = _devices.FirstOrDefault(item => string.Equals(item.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
        if (device is not null)
        {
            return device;
        }

        device = new KeyboardDevice
        {
            Id = e.DeviceId,
            DevicePath = e.DeviceHardwareId,
            DisplayName = string.IsNullOrWhiteSpace(e.DeviceName) ? e.DeviceId : e.DeviceName,
            DeviceType = "Interception Keyboard"
        };

        _devices.Add(device);
        return device;
    }

    private void CaptureKeyFromEvent(KeyboardDevice eventDevice, DeviceKeyEventArgs e)
    {
        if (RuleDeviceCombo.SelectedItem is KeyboardDevice selectedDevice && !IsSameKeyboard(selectedDevice, eventDevice))
        {
            CapturedKeyText.Text = $"Ignored {e.KeyName}: it came from {eventDevice.DisplayName}, not the selected keyboard.";
            UpdateStatus("Pressed key came from a different keyboard");
            return;
        }

        var keyName = string.IsNullOrWhiteSpace(e.KeyName)
            ? $"Scan {e.ScanCode}"
            : e.KeyName;

        var option = AddKeyOptionIfMissing(new KeyOption(keyName, 0, e.ScanCode, e.IsExtendedKey));
        KeyCombo.SelectedItem = option;
        RuleDeviceCombo.SelectedItem = eventDevice;
        _isCapturingRuleKey = false;

        CapturedKeyText.Text = $"Captured: {option.Name} from {eventDevice.DisplayName}";
        UpdateStatus("Key captured. Now click Add Key Rule.");
    }

    private void RebindSavedRulesToCurrentDevices(bool saveAfterRebind)
    {
        var changed = false;

        foreach (var rule in _rules)
        {
            var matchedDevice = FindDeviceForSavedRule(rule.DeviceHardwareId, rule.DeviceId);
            if (matchedDevice is null || !matchedDevice.Id.StartsWith("interception:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var hardwareId = NormalizeHardwareId(matchedDevice.DevicePath);
            if (!string.Equals(rule.DeviceId, matchedDevice.Id, StringComparison.OrdinalIgnoreCase))
            {
                rule.DeviceId = matchedDevice.Id;
                changed = true;
            }

            if (!string.Equals(rule.DeviceHardwareId, hardwareId, StringComparison.OrdinalIgnoreCase))
            {
                rule.DeviceHardwareId = hardwareId;
                changed = true;
            }

            if (!string.Equals(rule.DeviceName, matchedDevice.DisplayName, StringComparison.Ordinal))
            {
                rule.DeviceName = matchedDevice.DisplayName;
                changed = true;
            }
        }

        foreach (var rule in _disabledKeyboards)
        {
            var matchedDevice = FindDeviceForSavedRule(rule.HardwareId, rule.DeviceId);
            if (matchedDevice is null)
            {
                continue;
            }

            var hardwareId = NormalizeHardwareId(matchedDevice.DevicePath);
            if (!string.Equals(rule.DeviceId, matchedDevice.Id, StringComparison.OrdinalIgnoreCase))
            {
                rule.DeviceId = matchedDevice.Id;
                changed = true;
            }

            if (!string.Equals(rule.HardwareId, hardwareId, StringComparison.OrdinalIgnoreCase))
            {
                rule.HardwareId = hardwareId;
                changed = true;
            }

            if (!string.Equals(rule.DeviceName, matchedDevice.DisplayName, StringComparison.Ordinal))
            {
                rule.DeviceName = matchedDevice.DisplayName;
                changed = true;
            }
        }

        if (changed && saveAfterRebind)
        {
            SaveSettingsFromUi();
        }
    }

    private KeyboardDevice? FindDeviceForSavedRule(string hardwareId, string fallbackDeviceId)
    {
        if (!string.IsNullOrWhiteSpace(hardwareId))
        {
            var byHardware = _devices.FirstOrDefault(device => SameHardwareId(device.DevicePath, hardwareId));
            if (byHardware is not null)
            {
                return byHardware;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackDeviceId))
        {
            return _devices.FirstOrDefault(device => string.Equals(device.Id, fallbackDeviceId, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    private void SaveSettingsFromUi()
    {
        _settings.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _settings.MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
        _settings.Rules = _rules.ToList();
        _settings.DisabledKeyboards = _disabledKeyboards.ToList();
        _settingsService.Save(_settings);
    }

    private void UpdateDeviceBlocker()
    {
        RebindSavedRulesToCurrentDevices(saveAfterRebind: false);
        _deviceBlockerService.UpdateRules(_rules, _disabledKeyboards);
        _deviceBlockerService.Start();
    }

    private void ApplyStartupSetting(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true);
            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                var executablePath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(executablePath))
                {
                    key.SetValue(StartupRegistryValueName, $"\"{executablePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(StartupRegistryValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            UpdateStatus("Could not update startup setting");
        }
    }

    private void UpdateRuleCount()
    {
        UpdateProtectionSummary();
    }

    private void UpdateSelectedDeviceStatus()
    {
        if (KeyboardList.SelectedItem is not KeyboardDevice device)
        {
            SelectedDeviceStatusText.Text = "Status: not selected";
            return;
        }

        SelectedDeviceStatusText.Text = IsKeyboardDisabled(device)
            ? "Status: saved full-keyboard entry paused"
            : "Status: enabled";
    }

    private bool IsKeyboardDisabled(KeyboardDevice device)
    {
        return _disabledKeyboards.Any(rule => rule.IsEnabled && IsSameKeyboard(rule, device));
    }

    private static bool IsSameKeyboard(KeyboardRule rule, KeyboardDevice device)
    {
        return SameHardwareId(rule.DeviceHardwareId, device.DevicePath) ||
               string.Equals(rule.DeviceId, device.Id, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameKeyboard(DisabledKeyboardRule rule, KeyboardDevice device)
    {
        return SameHardwareId(rule.HardwareId, device.DevicePath) ||
               string.Equals(rule.DeviceId, device.Id, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameKeyboard(KeyboardDevice left, KeyboardDevice right)
    {
        return SameHardwareId(left.DevicePath, right.DevicePath) ||
               string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameHardwareId(string left, string right)
    {
        var normalizedLeft = NormalizeHardwareId(left);
        var normalizedRight = NormalizeHardwareId(right);
        if (string.IsNullOrWhiteSpace(normalizedLeft) || string.IsNullOrWhiteSpace(normalizedRight))
        {
            return false;
        }

        if (string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var coreLeft = ExtractCoreHardwareId(normalizedLeft);
        var coreRight = ExtractCoreHardwareId(normalizedRight);

        if (string.IsNullOrWhiteSpace(coreLeft) || string.IsNullOrWhiteSpace(coreRight))
        {
            return false;
        }

        if (string.Equals(coreLeft, coreRight, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (coreLeft.Contains("VID_") && coreLeft.Contains("PID_") &&
            coreRight.Contains("VID_") && coreRight.Contains("PID_"))
        {
            var vidLeft = GetSubpart(coreLeft, "VID_");
            var pidLeft = GetSubpart(coreLeft, "PID_");
            var vidRight = GetSubpart(coreRight, "VID_");
            var pidRight = GetSubpart(coreRight, "PID_");

            if (!string.IsNullOrEmpty(vidLeft) && !string.IsNullOrEmpty(pidLeft) &&
                string.Equals(vidLeft, vidRight, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(pidLeft, pidRight, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ExtractCoreHardwareId(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var parts = path.Split(new[] { '\\', '#', '/' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var upperPart = part.Trim().ToUpperInvariant();
            if (upperPart.Contains("VID_") && upperPart.Contains("PID_"))
            {
                return upperPart;
            }
            if (upperPart.Contains("PNP03") || upperPart.Contains("PNP0A"))
            {
                return upperPart;
            }
        }
        
        foreach (var part in parts)
        {
            var upperPart = part.Trim().ToUpperInvariant();
            if (upperPart.Equals("HID", StringComparison.Ordinal) ||
                upperPart.Equals("KEYBOARD", StringComparison.Ordinal) ||
                upperPart.Equals("ACPI", StringComparison.Ordinal) ||
                upperPart.StartsWith("??", StringComparison.Ordinal) ||
                upperPart.Contains("{") ||
                upperPart.Length < 3)
            {
                continue;
            }
            
            return upperPart;
        }

        return path.Trim().ToUpperInvariant();
    }

    private static string GetSubpart(string source, string prefix)
    {
        var idx = source.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return string.Empty;
        var sub = source.Substring(idx);
        var endIdx = sub.IndexOf('&');
        return endIdx < 0 ? sub : sub.Substring(0, endIdx);
    }

    private static string NormalizeHardwareId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private KeyOption AddKeyOptionIfMissing(KeyOption option)
    {
        var existing = _keyOptions.FirstOrDefault(item =>
            item.ScanCode == option.ScanCode &&
            item.IsExtendedKey == option.IsExtendedKey);

        if (existing is not null)
        {
            return existing;
        }

        _keyOptions.Add(option);
        return option;
    }

    private void UpdateStatus(string message)
    {
        StatusPill.Text = message;
        FooterText.Text = message;
    }

    private static void EnsureRuleCompatibility(KeyboardRule rule)
    {
        if (rule.ScanCode > 0)
        {
            return;
        }

        var option = BuildKeyOptions().FirstOrDefault(item => item.VirtualKey == rule.VirtualKey);
        if (option is not null)
        {
            rule.ScanCode = option.ScanCode;
            rule.IsExtendedKey = option.IsExtendedKey;
        }
    }

    private static IReadOnlyList<KeyOption> BuildKeyOptions()
    {
        return new List<KeyOption>
        {
            new("Space", 0x20, 0x39, false),
            new("Enter", 0x0D, 0x1C, false),
            new("Numpad Enter", 0x0D, 0x1C, true),
            new("Backspace", 0x08, 0x0E, false),
            new("Tab", 0x09, 0x0F, false),
            new("Escape", 0x1B, 0x01, false),
            new("Left Ctrl", 0xA2, 0x1D, false),
            new("Right Ctrl", 0xA3, 0x1D, true),
            new("Left Alt", 0xA4, 0x38, false),
            new("Right Alt", 0xA5, 0x38, true),
            new("Left Shift", 0xA0, 0x2A, false),
            new("Right Shift", 0xA1, 0x36, false),
            new("Caps Lock", 0x14, 0x3A, false),
            new("Delete", 0x2E, 0x53, true),
            new("Insert", 0x2D, 0x52, true)
        };
    }

    private sealed record KeyOption(string Name, ushort VirtualKey, ushort ScanCode, bool IsExtendedKey);
    private void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = "KeyDisablerSettings.json",
            Title = "Export Settings"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                System.IO.File.Copy(_settingsService.SettingsPath, dialog.FileName, true);
                UpdateStatus("Settings exported successfully");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export settings: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            Title = "Import Settings"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                System.IO.File.Copy(dialog.FileName, _settingsService.SettingsPath, true);
                
                // Reload settings
                _settings = _settingsService.Load();
                
                // Refresh UI checkboxes
                StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
                MinimizeToTrayCheck.IsChecked = _settings.MinimizeToTray;
                ThemeComboBox.SelectedIndex = _settings.Theme switch
                {
                    AppThemeMode.System => 0,
                    AppThemeMode.Light => 1,
                    AppThemeMode.Dark => 2,
                    _ => 0
                };
                ThemeService.ApplyTheme(_settings.Theme);

                // Reload lists
                _rules.Clear();
                _disabledKeyboards.Clear();
                _keyOptions.Clear();

                foreach (var rule in _settings.Rules)
                {
                    EnsureRuleCompatibility(rule);
                    _rules.Add(rule);
                    AddKeyOptionIfMissing(new KeyOption(rule.KeyName, rule.VirtualKey, rule.ScanCode, rule.IsExtendedKey));
                }

                foreach (var kb in _settings.DisabledKeyboards)
                {
                    _disabledKeyboards.Add(kb);
                }

                // Restart blocker
                UpdateDeviceBlocker();

                UpdateStatus("Settings imported successfully");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to import settings: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
