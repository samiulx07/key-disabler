using System.Runtime.CompilerServices;
using System.Windows;
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

        if (_singleInstanceHookAttached)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(SingleInstanceWndProc);
        _singleInstanceHookAttached = true;
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
