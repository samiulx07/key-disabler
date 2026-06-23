using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using WpfBorder = System.Windows.Controls.Border;
using WpfButton = System.Windows.Controls.Button;
using WpfControl = System.Windows.Controls.Control;
using WpfGrid = System.Windows.Controls.Grid;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

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
    private const double TesterKeyHeight = 50;
    private const double TesterKeyWidth = 50;
    private const double TesterKeyGap = 4;

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

        var root = FindKeyboardLayoutAncestor<WpfBorder>(combo);
        if (root is null)
        {
            return;
        }

        foreach (var textBlock in FindKeyboardLayoutChildren<WpfTextBlock>(root))
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

        var historyBorder = FindKeyboardLayoutAncestor<WpfBorder>(historyList);
        if (historyBorder is not null)
        {
            historyBorder.Visibility = Visibility.Collapsed;
        }

        var parentGrid = historyBorder is null ? null : FindKeyboardLayoutAncestor<WpfGrid>(historyBorder);
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

        var keyboardShell = new WpfBorder
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 4, 0, 0),
            Background = new LinearGradientBrush(
                Color.FromRgb(26, 24, 24),
                Color.FromRgb(9, 9, 9),
                new Point(0, 0),
                new Point(0, 1)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(82, 42, 42)),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(255, 29, 45),
                BlurRadius = 18,
                ShadowDepth = 0,
                Opacity = 0.16
            }
        };

        var keyboard = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        keyboard.Children.Add(BuildReferenceMainKeyboardSection());
        keyboard.Children.Add(CreateReferenceSpacer(18));
        keyboard.Children.Add(BuildReferenceNavigationSection());
        keyboard.Children.Add(CreateReferenceSpacer(18));
        keyboard.Children.Add(BuildReferenceNumpadSection());

        keyboardShell.Child = keyboard;
        _keyboardTesterLayoutPanel.Children.Add(keyboardShell);

        UpdateTesterCountText();
    }

    private WpfStackPanel BuildReferenceMainKeyboardSection()
    {
        var section = new WpfStackPanel { Orientation = WpfOrientation.Vertical };

        AddReferenceKeyboardRow(section,
            ReferenceKey("Esc", 0x01, false, 56), CreateReferenceSpacer(36),
            ReferenceKey("F1", 0x3B), ReferenceKey("F2", 0x3C), ReferenceKey("F3", 0x3D), ReferenceKey("F4", 0x3E), CreateReferenceSpacer(16),
            ReferenceKey("F5", 0x3F), ReferenceKey("F6", 0x40), ReferenceKey("F7", 0x41), ReferenceKey("F8", 0x42), CreateReferenceSpacer(16),
            ReferenceKey("F9", 0x43), ReferenceKey("F10", 0x44), ReferenceKey("F11", 0x57), ReferenceKey("F12", 0x58));

        AddReferenceKeyboardRow(section,
            ReferenceKey("~\n`", 0x29), ReferenceKey("!\n1", 0x02), ReferenceKey("@\n2", 0x03), ReferenceKey("#\n3", 0x04), ReferenceKey("$\n4", 0x05), ReferenceKey("%\n5", 0x06),
            ReferenceKey("^\n6", 0x07), ReferenceKey("&&\n7", 0x08), ReferenceKey("*\n8", 0x09), ReferenceKey("(\n9", 0x0A), ReferenceKey(")\n0", 0x0B), ReferenceKey("_\n-", 0x0C),
            ReferenceKey("+\n=", 0x0D), ReferenceKey("Backspace", 0x0E, false, 106));

        AddReferenceKeyboardRow(section,
            ReferenceKey("Tab\n↹", 0x0F, false, 76), ReferenceKey("Q", 0x10), ReferenceKey("W", 0x11), ReferenceKey("E", 0x12), ReferenceKey("R", 0x13), ReferenceKey("T", 0x14),
            ReferenceKey("Y", 0x15), ReferenceKey("U", 0x16), ReferenceKey("I", 0x17), ReferenceKey("O", 0x18), ReferenceKey("P", 0x19), ReferenceKey("{\n[", 0x1A),
            ReferenceKey("}\n]", 0x1B), ReferenceKey("|\n\\", 0x2B, false, 76));

        AddReferenceKeyboardRow(section,
            ReferenceKey("Caps\nLock", 0x3A, false, 92), ReferenceKey("A", 0x1E), ReferenceKey("S", 0x1F), ReferenceKey("D", 0x20), ReferenceKey("F", 0x21), ReferenceKey("G", 0x22),
            ReferenceKey("H", 0x23), ReferenceKey("J", 0x24), ReferenceKey("K", 0x25), ReferenceKey("L", 0x26), ReferenceKey(":\n;", 0x27), ReferenceKey("\"\n'", 0x28),
            ReferenceKey("Enter", 0x1C, false, 126));

        AddReferenceKeyboardRow(section,
            ReferenceKey("Shift", 0x2A, false, 116), ReferenceKey("Z", 0x2C), ReferenceKey("X", 0x2D), ReferenceKey("C", 0x2E), ReferenceKey("V", 0x2F), ReferenceKey("B", 0x30),
            ReferenceKey("N", 0x31), ReferenceKey("M", 0x32), ReferenceKey("<\n,", 0x33), ReferenceKey(">\n.", 0x34), ReferenceKey("?\n/", 0x35), ReferenceKey("Shift", 0x36, false, 158));

        AddReferenceKeyboardRow(section,
            ReferenceKey("Ctrl", 0x1D, false, 62), ReferenceKey("⊞", 0x5B, true, 62), ReferenceKey("Alt", 0x38, false, 66),
            ReferenceKey(string.Empty, 0x39, false, 304), ReferenceKey("Alt", 0x38, true, 62), ReferenceKey("⊞", 0x5B, true, 62),
            ReferenceKey("▤", 0x5D, true, 62), ReferenceKey("Ctrl", 0x1D, true, 62));

        return section;
    }

    private WpfGrid BuildReferenceNavigationSection()
    {
        var grid = CreateReferenceGrid(columns: 3, rows: 6);

        AddReferenceKeyToGrid(grid, ReferenceKey("Print\nScreen", 0x37, true), 0, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("Scroll\nLock", 0x46), 0, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("Pause\nBreak", 0x45, true), 0, 2);

        AddReferenceKeyToGrid(grid, ReferenceKey("Insert", 0x52, true), 1, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("Home", 0x47, true), 1, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("Page\nUp", 0x49, true), 1, 2);

        AddReferenceKeyToGrid(grid, ReferenceKey("Delete", 0x53, true), 2, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("End", 0x4F, true), 2, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("Page\nDown", 0x51, true), 2, 2);

        AddReferenceKeyToGrid(grid, ReferenceKey("↑", 0x48, true), 4, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("←", 0x4B, true), 5, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("↓", 0x50, true), 5, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("→", 0x4D, true), 5, 2);

        return grid;
    }

    private WpfGrid BuildReferenceNumpadSection()
    {
        var grid = CreateReferenceGrid(columns: 4, rows: 5);

        AddReferenceKeyToGrid(grid, ReferenceKey("Num\nLock", 0x45), 0, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("/", 0x35, true), 0, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("*", 0x37), 0, 2);
        AddReferenceKeyToGrid(grid, ReferenceKey("-", 0x4A), 0, 3);

        AddReferenceKeyToGrid(grid, ReferenceKey("7\nHome", 0x47), 1, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("8\n↑", 0x48), 1, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("9\nPgUp", 0x49), 1, 2);
        AddReferenceKeyToGrid(grid, ReferenceKey("+", 0x4E), 1, 3, rowSpan: 2);

        AddReferenceKeyToGrid(grid, ReferenceKey("4\n←", 0x4B), 2, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("5", 0x4C), 2, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("6\n→", 0x4D), 2, 2);

        AddReferenceKeyToGrid(grid, ReferenceKey("1\nEnd", 0x4F), 3, 0);
        AddReferenceKeyToGrid(grid, ReferenceKey("2\n↓", 0x50), 3, 1);
        AddReferenceKeyToGrid(grid, ReferenceKey("3\nPgDn", 0x51), 3, 2);
        AddReferenceKeyToGrid(grid, ReferenceKey("Enter", 0x1C, true), 3, 3, rowSpan: 2);

        AddReferenceKeyToGrid(grid, ReferenceKey("0\nIns", 0x52), 4, 0, columnSpan: 2);
        AddReferenceKeyToGrid(grid, ReferenceKey(".\nDel", 0x53), 4, 2);

        return grid;
    }

    private WpfGrid CreateReferenceGrid(int columns, int rows)
    {
        var grid = new WpfGrid();

        for (var column = 0; column < columns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(TesterKeyWidth + TesterKeyGap) });
        }

        for (var row = 0; row < rows; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(TesterKeyHeight + TesterKeyGap) });
        }

        return grid;
    }

    private void AddReferenceKeyboardRow(WpfStackPanel section, params FrameworkElement[] elements)
    {
        var row = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, TesterKeyGap)
        };

        foreach (var element in elements)
        {
            row.Children.Add(element);
        }

        section.Children.Add(row);
    }

    private WpfButton ReferenceKey(string label, ushort scanCode, bool isExtendedKey = false, double width = TesterKeyWidth)
    {
        var button = new WpfButton
        {
            Content = label,
            Width = width,
            MinWidth = width,
            Height = TesterKeyHeight,
            MinHeight = TesterKeyHeight,
            Margin = new Thickness(2),
            Padding = new Thickness(4),
            FontSize = label.Contains('\n') ? 10.5 : 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = false,
            IsHitTestVisible = false,
            Template = CreateReferenceKeyTemplate(),
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 8,
                ShadowDepth = 2,
                Opacity = 0.75
            }
        };

        var keyId = BuildTesterKeyId(scanCode, isExtendedKey);
        _keyboardTesterButtons[keyId] = button;
        _keyboardTesterStates[keyId] = "Normal";
        ApplyTesterButtonState(button, "Normal");
        return button;
    }

    private static ControlTemplate CreateReferenceKeyTemplate()
    {
        var template = new ControlTemplate(typeof(WpfButton));

        var outer = new FrameworkElementFactory(typeof(WpfBorder));
        outer.SetValue(WpfBorder.CornerRadiusProperty, new CornerRadius(7));
        outer.SetValue(WpfBorder.BorderThicknessProperty, new Thickness(1));
        outer.SetBinding(WpfBorder.BackgroundProperty, new TemplateBindingExtension(WpfControl.BackgroundProperty));
        outer.SetBinding(WpfBorder.BorderBrushProperty, new TemplateBindingExtension(WpfControl.BorderBrushProperty));
        outer.SetValue(WpfBorder.PaddingProperty, new Thickness(2));

        var inner = new FrameworkElementFactory(typeof(WpfBorder));
        inner.SetValue(WpfBorder.CornerRadiusProperty, new CornerRadius(5));
        inner.SetValue(WpfBorder.BorderThicknessProperty, new Thickness(1));
        inner.SetValue(WpfBorder.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)));
        inner.SetValue(WpfBorder.PaddingProperty, new Thickness(3));
        inner.SetBinding(WpfBorder.BackgroundProperty, new TemplateBindingExtension(WpfControl.BackgroundProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
        presenter.SetValue(ContentPresenter.SnapsToDevicePixelsProperty, true);

        inner.AppendChild(presenter);
        outer.AppendChild(inner);
        template.VisualTree = outer;

        return template;
    }

    private void AddReferenceKeyToGrid(WpfGrid grid, WpfButton button, int row, int column, int columnSpan = 1, int rowSpan = 1)
    {
        WpfGrid.SetRow(button, row);
        WpfGrid.SetColumn(button, column);
        WpfGrid.SetColumnSpan(button, columnSpan);
        WpfGrid.SetRowSpan(button, rowSpan);
        button.Height = double.NaN;
        button.MinHeight = TesterKeyHeight;
        grid.Children.Add(button);
    }

    private static WpfBorder CreateReferenceSpacer(double width)
    {
        return new WpfBorder { Width = width };
    }

    private static T? FindKeyboardLayoutAncestor<T>(DependencyObject start) where T : DependencyObject
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

    private static IEnumerable<T> FindKeyboardLayoutChildren<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindKeyboardLayoutChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
