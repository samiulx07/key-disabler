using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfDockPanel = System.Windows.Controls.DockPanel;
using WpfGrid = System.Windows.Controls.Grid;
using WpfListView = System.Windows.Controls.ListView;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace KeyDisabler.App;

internal static class MainWindowRuleListActionsBootstrap
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
            window.Dispatcher.BeginInvoke(new Action(window.ApplyRuleListActionHeaders), DispatcherPriority.ContextIdle);
        }
    }
}

public partial class MainWindow
{
    internal void ApplyRuleListActionHeaders()
    {
        AddBlockRulesHeaderActions();
        AddRemapRulesHeaderActions();
    }

    private void AddBlockRulesHeaderActions()
    {
        if (FindName("RulesList") is not WpfListView rulesList || rulesList.Parent is not WpfGrid grid)
        {
            return;
        }

        if (FindVisualChildByNameLocal<FrameworkElement>(grid, "SavedBlockRulesActionHeader") is not null)
        {
            return;
        }

        HideTextBlockInGridRow(grid, 0, "Saved block rules");

        var header = BuildActionHeader("Saved Block Rules", RemoveRule_Click, ResetAllBlockRules_Click);
        header.Name = "SavedBlockRulesActionHeader";
        WpfGrid.SetRow(header, 0);
        grid.Children.Add(header);
    }

    private void AddRemapRulesHeaderActions()
    {
        if (FindName("RemapRulesList") is not WpfListView remapList || remapList.Parent is not WpfGrid grid)
        {
            return;
        }

        if (FindVisualChildByNameLocal<FrameworkElement>(grid, "SavedRemapRulesActionHeader") is not null)
        {
            return;
        }

        HideGridRowChildren(grid, 2);

        var header = BuildActionHeader("Saved Remap Rules", RemoveRemapRule_Click, ResetAllRemapRules_Click);
        header.Name = "SavedRemapRulesActionHeader";
        WpfGrid.SetRow(header, 2);
        grid.Children.Add(header);
    }

    private WpfDockPanel BuildActionHeader(string title, RoutedEventHandler removeHandler, RoutedEventHandler resetHandler)
    {
        var header = new WpfDockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0, 0, 0, 14)
        };

        var actions = new WpfStackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        WpfDockPanel.SetDock(actions, Dock.Right);

        var removeButton = new WpfButton
        {
            Content = "Remove Selected",
            Padding = new Thickness(12, 7, 12, 7),
            Margin = new Thickness(0, 0, 8, 0)
        };
        removeButton.Click += removeHandler;

        var resetButton = new WpfButton
        {
            Content = "Reset All",
            Padding = new Thickness(12, 7, 12, 7),
            Margin = new Thickness(0)
        };
        resetButton.Click += resetHandler;

        actions.Children.Add(removeButton);
        actions.Children.Add(resetButton);
        header.Children.Add(actions);

        var titleText = new WpfTextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleText.SetResourceReference(ForegroundProperty, "TextPrimary");
        header.Children.Add(titleText);

        return header;
    }

    private void ResetAllBlockRules_Click(object sender, RoutedEventArgs e)
    {
        if (_rules.Count == 0)
        {
            UpdateStatus("No block rules to reset");
            return;
        }

        _rules.Clear();
        SaveSettingsFromUi();
        UpdateDeviceBlocker();
        UpdateRuleCount();
        UpdateProtectionSummary();
        UpdateStatus("All block rules reset");
    }

    private void ResetAllRemapRules_Click(object sender, RoutedEventArgs e)
    {
        if (_remapRules.Count == 0)
        {
            UpdateStatus("No remap rules to reset");
            return;
        }

        _remapRules.Clear();
        SaveRemapSettings();
        UpdateRemapDeviceBlocker();
        UpdateProtectionSummary();
        UpdateStatus("All remap rules reset");
    }

    private static void HideGridRowChildren(WpfGrid grid, int row)
    {
        foreach (UIElement child in grid.Children)
        {
            if (WpfGrid.GetRow(child) == row)
            {
                child.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static void HideTextBlockInGridRow(WpfGrid grid, int row, string text)
    {
        foreach (var textBlock in FindVisualChildrenLocal<WpfTextBlock>(grid))
        {
            if (WpfGrid.GetRow(textBlock) == row && string.Equals(textBlock.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                textBlock.Visibility = Visibility.Collapsed;
            }
        }
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

    private static T? FindVisualChildByNameLocal<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        foreach (var child in FindVisualChildrenLocal<T>(root))
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
