using System.Runtime.InteropServices;
using System.Windows.Input;
using AppKeyboardDevice = KeyDisabler.App.Models.KeyboardDevice;

namespace KeyDisabler.App.Services;

public sealed class RawInputService
{
    private const uint RIM_TYPEKEYBOARD = 1;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIDI_DEVICENAME = 0x20000007;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const int WM_INPUT = 0x00FF;
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_SYSKEYDOWN = 0x0104;

    public event EventHandler<RawKeyEventArgs>? KeyPressed;

    public bool RegisterKeyboardInput(IntPtr windowHandle)
    {
        var device = new RAWINPUTDEVICE
        {
            usUsagePage = 0x01,
            usUsage = 0x06,
            dwFlags = RIDEV_INPUTSINK,
            hwndTarget = windowHandle
        };

        return RegisterRawInputDevices(
            new[] { device },
            1,
            (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
    }

    public IReadOnlyList<AppKeyboardDevice> GetKeyboardDevices()
    {
        var devices = new List<AppKeyboardDevice>();
        uint deviceCount = 0;
        var structSize = (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>();

        var result = GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, structSize);
        if (result == uint.MaxValue || deviceCount == 0)
        {
            return devices;
        }

        var bufferSize = (int)(structSize * deviceCount);
        var buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            result = GetRawInputDeviceList(buffer, ref deviceCount, structSize);
            if (result == uint.MaxValue)
            {
                return devices;
            }

            for (var i = 0; i < deviceCount; i++)
            {
                var itemPointer = IntPtr.Add(buffer, i * (int)structSize);
                var item = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(itemPointer);

                if (item.dwType != RIM_TYPEKEYBOARD)
                {
                    continue;
                }

                var devicePath = GetDevicePath(item.hDevice);
                if (string.IsNullOrWhiteSpace(devicePath))
                {
                    continue;
                }

                devices.Add(new AppKeyboardDevice
                {
                    Id = devicePath,
                    DevicePath = devicePath,
                    DisplayName = BuildDisplayName(devicePath),
                    DeviceType = "Keyboard"
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return devices
            .GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(device => device.DisplayName)
            .ToList();
    }

    public void ProcessMessage(IntPtr message, IntPtr lParam)
    {
        if (message.ToInt32() != WM_INPUT)
        {
            return;
        }

        uint size = 0;
        var headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
        var sizeResult = GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref size, headerSize);
        if (sizeResult == uint.MaxValue || size == 0)
        {
            return;
        }

        var buffer = Marshal.AllocHGlobal((int)size);

        try
        {
            var read = GetRawInputData(lParam, RID_INPUT, buffer, ref size, headerSize);
            if (read == uint.MaxValue || read != size)
            {
                return;
            }

            var raw = Marshal.PtrToStructure<RAWINPUT>(buffer);
            if (raw.header.dwType != RIM_TYPEKEYBOARD)
            {
                return;
            }

            if (raw.keyboard.Message != WM_KEYDOWN && raw.keyboard.Message != WM_SYSKEYDOWN)
            {
                return;
            }

            if (raw.keyboard.VKey == 0)
            {
                return;
            }

            var devicePath = GetDevicePath(raw.header.hDevice);
            if (string.IsNullOrWhiteSpace(devicePath))
            {
                return;
            }

            var key = KeyInterop.KeyFromVirtualKey(raw.keyboard.VKey);
            var keyName = key == Key.None ? $"VK_{raw.keyboard.VKey}" : key.ToString();

            KeyPressed?.Invoke(this, new RawKeyEventArgs(devicePath, raw.keyboard.VKey, keyName));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string GetDevicePath(IntPtr deviceHandle)
    {
        uint size = 0;
        var sizeResult = GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, IntPtr.Zero, ref size);
        if (sizeResult == uint.MaxValue || size == 0)
        {
            return string.Empty;
        }

        var buffer = Marshal.AllocHGlobal((int)size * 2);
        try
        {
            var result = GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, buffer, ref size);
            if (result == uint.MaxValue)
            {
                return string.Empty;
            }

            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string BuildDisplayName(string devicePath)
    {
        var upper = devicePath.ToUpperInvariant();

        if (upper.Contains("ACPI") || upper.Contains("PNP0303") || upper.Contains("PNP0320"))
        {
            return "Built-in / Laptop Keyboard";
        }

        if (upper.Contains("VID_"))
        {
            var vid = ExtractToken(upper, "VID_");
            var pid = ExtractToken(upper, "PID_");
            return string.IsNullOrWhiteSpace(pid)
                ? $"USB Keyboard {vid}".Trim()
                : $"USB Keyboard {vid} {pid}".Trim();
        }

        if (upper.Contains("BTH") || upper.Contains("BLUETOOTH"))
        {
            return "Bluetooth Keyboard";
        }

        return "Keyboard Device";
    }

    private static string ExtractToken(string value, string prefix)
    {
        var index = value.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return string.Empty;
        }

        var start = index + prefix.Length;
        var end = start;
        while (end < value.Length && char.IsLetterOrDigit(value[end]))
        {
            end++;
        }

        return value[start..end];
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(
        RAWINPUTDEVICE[] pRawInputDevices,
        uint uiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList(
        IntPtr pRawInputDeviceList,
        ref uint puiNumDevices,
        uint cbSize);

    [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(
        IntPtr hDevice,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(
        IntPtr hRawInput,
        uint uiCommand,
        IntPtr pData,
        ref uint pcbSize,
        uint cbSizeHeader);

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICELIST
    {
        public IntPtr hDevice;
        public uint dwType;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUT
    {
        public RAWINPUTHEADER header;
        public RAWKEYBOARD keyboard;
    }
}
