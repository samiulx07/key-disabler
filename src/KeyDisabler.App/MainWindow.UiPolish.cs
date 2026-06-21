using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace KeyDisabler.App;

internal static class MainWindowUiPolishBootstrap
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
                new Action(window.ApplyUiPolish),
                DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    internal void ApplyUiPolish()
    {
        EnableAutoScrollForList("DisabledKeyboardsList");
        EnableAutoScrollForList("RulesList");
        EnableAutoScrollForList("RemapRulesList");
        EnableAutoScrollForList("TesterHistoryList");
        ApplyTabCornerAndBrandFix();
    }

    private void EnableAutoScrollForList(string listName)
    {
        if (FindName(listName) is not DependencyObject list)
        {
            return;
        }

        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Auto);
        ScrollViewer.SetCanContentScroll(list, true);
    }

    private void ApplyTabCornerAndBrandFix()
    {
        foreach (var tabControl in FindVisualChildren<TabControl>(this))
        {
            tabControl.SelectionChanged -= TabControl_SelectionChanged_ForPolish;
            tabControl.SelectionChanged += TabControl_SelectionChanged_ForPolish;

            foreach (var tabItem in FindVisualChildren<TabItem>(tabControl))
            {
                PolishTabItem(tabItem);
            }
        }
    }

    private void TabControl_SelectionChanged_ForPolish(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not TabControl tabControl)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            foreach (var tabItem in FindVisualChildren<TabItem>(tabControl))
            {
                PolishTabItem(tabItem);
            }
        }), DispatcherPriority.ApplicationIdle);
    }

    private void PolishTabItem(TabItem tabItem)
    {
        tabItem.ApplyTemplate();
        if (FindVisualChildByName<Border>(tabItem, "TabBorder") is Border border)
        {
            border.CornerRadius = new CornerRadius(14, 14, 14, 14);
        }

        if (tabItem.IsSelected)
        {
            tabItem.SetResourceReference(ForegroundProperty, "TabSelectedForeground");
        }
        else
        {
            tabItem.SetResourceReference(ForegroundProperty, "TabUnselectedForeground");
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null)
        {
            yield break;
        }

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
        if (root is null)
        {
            return null;
        }

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
