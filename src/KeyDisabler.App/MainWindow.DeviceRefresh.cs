using System.Windows;
using System.Windows.Interop;

namespace KeyDisabler.App;

public partial class MainWindow
{
    private const int WmDeviceChange = 0x0219;
    private const int DbtDeviceArrival = 0x8000;
    private const int DbtDeviceRemoveComplete = 0x8004;
    private const int DbtDevNodesChanged = 0x0007;

    private bool _deviceRefreshHookAttached;
    private CancellationTokenSource? _deviceRefreshDebounce;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            LoadedEvent,
            new RoutedEventHandler(MainWindow_LoadedDeviceRefresh));
    }

    private static void MainWindow_LoadedDeviceRefresh(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            window.AttachDeviceRefreshHooks();
        }
    }

    private void AttachDeviceRefreshHooks()
    {
        if (_deviceRefreshHookAttached)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(DeviceChangeWndProc);
        _deviceRefreshHookAttached = true;

        ScheduleDeviceRefresh("Initial delayed device scan", 700);
        ScheduleDeviceRefresh("Second delayed device scan", 2500);
    }

    private IntPtr DeviceChangeWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmDeviceChange)
        {
            return IntPtr.Zero;
        }

        var eventCode = wParam.ToInt32();
        if (eventCode is DbtDeviceArrival or DbtDeviceRemoveComplete or DbtDevNodesChanged)
        {
            ScheduleDeviceRefresh("Windows device list changed", 900);
        }

        return IntPtr.Zero;
    }

    private void ScheduleDeviceRefresh(string reason, int delayMilliseconds)
    {
        _deviceRefreshDebounce?.Cancel();
        _deviceRefreshDebounce?.Dispose();

        var cancellation = new CancellationTokenSource();
        _deviceRefreshDebounce = cancellation;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMilliseconds, cancellation.Token);
                await Dispatcher.InvokeAsync(() =>
                {
                    RefreshDevices();
                    UpdateDeviceBlocker();
                    UpdateSelectedDeviceStatus();
                    FooterText.Text = reason;
                });
            }
            catch (OperationCanceledException)
            {
                // Debounced by a newer refresh request.
            }
        }, cancellation.Token);
    }
}
