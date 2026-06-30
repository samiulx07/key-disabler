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

        Dispatcher.BeginInvoke(new Action(UpdateProtectionSummary), DispatcherPriority.ApplicationIdle);
    }

    private void RuleCollections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(UpdateProtectionSummary), DispatcherPriority.ApplicationIdle);
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
}

