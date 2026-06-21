using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using KeyDisabler.App.Models;

namespace KeyDisabler.App;

internal static class MainWindowRemapBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            Window.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoaded));
    }

    private static void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            window.Dispatcher.BeginInvoke(
                new Action(window.InitializeRemapRules),
                DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    private readonly ObservableCollection<KeyRemapRule> _remapRules = new();
    private bool _isCapturingRemapFromKey;
    private bool _isCapturingRemapToKey;
    private bool _remapRulesInitialized;

    internal void InitializeRemapRules()
    {
        if (_remapRulesInitialized)
        {
            return;
        }

        _remapRulesInitialized = true;

        RemapDeviceCombo.ItemsSource = _devices;
        RemapFromKeyCombo.ItemsSource = _keyOptions;
        RemapToKeyCombo.ItemsSource = _keyOptions;
        RemapRulesList.ItemsSource = _remapRules;

        foreach (var remapRule in _settings.RemapRules ?? new List<KeyRemapRule>())
        {
            _remapRules.Add(remapRule);
            AddKeyOptionIfMissing(new KeyOption(remapRule.FromKeyName, 0, remapRule.FromScanCode, remapRule.FromIsExtendedKey));
            AddKeyOptionIfMissing(new KeyOption(remapRule.ToKeyName, 0, remapRule.ToScanCode, remapRule.ToIsExtendedKey));
        }

        if (KeyboardList.SelectedItem is KeyboardDevice selectedKeyboard)
        {
            RemapDeviceCombo.SelectedItem = selectedKeyboard;
        }
        else if (_devices.Count > 0)
        {
            RemapDeviceCombo.SelectedIndex = 0;
        }

        SelectDefaultRemapKeys();
        _deviceBlockerService.KeyReceived += RemapDeviceBlocker_KeyReceived;
        UpdateRemapDeviceBlocker();
        UpdateProtectionSummary();
    }

    private void CaptureRemapFromKey_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRemapKeyboard() is null)
        {
            UpdateStatus("Select or detect a keyboard first");
            return;
        }

        _isCapturingRemapFromKey = true;
        _isCapturingRemapToKey = false;
        _isCapturingRuleKey = false;
        _isDetectingDevice = false;
        RemapFromCapturedText.Text = "From capture active: press the healthy physical key.";
        UpdateStatus("Press the physical key to remap from");
    }

    private void CaptureRemapToKey_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRemapKeyboard() is null)
        {
            UpdateStatus("Select or detect a keyboard first");
            return;
        }

        _isCapturingRemapToKey = true;
        _isCapturingRemapFromKey = false;
        _isCapturingRuleKey = false;
        _isDetectingDevice = false;
        RemapToCapturedText.Text = "To capture active: press the key output you want.";
        UpdateStatus("Press the key to remap to");
    }

    private void AddRemapRule_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRemapKeyboard() is not KeyboardDevice device)
        {
            UpdateStatus("Select a keyboard first");
            return;
        }

        if (RemapFromKeyCombo.SelectedItem is not KeyOption fromKey)
        {
            UpdateStatus("Select or capture the From key first");
            return;
        }

        if (RemapToKeyCombo.SelectedItem is not KeyOption toKey)
        {
            UpdateStatus("Select or capture the To key first");
            return;
        }

        if (fromKey.ScanCode == toKey.ScanCode && fromKey.IsExtendedKey == toKey.IsExtendedKey)
        {
            UpdateStatus("From key and To key cannot be the same");
            return;
        }

        var exists = _remapRules.Any(rule =>
            rule.IsEnabled &&
            IsSameKeyboard(rule, device) &&
            rule.FromScanCode == fromKey.ScanCode &&
            rule.FromIsExtendedKey == fromKey.IsExtendedKey);

        if (exists)
        {
            UpdateStatus("A remap rule already exists for this keyboard and From key");
            return;
        }

        _remapRules.Add(BuildRemapRule(device, fromKey, toKey));
        SaveRemapSettings();
        UpdateRemapDeviceBlocker();
        UpdateProtectionSummary();
        UpdateStatus($"Remap saved: {fromKey.Name} → {toKey.Name}");
    }

    private void RemoveRemapRule_Click(object sender, RoutedEventArgs e)
    {
        if (RemapRulesList.SelectedItem is not KeyRemapRule rule)
        {
            UpdateStatus("Select a remap rule to remove");
            return;
        }

        _remapRules.Remove(rule);
        SaveRemapSettings();
        UpdateRemapDeviceBlocker();
        UpdateProtectionSummary();
        UpdateStatus("Remap rule removed");
    }

    private void FixBrokenSpacebar_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRemapKeyboard() is not KeyboardDevice device)
        {
            UpdateStatus("Select your laptop keyboard first");
            return;
        }

        var space = EnsureKeyOption("Space", 0x20, 0x39, false);
        var rightAlt = EnsureKeyOption("Right Alt", 0xA5, 0x38, true);
        var hardwareId = NormalizeHardwareId(device.DevicePath);

        var blockExists = _rules.Any(rule =>
            IsSameKeyboard(rule, device) &&
            rule.ScanCode == space.ScanCode &&
            rule.IsExtendedKey == space.IsExtendedKey);

        if (!blockExists)
        {
            _rules.Add(new KeyboardRule
            {
                DeviceId = device.Id,
                DeviceHardwareId = hardwareId,
                DeviceName = device.DisplayName,
                VirtualKey = space.VirtualKey,
                ScanCode = space.ScanCode,
                IsExtendedKey = space.IsExtendedKey,
                KeyName = space.Name,
                IsEnabled = true
            });
        }

        var remapExists = _remapRules.Any(rule =>
            rule.IsEnabled &&
            IsSameKeyboard(rule, device) &&
            rule.FromScanCode == rightAlt.ScanCode &&
            rule.FromIsExtendedKey == rightAlt.IsExtendedKey);

        if (!remapExists)
        {
            _remapRules.Add(BuildRemapRule(device, rightAlt, space));
        }

        RuleDeviceCombo.SelectedItem = device;
        RemapDeviceCombo.SelectedItem = device;
        KeyCombo.SelectedItem = space;
        RemapFromKeyCombo.SelectedItem = rightAlt;
        RemapToKeyCombo.SelectedItem = space;
        CapturedKeyText.Text = $"Block rule ready: {space.Name} from {device.DisplayName}";
        RemapFromCapturedText.Text = $"From: {rightAlt.Name}";
        RemapToCapturedText.Text = $"To: {space.Name}";

        SaveSettingsFromUi();
        SaveRemapSettings();
        UpdateDeviceBlocker();
        UpdateRemapDeviceBlocker();
        UpdateProtectionSummary();
        UpdateStatus("Broken Spacebar preset applied: Space blocked, Right Alt becomes Space");
    }

    private void RemapDeviceBlocker_KeyReceived(object? sender, DeviceKeyEventArgs e)
    {
        if (!_isCapturingRemapFromKey && !_isCapturingRemapToKey)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            var device = EnsureDeviceFromEvent(e);
            CaptureRemapKeyFromEvent(device, e, captureFromKey: _isCapturingRemapFromKey);
        });
    }

    private void CaptureRemapKeyFromEvent(KeyboardDevice eventDevice, DeviceKeyEventArgs e, bool captureFromKey)
    {
        var selectedDevice = SelectedRemapKeyboard();
        if (selectedDevice is not null && !IsSameKeyboard(selectedDevice, eventDevice))
        {
            var message = $"Ignored {e.KeyName}: it came from {eventDevice.DisplayName}, not the selected keyboard.";
            if (captureFromKey)
            {
                RemapFromCapturedText.Text = message;
            }
            else
            {
                RemapToCapturedText.Text = message;
            }

            UpdateStatus("Pressed key came from a different keyboard");
            return;
        }

        RemapDeviceCombo.SelectedItem = eventDevice;
        var keyName = string.IsNullOrWhiteSpace(e.KeyName) ? $"Scan {e.ScanCode}" : e.KeyName;
        var option = AddKeyOptionIfMissing(new KeyOption(keyName, 0, e.ScanCode, e.IsExtendedKey));

        if (captureFromKey)
        {
            RemapFromKeyCombo.SelectedItem = option;
            RemapFromCapturedText.Text = $"From: {option.Name} from {eventDevice.DisplayName}";
            _isCapturingRemapFromKey = false;
            UpdateStatus("From key captured. Now choose/capture To key.");
        }
        else
        {
            RemapToKeyCombo.SelectedItem = option;
            RemapToCapturedText.Text = $"To: {option.Name}";
            _isCapturingRemapToKey = false;
            UpdateStatus("To key captured. Now click Add Remap Rule.");
        }
    }

    private KeyRemapRule BuildRemapRule(KeyboardDevice device, KeyOption fromKey, KeyOption toKey)
    {
        return new KeyRemapRule
        {
            DeviceId = device.Id,
            DeviceHardwareId = NormalizeHardwareId(device.DevicePath),
            DeviceName = device.DisplayName,
            FromScanCode = fromKey.ScanCode,
            FromIsExtendedKey = fromKey.IsExtendedKey,
            FromKeyName = fromKey.Name,
            ToScanCode = toKey.ScanCode,
            ToIsExtendedKey = toKey.IsExtendedKey,
            ToKeyName = toKey.Name,
            IsEnabled = true
        };
    }

    private KeyboardDevice? SelectedRemapKeyboard()
    {
        return RemapDeviceCombo.SelectedItem as KeyboardDevice
            ?? RuleDeviceCombo.SelectedItem as KeyboardDevice
            ?? KeyboardList.SelectedItem as KeyboardDevice;
    }

    private KeyOption EnsureKeyOption(string name, ushort virtualKey, ushort scanCode, bool isExtendedKey)
    {
        var existing = _keyOptions.FirstOrDefault(item =>
            item.ScanCode == scanCode && item.IsExtendedKey == isExtendedKey);

        return existing ?? AddKeyOptionIfMissing(new KeyOption(name, virtualKey, scanCode, isExtendedKey));
    }

    private void SelectDefaultRemapKeys()
    {
        var rightAlt = EnsureKeyOption("Right Alt", 0xA5, 0x38, true);
        var space = EnsureKeyOption("Space", 0x20, 0x39, false);
        RemapFromKeyCombo.SelectedItem = rightAlt;
        RemapToKeyCombo.SelectedItem = space;
        RemapFromCapturedText.Text = $"From: {rightAlt.Name}";
        RemapToCapturedText.Text = $"To: {space.Name}";
    }

    private void SaveRemapSettings()
    {
        _settings.RemapRules = _remapRules.ToList();
        _settingsService.Save(_settings);
    }

    private void UpdateRemapDeviceBlocker()
    {
        _deviceBlockerService.UpdateRemapRules(_remapRules);
        _deviceBlockerService.Start();
    }

    private void UpdateProtectionSummary()
    {
        var blockText = _rules.Count == 1 ? "1 block rule" : $"{_rules.Count} block rules";
        var remapText = _remapRules.Count == 1 ? "1 remap rule" : $"{_remapRules.Count} remap rules";
        var disabledText = _disabledKeyboards.Count == 1 ? "1 disabled keyboard" : $"{_disabledKeyboards.Count} disabled keyboards";
        RuleCountText.Text = $"{blockText}, {remapText}, {disabledText}";
    }

    private static bool IsSameKeyboard(KeyRemapRule rule, KeyboardDevice device)
    {
        return SameHardwareId(rule.DeviceHardwareId, device.DevicePath) ||
               string.Equals(rule.DeviceId, device.Id, StringComparison.OrdinalIgnoreCase);
    }
}
