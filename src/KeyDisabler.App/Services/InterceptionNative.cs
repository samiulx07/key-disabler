using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyDisabler.App.Services;

internal static class InterceptionNative
{
    public const int KeyboardMinDevice = 1;
    public const int KeyboardMaxDevice = 10;
    public const ushort FilterKeyAll = 0xFFFF;
    public const ushort KeyStateUp = 0x01;
    public const ushort KeyStateE0 = 0x02;
    public const ushort KeyStateE1 = 0x04;

    static InterceptionNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(InterceptionNative).Assembly, ResolveNativeLibrary);
    }

    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, "interception.dll", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(libraryName, "interception", StringComparison.OrdinalIgnoreCase))
        {
            return IntPtr.Zero;
        }

        foreach (var candidate in GetInterceptionDllCandidates())
        {
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private static IEnumerable<string> GetInterceptionDllCandidates()
    {
        var baseDirectory = AppContext.BaseDirectory;

        yield return Path.Combine(baseDirectory, "interception.dll");
        yield return Path.Combine(baseDirectory, "driver", "interception.dll");

        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (!string.IsNullOrWhiteSpace(processDirectory) &&
            !string.Equals(processDirectory, baseDirectory, StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(processDirectory, "interception.dll");
            yield return Path.Combine(processDirectory, "driver", "interception.dll");
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int InterceptionPredicate(int device);

    [StructLayout(LayoutKind.Sequential)]
    public struct InterceptionKeyStroke
    {
        public ushort Code;
        public ushort State;
        public uint Information;
    }

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr interception_create_context();

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void interception_destroy_context(IntPtr context);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void interception_set_filter(
        IntPtr context,
        InterceptionPredicate predicate,
        ushort filter);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_wait(IntPtr context);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_receive(
        IntPtr context,
        int device,
        ref InterceptionKeyStroke stroke,
        uint nstroke);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int interception_send(
        IntPtr context,
        int device,
        ref InterceptionKeyStroke stroke,
        uint nstroke);

    [DllImport("interception.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int interception_get_hardware_id(
        IntPtr context,
        int device,
        StringBuilder hardwareIdBuffer,
        uint bufferLength);
}