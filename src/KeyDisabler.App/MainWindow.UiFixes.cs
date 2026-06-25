using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfBorder = System.Windows.Controls.Border;
using WpfScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfSelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfTabItem = System.Windows.Controls.TabItem;

namespace KeyDisabler.App;

internal static class MainWindowUiFixesBootstrap
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
            window.Dispatcher.BeginInvoke(new Action(window.ApplyUiFixes), DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    internal void ApplyUiFixes()
    {
        EnableAutoScrollForList("DisabledKeyboardsList");
        EnableAutoScrollForList("RulesList");
        EnableAutoScrollForList("RemapRulesList");
        EnableAutoScrollForList("TesterHistoryList");
        ApplyTabCornerFix();
    }

    private void EnableAutoScrollForList(string listName)
    {
        if (FindName(listName) is not DependencyObject list)
        {
            return;
        }

        WpfScrollViewer.SetVerticalScrollBarVisibility(list, WpfScrollBarVisibility.Auto);
        WpfScrollViewer.SetHorizontalScrollBarVisibility(list, WpfScrollBarVisibility.Auto);
        WpfScrollViewer.SetCanContentScroll(list, true);
    }

    private void ApplyTabCornerFix()
    {
        foreach (var tabControl in FindVisualChildren<WpfTabControl>(this))
        {
            tabControl.SelectionChanged -= TabControl_SelectionChanged_ForFixes;
            tabControl.SelectionChanged += TabControl_SelectionChanged_ForFixes;

            foreach (var tabItem in FindVisualChildren<WpfTabItem>(tabControl))
            {
                FixTabItem(tabItem);
            }
        }
    }

    private void TabControl_SelectionChanged_ForFixes(object sender, WpfSelectionChangedEventArgs e)
    {
        if (sender is not WpfTabControl tabControl)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var tabItem in FindVisualChildren<WpfTabItem>(tabControl))
            {
                FixTabItem(tabItem);
            }
        }), DispatcherPriority.ApplicationIdle);
    }

    private void FixTabItem(WpfTabItem tabItem)
    {
        tabItem.ApplyTemplate();
        if (FindVisualChildByName<WpfBorder>(tabItem, "TabBorder") is WpfBorder border)
        {
            border.CornerRadius = new CornerRadius(14, 14, 14, 14);
            border.SnapsToDevicePixels = false;
        }

        tabItem.SetResourceReference(
            ForegroundProperty,
            tabItem.IsSelected ? "TabSelectedForeground" : "TabUnselectedForeground");
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static T? FindVisualChildByName<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild && string.Equals(typedChild.Name, name, StringComparison.Ordinal))
            {
                return typedChild;
            }

            var descendant = FindVisualChildByName<T>(child, name);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }
}
