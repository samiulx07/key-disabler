using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using KeyDisabler.App.Services;

namespace KeyDisabler.App;

internal static class MainWindowFooterDetectionBootstrap
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
            window.InitializeFooterDetectionControl();
        }
    }
}

public partial class MainWindow
{
    private bool _footerDetectionControlInitialized;

    internal void InitializeFooterDetectionControl()
    {
        if (_footerDetectionControlInitialized)
        {
            return;
        }

        _footerDetectionControlInitialized = true;
        _deviceBlockerService.KeyReceived += FooterDetectionControl_KeyReceived;
        _rawInputService.KeyPressed += FooterDetectionControl_RawKeyPressed;
    }

    private void FooterDetectionControl_KeyReceived(object? sender, DeviceKeyEventArgs e)
    {
        if (_isDashboardDetecting)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_isDashboardDetecting && !_isDetectingDevice && !_isCapturingRuleKey)
            {
                FooterText.Text = StatusPill.Text;
            }
        }), DispatcherPriority.ContextIdle);
    }

    private void FooterDetectionControl_RawKeyPressed(object? sender, RawKeyEventArgs e)
    {
        if (_isDashboardDetecting)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_isDashboardDetecting && !_isDetectingDevice && !_isCapturingRuleKey)
            {
                FooterText.Text = StatusPill.Text;
            }
        }), DispatcherPriority.ContextIdle);
    }
}
