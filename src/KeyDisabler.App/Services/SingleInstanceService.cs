using System.Runtime.InteropServices;

namespace KeyDisabler.App.Services;

internal sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Global\KeyDisabler_SamiulHasan_Samslab_SingleInstance";
    private const string ShowMessageName = "KeyDisabler.ShowMainWindow.v1";
    private static readonly IntPtr HwndBroadcast = new(0xFFFF);

    private readonly Mutex _mutex;
    private bool _isDisposed;

    public SingleInstanceService()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        IsFirstInstance = createdNew;
    }

    public static int ShowMessageId { get; } = RegisterWindowMessage(ShowMessageName);

    public bool IsFirstInstance { get; }

    public static void SignalExistingInstance()
    {
        if (ShowMessageId != 0)
        {
            PostMessage(HwndBroadcast, ShowMessageId, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (IsFirstInstance)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex was not owned anymore.
            }
        }

        _mutex.Dispose();
        _isDisposed = true;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
