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

    private AppSettings _settings = new();
    private TrayIconService? _trayIconService;
    private HwndSource? _hwndSource;
    private bool _isDetectingDevice;
    private bool _isLoading;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        _rawInputService.KeyPressed += RawInputService_KeyPressed;
        _deviceBlockerService.KeyReceived += DeviceBlockerService_KeyReceived;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;
        _settings = _settingsService.Load();

        foreach (var rule in _settings.Rules ?? new List<KeyboardRule>())
        {
            EnsureRuleCompatibility(rule);
            _rules.Add(rule);
        }

        foreach (var disabledKeyboard in _settings.DisabledKeyboards ?? new List<DisabledKeyboardRule>())
        {
            _disabledKeyboards.Add(disabledKeyboard);
        }

        KeyboardList.ItemsSource = _devices;
        RuleDeviceCombo.ItemsSource = _devices;
        RulesList.ItemsSource = _rules;
        DisabledKeyboardsList.ItemsSource = _disabledKeyboards;
        KeyCombo.ItemsSource = BuildKeyOptions();
        KeyCombo.SelectedIndex = 0;

        StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
        MinimizeToTrayCheck.IsChecked = _settings.MinimizeToTray;
        SettingsPathText.Text = _settingsService.SettingsPath;

        _deviceBlockerService.Start();
        RefreshDevices();
        UpdateRuleCount();
        UpdateDeviceBlocker();
        UpdateStatus(_deviceBlockerService.IsRunning ? "Device blocker active" : $"Driver not ready: {_deviceBlockerService.LastError}");

        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);
        _rawInputService.RegisterKeyboardInput(handle);

        _trayIconService = new TrayIconService(this);
        _isLoading = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!_deviceBlockerService.IsAvailable)
        {
            _rawInputService.ProcessMessage(new IntPtr(msg), lParam);
        }

        return IntPtr.Zero;
    }

    private void DeviceBlockerService_KeyReceived(object? sender, DeviceKeyEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            LastKeyText.Text = $"{e.KeyName} from {e.DeviceName}";
            FooterText.Text = e.WasBlocked
                ? $"Blocked: {e.KeyName} from {e.DeviceName}"
                : $"Allowed: {e.KeyName} from {e.DeviceName}";

            if (_isDetectingDevice)
            {
                var device = _devices.FirstOrDefault(item => string.Equals(item.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
                if (device is not null)
                {
                    KeyboardList.SelectedItem = device;
                    RuleDeviceCombo.SelectedItem = device;
                    _isDetectingDevice = false;
                    UpdateStatus("Keyboard detected");
                    _trayIconService?.ShowBalloon("Keyboard detected", device.DisplayName);
                }
            }
        });
    }

    private void RawInputService_KeyPressed(object? sender, RawKeyEventArgs e)
    {
        if (_deviceBlockerService.IsAvailable)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            var device = _devices.FirstOrDefault(item => string.Equals(item.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
            var deviceName = device?.DisplayName ?? "Unknown keyboard";

            LastKeyText.Text = $"{e.KeyName} from {deviceName}";
            FooterText.Text = $"Driver missing. Detection only: {e.KeyName} from {deviceName}";

            if (_isDetectingDevice && device is not null)
            {
                KeyboardList.SelectedItem = device;
                RuleDeviceCombo.SelectedItem = device;
                _isDetectingDevice = false;
                UpdateStatus("Keyboard detected, but driver is missing");
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
        UpdateStatus("Press a key on the target keyboard");
        FooterText.Text = "Detection mode is active. Press any key from the exact keyboard you want to use for this rule.";
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
        if (KeyboardList.SelectedItem is not KeyboardDevice device)
        {
            UpdateStatus("Select a keyboard first");
            return;
        }

        if (IsKeyboardDisabled(device.Id))
        {
            UpdateStatus("This keyboard is already disabled");
            return;
        }

        var otherEnabledKeyboardExists = _devices
            .Where(item => !string.Equals(item.Id, device.Id, StringComparison.OrdinalIgnoreCase))
            .Any(item => !IsKeyboardDisabled(item.Id));

        if (!otherEnabledKeyboardExists)
        {
            UpdateStatus("Safety block: cannot disable the last enabled keyboard");
            return;
        }

        _disabledKeyboards.Add(new DisabledKeyboardRule
        {
            DeviceId = device.Id,
            DeviceName = device.DisplayName,
            IsEnabled = true
        });

        SaveSettingsFromUi();
        UpdateDeviceBlocker();
        UpdateRuleCount();
        UpdateSelectedDeviceStatus();
        UpdateStatus("Selected keyboard disabled and saved");
    }

    private void EnableSelectedKeyboard_Click(object sender, RoutedEventArgs e)
    {
        DisabledKeyboardRule? disabledRule = null;

        if (KeyboardList.SelectedItem is KeyboardDevice device)
        {
            disabledRule = _disabledKeyboards.FirstOrDefault(rule => string.Equals(rule.DeviceId, device.Id, StringComparison.OrdinalIgnoreCase));
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
        UpdateStatus("Keyboard enabled again");
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        if (RuleDeviceCombo.SelectedItem is not KeyboardDevice device)
        {
            UpdateStatus("Select a keyboard first");
            return;
        }

        if (KeyCombo.SelectedItem is not KeyOption key)
        {
            UpdateStatus("Select a key first");
            return;
        }

        var exists = _rules.Any(rule =>
            string.Equals(rule.DeviceId, device.Id, StringComparison.OrdinalIgnoreCase) &&
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
            _trayIconService?.ShowBalloon("Key Disabler is still running", "Saved keyboard disables and key rules stay active while the tray app is running.");
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
        var selectedId = (KeyboardList.SelectedItem as KeyboardDevice)?.Id;
        _devices.Clear();

        var devices = _deviceBlockerService.GetKeyboardDevices();
        if (devices.Count == 0)
        {
            devices = _rawInputService.GetKeyboardDevices();
        }

        foreach (var device in devices)
        {
            _devices.Add(device);
        }

        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            KeyboardList.SelectedItem = _devices.FirstOrDefault(device => string.Equals(device.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        }

        if (KeyboardList.SelectedItem is null && _devices.Count > 0)
        {
            KeyboardList.SelectedIndex = 0;
        }

        FooterText.Text = $"Detected {_devices.Count} keyboard device(s).";
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
                    key.SetValue(StartupRegistryValueName, $"\"{executablePath}\"");
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
        var keyRuleText = _rules.Count == 1 ? "1 key rule" : $"{_rules.Count} key rules";
        var disabledKeyboardText = _disabledKeyboards.Count == 1 ? "1 disabled keyboard" : $"{_disabledKeyboards.Count} disabled keyboards";
        RuleCountText.Text = $"{keyRuleText}, {disabledKeyboardText}";
    }

    private void UpdateSelectedDeviceStatus()
    {
        if (KeyboardList.SelectedItem is not KeyboardDevice device)
        {
            SelectedDeviceStatusText.Text = "Status: not selected";
            return;
        }

        SelectedDeviceStatusText.Text = IsKeyboardDisabled(device.Id)
            ? "Status: disabled"
            : "Status: enabled";
    }

    private bool IsKeyboardDisabled(string deviceId)
    {
        return _disabledKeyboards.Any(rule =>
            rule.IsEnabled &&
            string.Equals(rule.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
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
}
