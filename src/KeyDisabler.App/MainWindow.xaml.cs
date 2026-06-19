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
    private readonly ObservableCollection<KeyboardDevice> _devices = new();
    private readonly ObservableCollection<KeyboardRule> _rules = new();

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
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;
        _settings = _settingsService.Load();

        foreach (var rule in _settings.Rules)
        {
            _rules.Add(rule);
        }

        KeyboardList.ItemsSource = _devices;
        RuleDeviceCombo.ItemsSource = _devices;
        RulesList.ItemsSource = _rules;
        KeyCombo.ItemsSource = BuildKeyOptions();
        KeyCombo.SelectedIndex = 0;

        StartWithWindowsCheck.IsChecked = _settings.StartWithWindows;
        MinimizeToTrayCheck.IsChecked = _settings.MinimizeToTray;
        SettingsPathText.Text = _settingsService.SettingsPath;

        RefreshDevices();
        UpdateRuleCount();
        UpdateStatus("Ready");

        var handle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);
        _rawInputService.RegisterKeyboardInput(handle);

        _trayIconService = new TrayIconService(this);
        _isLoading = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        _rawInputService.ProcessMessage(new IntPtr(msg), lParam);
        return IntPtr.Zero;
    }

    private void RawInputService_KeyPressed(object? sender, RawKeyEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var device = _devices.FirstOrDefault(item => string.Equals(item.Id, e.DeviceId, StringComparison.OrdinalIgnoreCase));
            var deviceName = device?.DisplayName ?? "Unknown keyboard";

            LastKeyText.Text = $"{e.KeyName} from {deviceName}";
            FooterText.Text = $"Last input: {e.KeyName} from {deviceName}";

            if (_isDetectingDevice && device is not null)
            {
                KeyboardList.SelectedItem = device;
                RuleDeviceCombo.SelectedItem = device;
                _isDetectingDevice = false;
                UpdateStatus("Keyboard detected");
                _trayIconService?.ShowBalloon("Keyboard detected", device.DisplayName);
            }
        });
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        RefreshDevices();
    }

    private void DetectDevice_Click(object sender, RoutedEventArgs e)
    {
        _isDetectingDevice = true;
        UpdateStatus("Press a key on the target keyboard");
        FooterText.Text = "Detection mode is active. Press any key from the laptop keyboard you want to identify.";
    }

    private void KeyboardList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (KeyboardList.SelectedItem is not KeyboardDevice device)
        {
            SelectedDeviceText.Text = "No keyboard selected";
            DevicePathText.Text = string.Empty;
            return;
        }

        SelectedDeviceText.Text = device.DisplayName;
        DevicePathText.Text = device.DevicePath;
        RuleDeviceCombo.SelectedItem = device;
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
            rule.VirtualKey == key.VirtualKey);

        if (exists)
        {
            UpdateStatus("Rule already exists");
            return;
        }

        var rule = new KeyboardRule
        {
            DeviceId = device.Id,
            DeviceName = device.DisplayName,
            VirtualKey = key.VirtualKey,
            KeyName = key.Name,
            IsEnabled = true
        };

        _rules.Add(rule);
        SaveSettingsFromUi();
        UpdateRuleCount();
        UpdateStatus("Rule saved");
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
        UpdateRuleCount();
        UpdateStatus("Rule removed");
    }

    private void OpenRulesTab_Click(object sender, RoutedEventArgs e)
    {
        RulesTab.IsSelected = true;
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
            _trayIconService?.Dispose();
            return;
        }

        if (_settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            _trayIconService?.ShowBalloon("Key Disabler is still running", "Double-click the tray icon to open it again.");
        }
        else
        {
            _allowClose = true;
            _hwndSource?.RemoveHook(WndProc);
            _trayIconService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void RefreshDevices()
    {
        var selectedId = (KeyboardList.SelectedItem as KeyboardDevice)?.Id;
        _devices.Clear();

        foreach (var device in _rawInputService.GetKeyboardDevices())
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
        _settingsService.Save(_settings);
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
        RuleCountText.Text = _rules.Count == 1 ? "1 rule saved" : $"{_rules.Count} rules saved";
    }

    private void UpdateStatus(string message)
    {
        StatusPill.Text = message;
        FooterText.Text = message;
    }

    private static IReadOnlyList<KeyOption> BuildKeyOptions()
    {
        return new List<KeyOption>
        {
            new("Space", 0x20),
            new("Enter", 0x0D),
            new("Backspace", 0x08),
            new("Tab", 0x09),
            new("Escape", 0x1B),
            new("Left Ctrl", 0xA2),
            new("Right Ctrl", 0xA3),
            new("Left Alt", 0xA4),
            new("Right Alt", 0xA5),
            new("Left Shift", 0xA0),
            new("Right Shift", 0xA1),
            new("Caps Lock", 0x14),
            new("Delete", 0x2E),
            new("Insert", 0x2D)
        };
    }

    private sealed record KeyOption(string Name, ushort VirtualKey);
}
