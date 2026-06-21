using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfDock = System.Windows.Controls.Dock;
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
            window.Dispatcher.BeginInvoke(new Action(window.InitializeRuleListActions), DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    private bool _ruleListActionsInitialized;

    internal void InitializeRuleListActions()
    {
        if (_ruleListActionsInitialized)
        {
            return;
        }

        _ruleListActionsInitialized = true;
        _rules.CollectionChanged += RuleCollections_CollectionChanged;
        _disabledKeyboards.CollectionChanged += RuleCollections_CollectionChanged;
        _remapRules.CollectionChanged += RuleCollections_CollectionChanged;

        ApplySavedRuleHeaders();
        Dispatcher.BeginInvoke(new Action(UpdateProtectionSummary), DispatcherPriority.ApplicationIdle);
    }

    private void RuleCollections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(UpdateProtectionSummary), DispatcherPriority.ApplicationIdle);
    }

    private void ApplySavedRuleHeaders()
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

        if (FindVisualChildByNameForRuleHeaders<FrameworkElement>(grid, "SavedBlockRulesActionHeader") is not null)
        {
            return;
        }

        HideTextBlockInRuleHeaderRow(grid, 0, "Saved block rules");

        var header = BuildRuleActionHeader("Saved Block Rules", RemoveRule_Click, ResetBlockRules_Click);
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

        if (FindVisualChildByNameForRuleHeaders<FrameworkElement>(grid, "SavedRemapRulesActionHeader") is not null)
        {
            return;
        }

        HideRuleHeaderGridRow(grid, 2);

        var header = BuildRuleActionHeader("Saved Remap Rules", RemoveRemapRule_Click, ResetRemapRules_Click);
        header.Name = "SavedRemapRulesActionHeader";
        WpfGrid.SetRow(header, 2);
        grid.Children.Add(header);
    }

    private WpfDockPanel BuildRuleActionHeader(string title, RoutedEventHandler removeHandler, RoutedEventHandler resetHandler)
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
        WpfDockPanel.SetDock(actions, WpfDock.Right);

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

    private void ResetBlockRules_Click(object sender, RoutedEventArgs e)
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
        UpdateStatus("All saved block rules reset");
    }

    private void ResetRemapRules_Click(object sender, RoutedEventArgs e)
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
        UpdateStatus("All saved remap rules reset");
    }

    private static void HideRuleHeaderGridRow(WpfGrid grid, int row)
    {
        foreach (UIElement child in grid.Children)
        {
            if (WpfGrid.GetRow(child) == row)
            {
                child.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static void HideTextBlockInRuleHeaderRow(WpfGrid grid, int row, string text)
    {
        foreach (var textBlock in FindVisualChildrenForRuleHeaders<WpfTextBlock>(grid))
        {
            if (WpfGrid.GetRow(textBlock) == row && string.Equals(textBlock.Text, text, StringComparison.OrdinalIgnoreCase))
            {
                textBlock.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildrenForRuleHeaders<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildrenForRuleHeaders<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static T? FindVisualChildByNameForRuleHeaders<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        foreach (var child in FindVisualChildrenForRuleHeaders<T>(root))
        {
            if (string.Equals(child.Name, name, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
