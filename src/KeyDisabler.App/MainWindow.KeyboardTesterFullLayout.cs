using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfBorder = System.Windows.Controls.Border;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace KeyDisabler.App;

internal static class MainWindowKeyboardTesterFullLayoutBootstrap
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
            window.ScheduleKeyboardTesterFullLayout();
        }
    }
}

public partial class MainWindow
{
    internal void ScheduleKeyboardTesterFullLayout()
    {
        var attempts = 0;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) =>
        {
            attempts++;
            if (_keyboardTesterLayoutPanel is null && attempts < 20)
            {
                return;
            }

            timer.Stop();
            if (_keyboardTesterLayoutPanel is not null)
            {
                ApplyKeyboardTesterFullLayout();
            }
        };
        timer.Start();
    }

    private void ApplyKeyboardTesterFullLayout()
    {
        HideKeyboardTesterIntroText();
        HideKeyboardTesterHistoryColumn();
        RebuildKeyboardTesterFullLayout();
    }

    private void HideKeyboardTesterIntroText()
    {
        if (FindName("TesterKeyboardCombo") is not FrameworkElement combo)
        {
            return;
        }

        var root = FindAncestorLocal<WpfBorder>(combo);
        if (root is null)
        {
            return;
        }

        foreach (var textBlock in FindVisualChildrenLocal<WpfTextBlock>(root))
        {
            if (string.Equals(textBlock.Text, "Keyboard Tester", StringComparison.OrdinalIgnoreCase) ||
                textBlock.Text.StartsWith("Select a keyboard, start testing", StringComparison.OrdinalIgnoreCase))
            {
                textBlock.Visibility = Visibility.Collapsed;
                textBlock.Margin = new Thickness(0);
            }
        }
    }

    private void HideKeyboardTesterHistoryColumn()
    {
        if (FindName("TesterHistoryList") is not FrameworkElement historyList)
        {
            return;
        }

        var historyBorder = FindAncestorLocal<WpfBorder>(historyList);
        if (historyBorder is not null)
        {
            historyBorder.Visibility = Visibility.Collapsed;
        }

        var parentGrid = historyBorder is null ? null : FindAncestorLocal<WpfGrid>(historyBorder);
        if (parentGrid?.ColumnDefinitions.Count >= 2)
        {
            parentGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            parentGrid.ColumnDefinitions[1].Width = new GridLength(0);
        }
    }

    private void RebuildKeyboardTesterFullLayout()
    {
        if (_keyboardTesterLayoutPanel is null)
        {
            return;
        }

        _keyboardTesterLayoutPanel.Children.Clear();
        _keyboardTesterButtons.Clear();
        _keyboardTesterStates.Clear();

        AddFullKeyboardRow(
            Key("Esc", 0x01), Gap(18),
            Key("F1", 0x3B), Key("F2", 0x3C), Key("F3", 0x3D), Key("F4", 0x3E), Gap(12),
            Key("F5", 0x3F), Key("F6", 0x40), Key("F7", 0x41), Key("F8", 0x42), Gap(12),
            Key("F9", 0x43), Key("F10", 0x44), Key("F11", 0x57), Key("F12", 0x58), Gap(18),
            Key("PrtSc", 0x37, true), Key("Scroll", 0x46), Key("Pause", 0x45, true));

        AddFullKeyboardRow(
            Key("`", 0x29), Key("1", 0x02), Key("2", 0x03), Key("3", 0x04), Key("4", 0x05), Key("5", 0x06),
            Key("6", 0x07), Key("7", 0x08), Key("8", 0x09), Key("9", 0x0A), Key("0", 0x0B), Key("-", 0x0C), Key("=", 0x0D), Key("Backspace", 0x0E, false, 108), Gap(18),
            Key("Insert", 0x52, true), Key("Home", 0x47, true), Key("PgUp", 0x49, true), Gap(18),
            Key("Num", 0x45), Key("/", 0x35, true), Key("*", 0x37), Key("-", 0x4A));

        AddFullKeyboardRow(
            Key("Tab", 0x0F, false, 74), Key("Q", 0x10), Key("W", 0x11), Key("E", 0x12), Key("R", 0x13), Key("T", 0x14),
            Key("Y", 0x15), Key("U", 0x16), Key("I", 0x17), Key("O", 0x18), Key("P", 0x19), Key("[", 0x1A), Key("]", 0x1B), Key("\\", 0x2B, false, 82), Gap(18),
            Key("Delete", 0x53, true), Key("End", 0x4F, true), Key("PgDn", 0x51, true), Gap(18),
            Key("7", 0x47), Key("8", 0x48), Key("9", 0x49), Key("+", 0x4E));

        AddFullKeyboardRow(
            Key("Caps", 0x3A, false, 88), Key("A", 0x1E), Key("S", 0x1F), Key("D", 0x20), Key("F", 0x21), Key("G", 0x22),
            Key("H", 0x23), Key("J", 0x24), Key("K", 0x25), Key("L", 0x26), Key(";", 0x27), Key("'", 0x28), Key("Enter", 0x1C, false, 130), Gap(202),
            Key("4", 0x4B), Key("5", 0x4C), Key("6", 0x4D), Key("+", 0x4E));

        AddFullKeyboardRow(
            Key("Shift", 0x2A, false, 116), Key("Z", 0x2C), Key("X", 0x2D), Key("C", 0x2E), Key("V", 0x2F), Key("B", 0x30),
            Key("N", 0x31), Key("M", 0x32), Key(",", 0x33), Key(".", 0x34), Key("/", 0x35), Key("R Shift", 0x36, false, 148), Gap(78),
            Key("↑", 0x48, true), Gap(78),
            Key("1", 0x4F), Key("2", 0x50), Key("3", 0x51), Key("Enter", 0x1C, true));

        AddFullKeyboardRow(
            Key("Ctrl", 0x1D), Key("Win", 0x5B, true), Key("Alt", 0x38), Key("Space", 0x39, false, 318), Key("Right Alt", 0x38, true, 86),
            Key("Menu", 0x5D, true), Key("R Ctrl", 0x1D, true, 70), Gap(18),
            Key("←", 0x4B, true), Key("↓", 0x50, true), Key("→", 0x4D, true), Gap(18),
            Key("0", 0x52, false, 104), Key(".", 0x53), Key("Enter", 0x1C, true));

        UpdateTesterCountText();
    }

    private void AddFullKeyboardRow(params FullKeyboardItem[] items)
    {
        if (_keyboardTesterLayoutPanel is null)
        {
            return;
        }

        var row = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var item in items)
        {
            if (item.IsGap)
            {
                row.Children.Add(new WpfBorder { Width = item.Width, Height = 42, Margin = new Thickness(3, 0, 3, 0) });
                continue;
            }

            var button = new WpfButton
            {
                Content = item.Label,
                Width = item.Width,
                Height = 42,
                Margin = new Thickness(3),
                Padding = new Thickness(6, 4, 6, 4),
                FontWeight = FontWeights.SemiBold,
                Focusable = false,
                IsHitTestVisible = false
            };

            var keyId = BuildTesterKeyId(item.ScanCode, item.IsExtendedKey);
            _keyboardTesterButtons[keyId] = button;
            _keyboardTesterStates[keyId] = "Normal";
            ApplyTesterButtonState(button, "Normal");
            row.Children.Add(button);
        }

        _keyboardTesterLayoutPanel.Children.Add(row);
    }

    private static FullKeyboardItem Key(string label, ushort scanCode, bool isExtendedKey = false, double width = 52)
    {
        return new FullKeyboardItem(label, scanCode, isExtendedKey, width, false);
    }

    private static FullKeyboardItem Gap(double width)
    {
        return new FullKeyboardItem(string.Empty, 0, false, width, true);
    }

    private static T? FindAncestorLocal<T>(DependencyObject start) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(start);
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildrenLocal<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildrenLocal<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed record FullKeyboardItem(string Label, ushort ScanCode, bool IsExtendedKey, double Width, bool IsGap);
}
