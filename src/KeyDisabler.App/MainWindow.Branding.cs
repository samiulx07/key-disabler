using System.Windows;
using System.Windows.Interop;
using KeyDisabler.App.Services;

namespace KeyDisabler.App;

internal static class MainWindowBrandingBootstrap
{
    [System.Runtime.CompilerServices.ModuleInitializer]
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
            window.ApplyBranding();
        }
    }
}

public partial class MainWindow
{
    internal void ApplyBranding()
    {
        Icon = BrandAssetService.LoadWindowIcon();

        // Load the square icon (AppIcon.png) for the Dev Options icon area
        var iconImage = BrandAssetService.LoadAppIconImage();
        if (iconImage is not null)
        {
            AboutLogoImage.Source = iconImage;
        }
        else
        {
            // Fall back to the full horizontal logo if icon is missing
            AboutLogoImage.Source = BrandAssetService.LoadAboutLogo();
        }
    }
}
