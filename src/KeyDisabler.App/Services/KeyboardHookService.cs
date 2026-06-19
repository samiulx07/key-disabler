using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private readonly LowLevelKeyboardProc _hookCallback;
    private readonly object _syncRoot = new();
    private HashSet<int> _blockedVirtualKeys = new();
    private IntPtr _hookId = IntPtr.Zero;
    private bool _isDisposed;

    public KeyboardHookService()
    {
        _hookCallback = HookCallback;
    }

    public bool IsRunning => _hookId != IntPtr.Zero;

    public void Start()
    {
        if (_hookId != IntPtr.Zero)
        {
            return;
        }

        _hookId = SetHook(_hookCallback);
    }

    public void UpdateRules(IEnumerable<KeyboardRule> rules)
    {
        var activeKeys = rules
            .Where(rule => rule.IsEnabled)
            .Select(rule => (int)rule.VirtualKey)
            .Where(key => key > 0)
            .ToHashSet();

        lock (_syncRoot)
        {
            _blockedVirtualKeys = activeKeys;
        }
    }

    public void Stop()
    {
        if (_hookId == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private static IntPtr SetHook(LowLevelKeyboardProc callback)
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;

        var moduleHandle = currentModule is null
            ? IntPtr.Zero
            : GetModuleHandle(currentModule.ModuleName);

        return SetWindowsHookEx(WH_KEYBOARD_LL, callback, moduleHandle, 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsKeyboardMessage(wParam))
        {
            var hookInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            lock (_syncRoot)
            {
                if (_blockedVirtualKeys.Contains(hookInfo.vkCode))
                {
                    return new IntPtr(1);
                }
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static bool IsKeyboardMessage(IntPtr wParam)
    {
        var message = wParam.ToInt32();
        return message is WM_KEYDOWN or WM_KEYUP or WM_SYSKEYDOWN or WM_SYSKEYUP;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Stop();
        _isDisposed = true;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
