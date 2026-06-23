using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace KeyDisabler.App;

internal static class MainWindowRemapTabNamingBootstrap
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
            window.Dispatcher.BeginInvoke(new Action(window.ApplyRemapTabNaming), DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    internal void ApplyRemapTabNaming()
    {
        RemapTab.Header = "⇄  Remap Key";

        foreach (var button in FindRemapTabNamingVisualChildren<Button>(this))
        {
            if (button.Content is string text && text.Contains("Open Remap Tab", StringComparison.OrdinalIgnoreCase))
            {
                button.Content = "Open Remap Key Tab  ›";
            }
        }
    }

    private static IEnumerable<T> FindRemapTabNamingVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindRemapTabNamingVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
