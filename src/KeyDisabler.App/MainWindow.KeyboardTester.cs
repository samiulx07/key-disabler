using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using KeyDisabler.App.Models;
using KeyDisabler.App.Services;

namespace KeyDisabler.App;

internal static class MainWindowKeyboardTesterBootstrap
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
                new Action(window.InitializeKeyboardTester),
                DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    private readonly ObservableCollection<KeyboardTestHistoryItem> _keyboardTestHistory = new();
    private readonly Dictionary<string, Button> _keyboardTesterButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _keyboardTesterStates = new(StringComparer.OrdinalIgnoreCase);

    private ComboBox? _testerKeyboardCombo;
    private TextBlock? _testerSelectedKeyboardText;
    private TextBlock? _testerDropdownHintText;
    private Button? _toggleKeyboardTesterButton;
    private Button? _resetKeyboardTesterButton;
    private TextBlock? _testerLastKeyText;
    private TextBlock? _testerCountText;
    private StackPanel? _keyboardTesterLayoutPanel;
    private ListView? _testerHistoryList;

    private bool _keyboardTesterInitialized;
    private bool _isKeyboardTesting;

    internal void InitializeKeyboardTester()
    {
        if (_keyboardTesterInitialized)
        {
            return;
        }

        _testerKeyboardCombo = FindName("TesterKeyboardCombo") as ComboBox;
        _testerSelectedKeyboardText = FindName("TesterSelectedKeyboardText") as TextBlock;
        _testerDropdownHintText = FindName("TesterDropdownHintText") as TextBlock;
        _toggleKeyboardTesterButton = FindName("ToggleKeyboardTesterButton") as Button;
        _resetKeyboardTesterButton = FindName("ResetKeyboardTesterButton") as Button;
        _testerLastKeyText = FindName("TesterLastKeyText") as TextBlock;
        _testerCountText = FindName("TesterCountText") as TextBlock;
        _keyboardTesterLayoutPanel = FindName("KeyboardTesterLayoutPanel") as StackPanel;
        _testerHistoryList = FindName("TesterHistoryList") as ListView;

        if (_keyboardTesterLayoutPanel is null)
        {
            return;
        }

        _keyboardTesterInitialized = true;

        if (_testerKeyboardCombo is not null)
        {
            _testerKeyboardCombo.ItemsSource = _devices;
            _testerKeyboardCombo.DisplayMemberPath = nameof(KeyboardDevice.DisplayName);
            _testerKeyboardCombo.SelectionChanged += TesterKeyboardCombo_SelectionChanged;
        }

        if (_testerHistoryList is not null)
        {
            _testerHistoryList.ItemsSource = _keyboardTestHistory;
        }

        _devices.CollectionChanged += TesterDevices_CollectionChanged;
        _deviceBlockerService.KeyReceived += KeyboardTester_KeyReceived;

        BuildKeyboardTesterLayout();
        RefreshTesterKeyboardSelector();
        UpdateTesterCountText();
    }

    private void TesterDevices_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshTesterKeyboardSelector();
    }

    private void TesterKeyboardCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTesterSelectedKeyboardText();
    }

    private void ToggleKeyboardTester_Click(object sender, RoutedEventArgs e)
    {
        if (_isKeyboardTesting)
        {
            _isKeyboardTesting = false;
            if (_toggleKeyboardTesterButton is not null)
            {
                _toggleKeyboardTesterButton.Content = "Start Testing";
            }

            if (_testerLastKeyText is not null)
            {
                _testerLastKeyText.Text = "Testing stopped";
            }

            UpdateStatus("Keyboard tester stopped");
            return;
        }

        if (SelectedTesterKeyboard() is null)
        {
            UpdateStatus("Select a keyboard to test first");
            return;
        }

        _isKeyboardTesting = true;
        if (_toggleKeyboardTesterButton is not null)
        {
            _toggleKeyboardTesterButton.Content = "Stop Testing";
        }

        if (_testerLastKeyText is not null)
        {
            _testerLastKeyText.Text = "Listening for key presses from the selected keyboard...";
        }

        UpdateStatus("Keyboard tester active");
    }

    private void ResetKeyboardTester_Click(object sender, RoutedEventArgs e)
    {
        _keyboardTestHistory.Clear();
        _keyboardTesterStates.Clear();

        foreach (var button in _keyboardTesterButtons.Values)
        {
            ApplyTesterButtonState(button, "Normal");
        }

        if (_testerLastKeyText is not null)
        {
            _testerLastKeyText.Text = "Press Start Testing, then press keys on the selected keyboard.";
        }

        UpdateTesterCountText();
        UpdateStatus("Keyboard tester reset");
    }

    private void KeyboardTester_KeyReceived(object? sender, DeviceKeyEventArgs e)
    {
        if (!_isKeyboardTesting)
        {
            return;
        }

        Dispatcher.Invoke(() =>
        {
            var selected = SelectedTesterKeyboard();
            if (selected is null)
            {
                return;
            }

            var eventDevice = EnsureDeviceFromEvent(e);
            if (!IsSameKeyboard(selected, eventDevice))
            {
                return;
            }

            MarkTesterKey(eventDevice, e);
        });
    }

    private void MarkTesterKey(KeyboardDevice device, DeviceKeyEventArgs e)
    {
        var keyId = BuildTesterKeyId(e.ScanCode, e.IsExtendedKey);
        var remapRule = FindTesterRemapRule(device, e.ScanCode, e.IsExtendedKey);
        var action = e.WasBlocked
            ? "Blocked"
            : remapRule is not null
                ? $"Remapped to {remapRule.ToKeyName}"
                : "Allowed";

        var finalState = e.WasBlocked
            ? "Blocked"
            : remapRule is not null
                ? "Remapped"
                : "Tested";

        if (_keyboardTesterButtons.TryGetValue(keyId, out var button))
        {
            ApplyTesterButtonState(button, "Pressed");

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _keyboardTesterStates[keyId] = finalState;
                ApplyTesterButtonState(button, finalState);
                UpdateTesterCountText();
            };
            timer.Start();
        }

        var displayName = string.IsNullOrWhiteSpace(e.KeyName) ? $"Scan {e.ScanCode}" : e.KeyName;
        if (_testerLastKeyText is not null)
        {
            _testerLastKeyText.Text = $"{displayName} from {device.DisplayName} — {action}";
        }

        _keyboardTestHistory.Insert(0, new KeyboardTestHistoryItem
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Key = displayName,
            ScanCode = e.ScanCode.ToString(),
            Extended = e.IsExtendedKey ? "Yes" : "No",
            Action = action
        });

        while (_keyboardTestHistory.Count > 25)
        {
            _keyboardTestHistory.RemoveAt(_keyboardTestHistory.Count - 1);
        }
    }

    private KeyRemapRule? FindTesterRemapRule(KeyboardDevice device, ushort scanCode, bool isExtendedKey)
    {
        return _remapRules.FirstOrDefault(rule =>
            rule.IsEnabled &&
            IsSameKeyboard(rule, device) &&
            rule.FromScanCode == scanCode &&
            rule.FromIsExtendedKey == isExtendedKey);
    }

    private KeyboardDevice? SelectedTesterKeyboard()
    {
        if (_testerKeyboardCombo?.SelectedItem is KeyboardDevice selected)
        {
            return selected;
        }

        if (_devices.Count == 1)
        {
            return _devices[0];
        }

        return KeyboardList.SelectedItem as KeyboardDevice;
    }

    private void RefreshTesterKeyboardSelector()
    {
        if (!_keyboardTesterInitialized)
        {
            return;
        }

        if (_testerKeyboardCombo is not null)
        {
            if (_devices.Count <= 1)
            {
                _testerKeyboardCombo.Visibility = Visibility.Collapsed;
                if (_devices.Count == 1)
                {
                    _testerKeyboardCombo.SelectedItem = _devices[0];
                }
            }
            else
            {
                _testerKeyboardCombo.Visibility = Visibility.Visible;
                if (_testerKeyboardCombo.SelectedItem is not KeyboardDevice || !_devices.Contains((KeyboardDevice)_testerKeyboardCombo.SelectedItem))
                {
                    _testerKeyboardCombo.SelectedIndex = 0;
                }
            }
        }

        if (_testerDropdownHintText is not null)
        {
            _testerDropdownHintText.Text = _devices.Count <= 1
                ? "Only one keyboard is detected, so it is selected automatically."
                : "Multiple keyboards detected. Choose the exact keyboard you want to test.";
        }

        UpdateTesterSelectedKeyboardText();
    }

    private void UpdateTesterSelectedKeyboardText()
    {
        if (_testerSelectedKeyboardText is null)
        {
            return;
        }

        var selected = SelectedTesterKeyboard();
        _testerSelectedKeyboardText.Text = selected is null
            ? "No keyboard selected"
            : $"Testing: {selected.DisplayName}";
    }

    private void UpdateTesterCountText()
    {
        if (_testerCountText is null)
        {
            return;
        }

        var testedCount = _keyboardTesterStates.Count(pair => pair.Value != "Normal");
        _testerCountText.Text = $"{testedCount} visual key(s) tested";
    }

    private void BuildKeyboardTesterLayout()
    {
        if (_keyboardTesterLayoutPanel is null || _keyboardTesterButtons.Count > 0)
        {
            return;
        }

        AddTesterRow(
            TesterKey("Esc", 0x01), TesterKey("F1", 0x3B), TesterKey("F2", 0x3C), TesterKey("F3", 0x3D), TesterKey("F4", 0x3E),
            TesterKey("F5", 0x3F), TesterKey("F6", 0x40), TesterKey("F7", 0x41), TesterKey("F8", 0x42),
            TesterKey("F9", 0x43), TesterKey("F10", 0x44), TesterKey("F11", 0x57), TesterKey("F12", 0x58));

        AddTesterRow(
            TesterKey("`", 0x29), TesterKey("1", 0x02), TesterKey("2", 0x03), TesterKey("3", 0x04), TesterKey("4", 0x05), TesterKey("5", 0x06),
            TesterKey("6", 0x07), TesterKey("7", 0x08), TesterKey("8", 0x09), TesterKey("9", 0x0A), TesterKey("0", 0x0B), TesterKey("-", 0x0C),
            TesterKey("=", 0x0D), TesterKey("Backspace", 0x0E, false, 96));

        AddTesterRow(
            TesterKey("Tab", 0x0F, false, 72), TesterKey("Q", 0x10), TesterKey("W", 0x11), TesterKey("E", 0x12), TesterKey("R", 0x13), TesterKey("T", 0x14),
            TesterKey("Y", 0x15), TesterKey("U", 0x16), TesterKey("I", 0x17), TesterKey("O", 0x18), TesterKey("P", 0x19), TesterKey("[", 0x1A),
            TesterKey("]", 0x1B), TesterKey("\\", 0x2B, false, 72));

        AddTesterRow(
            TesterKey("Caps", 0x3A, false, 86), TesterKey("A", 0x1E), TesterKey("S", 0x1F), TesterKey("D", 0x20), TesterKey("F", 0x21), TesterKey("G", 0x22),
            TesterKey("H", 0x23), TesterKey("J", 0x24), TesterKey("K", 0x25), TesterKey("L", 0x26), TesterKey(";", 0x27), TesterKey("'", 0x28),
            TesterKey("Enter", 0x1C, false, 100));

        AddTesterRow(
            TesterKey("Shift", 0x2A, false, 112), TesterKey("Z", 0x2C), TesterKey("X", 0x2D), TesterKey("C", 0x2E), TesterKey("V", 0x2F), TesterKey("B", 0x30),
            TesterKey("N", 0x31), TesterKey("M", 0x32), TesterKey(",", 0x33), TesterKey(".", 0x34), TesterKey("/", 0x35), TesterKey("R Shift", 0x36, false, 126));

        AddTesterRow(
            TesterKey("Ctrl", 0x1D), TesterKey("Win", 0x5B, true), TesterKey("Alt", 0x38), TesterKey("Space", 0x39, false, 270),
            TesterKey("Right Alt", 0x38, true, 86), TesterKey("Menu", 0x5D, true), TesterKey("R Ctrl", 0x1D, true),
            TesterKey("←", 0x4B, true), TesterKey("↑", 0x48, true), TesterKey("↓", 0x50, true), TesterKey("→", 0x4D, true));

        AddTesterRow(
            TesterKey("Num", 0x45), TesterKey("/", 0x35, true), TesterKey("*", 0x37), TesterKey("-", 0x4A),
            TesterKey("7", 0x47), TesterKey("8", 0x48), TesterKey("9", 0x49), TesterKey("+", 0x4E),
            TesterKey("4", 0x4B), TesterKey("5", 0x4C), TesterKey("6", 0x4D), TesterKey("N Enter", 0x1C, true, 72));
    }

    private void AddTesterRow(params TesterKeyDefinition[] keys)
    {
        if (_keyboardTesterLayoutPanel is null)
        {
            return;
        }

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var key in keys)
        {
            var button = new Button
            {
                Content = key.Label,
                Width = key.Width,
                Height = 42,
                Margin = new Thickness(3),
                Padding = new Thickness(6, 4, 6, 4),
                FontWeight = FontWeights.SemiBold,
                Focusable = false,
                IsHitTestVisible = false
            };

            var keyId = BuildTesterKeyId(key.ScanCode, key.IsExtendedKey);
            _keyboardTesterButtons[keyId] = button;
            _keyboardTesterStates[keyId] = "Normal";
            ApplyTesterButtonState(button, "Normal");
            row.Children.Add(button);
        }

        _keyboardTesterLayoutPanel.Children.Add(row);
    }

    private void ApplyTesterButtonState(Button button, string state)
    {
        switch (state)
        {
            case "Pressed":
                button.SetResourceReference(Control.BackgroundProperty, "PrimaryBrush");
                button.Foreground = System.Windows.Media.Brushes.White;
                break;
            case "Blocked":
                button.SetResourceReference(Control.BackgroundProperty, "WarningBackground");
                button.SetResourceReference(Control.ForegroundProperty, "WarningText");
                break;
            case "Remapped":
                button.SetResourceReference(Control.BackgroundProperty, "InfoBackground");
                button.SetResourceReference(Control.ForegroundProperty, "PrimaryBrush");
                break;
            case "Tested":
                button.SetResourceReference(Control.BackgroundProperty, "PrimarySoftBrush");
                button.SetResourceReference(Control.ForegroundProperty, "TextPrimary");
                break;
            default:
                button.SetResourceReference(Control.BackgroundProperty, "PanelBackground");
                button.SetResourceReference(Control.ForegroundProperty, "TextPrimary");
                break;
        }

        button.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
    }

    private static TesterKeyDefinition TesterKey(string label, ushort scanCode, bool isExtendedKey = false, double width = 48)
    {
        return new TesterKeyDefinition(label, scanCode, isExtendedKey, width);
    }

    private static string BuildTesterKeyId(ushort scanCode, bool isExtendedKey)
    {
        return $"{scanCode}|{isExtendedKey}";
    }

    private sealed record TesterKeyDefinition(string Label, ushort ScanCode, bool IsExtendedKey, double Width);
}

public sealed class KeyboardTestHistoryItem
{
    public string Time { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string ScanCode { get; init; } = string.Empty;
    public string Extended { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
}
