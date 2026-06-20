using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using KeyDisabler.App.Services;

namespace KeyDisabler.App;

internal static class MainWindowBrandingBootstrap
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
            window.ApplyBrandingAndSingleInstanceHooks();
        }
    }
}

public partial class MainWindow
{
    private bool _singleInstanceHookAttached;

    internal void ApplyBrandingAndSingleInstanceHooks()
    {
        Icon = BrandAssetService.LoadWindowIcon();
        AboutLogoImage.Source = BrandAssetService.LoadAboutLogo();
        UseFullAboutLogoLayout();

        if (_singleInstanceHookAttached)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(SingleInstanceWndProc);
        _singleInstanceHookAttached = true;
    }

    private void UseFullAboutLogoLayout()
    {
        AboutLogoImage.Width = 780;
        AboutLogoImage.Height = 130;
        AboutLogoImage.HorizontalAlignment = HorizontalAlignment.Left;
        AboutLogoImage.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(AboutLogoImage, 0);
        Grid.SetColumnSpan(AboutLogoImage, 2);

        if (AboutLogoImage.Parent is not Grid logoGrid)
        {
            return;
        }

        foreach (UIElement child in logoGrid.Children)
        {
            if (!ReferenceEquals(child, AboutLogoImage))
            {
                child.Visibility = Visibility.Collapsed;
            }
        }
    }

    private IntPtr SingleInstanceWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != SingleInstanceService.ShowMessageId)
        {
            return IntPtr.Zero;
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
        handled = true;
        return IntPtr.Zero;
    }
}
