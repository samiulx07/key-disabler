using System.Windows;
using System.Windows.Interop;
using KeyDisabler.App.Services;

namespace KeyDisabler.App;

public partial class MainWindow
{
    private const int WmDeviceChange = 0x0219;
    private const int DbtDeviceArrival = 0x8000;
    private const int DbtDeviceRemoveComplete = 0x8004;
    private const int DbtDevNodesChanged = 0x0007;

    private bool _deviceRefreshHookAttached;
    private bool _keyLearningHookAttached;
    private CancellationTokenSource? _deviceRefreshDebounce;



    private void AttachDeviceRefreshHooks()
    {
        if (_deviceRefreshHookAttached)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            handle = new WindowInteropHelper(this).EnsureHandle();
        }

        if (handle != IntPtr.Zero)
        {
            var source = HwndSource.FromHwnd(handle);
            source?.AddHook(DeviceChangeWndProc);
            _deviceRefreshHookAttached = true;
        }

        ScheduleDeviceRefresh("Initial delayed device scan", 700);
        ScheduleDeviceRefresh("Second delayed device scan", 2500);
    }

    private void InstallKeyCatalogAndLearning()
    {
        foreach (var key in BuildStandardKeyboardCatalog())
        {
            AddKeyOptionIfMissing(key);
        }

        if (_keyLearningHookAttached)
        {
            return;
        }

        _deviceBlockerService.KeyReceived += AutoLearnKeyFromDeviceEvent;
        _keyLearningHookAttached = true;
    }

    private void AutoLearnKeyFromDeviceEvent(object? sender, DeviceKeyEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var name = string.IsNullOrWhiteSpace(e.KeyName)
                ? BuildScanCodeName(e.ScanCode, e.IsExtendedKey)
                : e.KeyName;

            AddKeyOptionIfMissing(new KeyOption(name, 0, e.ScanCode, e.IsExtendedKey));
        });
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

    private static IReadOnlyList<KeyOption> BuildStandardKeyboardCatalog()
    {
        return new List<KeyOption>
        {
            new("Escape", 0x1B, 0x01, false),
            new("1", 0x31, 0x02, false),
            new("2", 0x32, 0x03, false),
            new("3", 0x33, 0x04, false),
            new("4", 0x34, 0x05, false),
            new("5", 0x35, 0x06, false),
            new("6", 0x36, 0x07, false),
            new("7", 0x37, 0x08, false),
            new("8", 0x38, 0x09, false),
            new("9", 0x39, 0x0A, false),
            new("0", 0x30, 0x0B, false),
            new("Minus", 0xBD, 0x0C, false),
            new("Equal", 0xBB, 0x0D, false),
            new("Backspace", 0x08, 0x0E, false),
            new("Tab", 0x09, 0x0F, false),
            new("Q", 0x51, 0x10, false),
            new("W", 0x57, 0x11, false),
            new("E", 0x45, 0x12, false),
            new("R", 0x52, 0x13, false),
            new("T", 0x54, 0x14, false),
            new("Y", 0x59, 0x15, false),
            new("U", 0x55, 0x16, false),
            new("I", 0x49, 0x17, false),
            new("O", 0x4F, 0x18, false),
            new("P", 0x50, 0x19, false),
            new("Left Bracket", 0xDB, 0x1A, false),
            new("Right Bracket", 0xDD, 0x1B, false),
            new("Enter", 0x0D, 0x1C, false),
            new("Left Ctrl", 0xA2, 0x1D, false),
            new("A", 0x41, 0x1E, false),
            new("S", 0x53, 0x1F, false),
            new("D", 0x44, 0x20, false),
            new("F", 0x46, 0x21, false),
            new("G", 0x47, 0x22, false),
            new("H", 0x48, 0x23, false),
            new("J", 0x4A, 0x24, false),
            new("K", 0x4B, 0x25, false),
            new("L", 0x4C, 0x26, false),
            new("Semicolon", 0xBA, 0x27, false),
            new("Quote", 0xDE, 0x28, false),
            new("Backtick", 0xC0, 0x29, false),
            new("Left Shift", 0xA0, 0x2A, false),
            new("Backslash", 0xDC, 0x2B, false),
            new("Z", 0x5A, 0x2C, false),
            new("X", 0x58, 0x2D, false),
            new("C", 0x43, 0x2E, false),
            new("V", 0x56, 0x2F, false),
            new("B", 0x42, 0x30, false),
            new("N", 0x4E, 0x31, false),
            new("M", 0x4D, 0x32, false),
            new("Comma", 0xBC, 0x33, false),
            new("Period", 0xBE, 0x34, false),
            new("Slash", 0xBF, 0x35, false),
            new("Right Shift", 0xA1, 0x36, false),
            new("Numpad Multiply", 0x6A, 0x37, false),
            new("Left Alt", 0xA4, 0x38, false),
            new("Space", 0x20, 0x39, false),
            new("Caps Lock", 0x14, 0x3A, false),
            new("F1", 0x70, 0x3B, false),
            new("F2", 0x71, 0x3C, false),
            new("F3", 0x72, 0x3D, false),
            new("F4", 0x73, 0x3E, false),
            new("F5", 0x74, 0x3F, false),
            new("F6", 0x75, 0x40, false),
            new("F7", 0x76, 0x41, false),
            new("F8", 0x77, 0x42, false),
            new("F9", 0x78, 0x43, false),
            new("F10", 0x79, 0x44, false),
            new("Num Lock", 0x90, 0x45, false),
            new("Scroll Lock", 0x91, 0x46, false),
            new("Numpad 7", 0x67, 0x47, false),
            new("Numpad 8", 0x68, 0x48, false),
            new("Numpad 9", 0x69, 0x49, false),
            new("Numpad Minus", 0x6D, 0x4A, false),
            new("Numpad 4", 0x64, 0x4B, false),
            new("Numpad 5", 0x65, 0x4C, false),
            new("Numpad 6", 0x66, 0x4D, false),
            new("Numpad Plus", 0x6B, 0x4E, false),
            new("Numpad 1", 0x61, 0x4F, false),
            new("Numpad 2", 0x62, 0x50, false),
            new("Numpad 3", 0x63, 0x51, false),
            new("Numpad 0", 0x60, 0x52, false),
            new("Numpad Decimal", 0x6E, 0x53, false),
            new("F11", 0x7A, 0x57, false),
            new("F12", 0x7B, 0x58, false),
            new("Right Ctrl", 0xA3, 0x1D, true),
            new("Right Alt", 0xA5, 0x38, true),
            new("Numpad Enter", 0x0D, 0x1C, true),
            new("Insert", 0x2D, 0x52, true),
            new("Delete", 0x2E, 0x53, true),
            new("Home", 0x24, 0x47, true),
            new("End", 0x23, 0x4F, true),
            new("Page Up", 0x21, 0x49, true),
            new("Page Down", 0x22, 0x51, true),
            new("Arrow Up", 0x26, 0x48, true),
            new("Arrow Down", 0x28, 0x50, true),
            new("Arrow Left", 0x25, 0x4B, true),
            new("Arrow Right", 0x27, 0x4D, true)
        };
    }

    private static string BuildScanCodeName(ushort scanCode, bool isExtended)
    {
        return isExtended
            ? $"Scan {scanCode} (Extended)"
            : $"Scan {scanCode}";
    }
}
