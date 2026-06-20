using System.Text;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public sealed class DeviceKeyboardBlockerService : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly InterceptionNative.InterceptionPredicate _keyboardPredicate;
    private readonly Dictionary<string, KeyboardRule> _rulesByDeviceAndKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, KeyboardDevice> _deviceCache = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;
    private IntPtr _context = IntPtr.Zero;
    private bool _isDisposed;

    public DeviceKeyboardBlockerService()
    {
        _keyboardPredicate = device => device is >= InterceptionNative.KeyboardMinDevice and <= InterceptionNative.KeyboardMaxDevice ? 1 : 0;
    }

    public event EventHandler<DeviceKeyEventArgs>? KeyReceived;

    public bool IsAvailable { get; private set; }
    public string LastError { get; private set; } = string.Empty;
    public bool IsRunning => _workerTask is { IsCompleted: false };

    public IReadOnlyList<KeyboardDevice> GetKeyboardDevices()
    {
        if (!EnsureContext())
        {
            return Array.Empty<KeyboardDevice>();
        }

        var devices = new List<KeyboardDevice>();

        for (var device = InterceptionNative.KeyboardMinDevice; device <= InterceptionNative.KeyboardMaxDevice; device++)
        {
            var id = BuildDeviceId(device);
            var hardwareId = GetHardwareId(device);
            if (string.IsNullOrWhiteSpace(hardwareId))
            {
                continue;
            }

            var displayName = BuildDisplayName(device, hardwareId);
            var keyboard = new KeyboardDevice
            {
                Id = id,
                DevicePath = hardwareId,
                DisplayName = displayName,
                DeviceType = "Interception Keyboard"
            };

            devices.Add(keyboard);
            _deviceCache[id] = keyboard;
        }

        LastError = devices.Count == 0
            ? "No Interception keyboard slots reported hardware IDs. Restart Windows after driver install, then press Refresh again."
            : string.Empty;

        return devices;
    }

    public IReadOnlyList<KeyboardDevice> HardRefreshKeyboardDevices()
    {
        StopWorkerAndDestroyContext();
        _deviceCache.Clear();
        IsAvailable = false;
        LastError = string.Empty;

        var devices = GetKeyboardDevices();
        Start();
        return devices;
    }

    public void Start()
    {
        if (!EnsureContext() || IsRunning)
        {
            return;
        }

        InterceptionNative.interception_set_filter(
            _context,
            _keyboardPredicate,
            InterceptionNative.FilterKeyAll);

        _cancellationTokenSource = new CancellationTokenSource();
        _workerTask = Task.Run(() => WorkerLoop(_cancellationTokenSource.Token));
    }

    public void UpdateRules(IEnumerable<KeyboardRule> rules, IEnumerable<DisabledKeyboardRule> inactiveFullKeyboardRules)
    {
        var activeRules = rules
            .Where(rule => rule.IsEnabled)
            .Where(rule => !string.IsNullOrWhiteSpace(rule.DeviceId))
            .Where(rule => rule.ScanCode > 0)
            .GroupBy(BuildRuleKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(BuildRuleKey, rule => rule, StringComparer.OrdinalIgnoreCase);

        lock (_syncRoot)
        {
            _rulesByDeviceAndKey.Clear();
            foreach (var pair in activeRules)
            {
                _rulesByDeviceAndKey[pair.Key] = pair.Value;
            }
        }
    }

    private void WorkerLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _context != IntPtr.Zero)
            {
                var device = InterceptionNative.interception_wait(_context);
                if (device < InterceptionNative.KeyboardMinDevice || device > InterceptionNative.KeyboardMaxDevice)
                {
                    continue;
                }

                ProcessDeviceEvent(device);
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LastError = ex.Message;
                IsAvailable = false;
            }
        }
    }

    private void ProcessDeviceEvent(int device)
    {
        var stroke = new InterceptionNative.InterceptionKeyStroke();
        var received = InterceptionNative.interception_receive(_context, device, ref stroke, 1);
        if (received <= 0)
        {
            return;
        }

        var shouldForward = true;

        try
        {
            var deviceId = BuildDeviceId(device);
            var hardwareId = GetHardwareId(device);
            var displayName = BuildDisplayName(device, hardwareId);
            RememberDevice(deviceId, hardwareId, displayName);

            var isExtended = (stroke.State & InterceptionNative.KeyStateE0) == InterceptionNative.KeyStateE0;
            var ruleKey = BuildRuleKey(deviceId, stroke.Code, isExtended);
            var keyName = KeyNameResolver.Resolve(stroke.Code, isExtended);
            var shouldStop = false;

            lock (_syncRoot)
            {
                shouldStop = _rulesByDeviceAndKey.ContainsKey(ruleKey);
            }

            shouldForward = !shouldStop;

            KeyReceived?.Invoke(
                this,
                new DeviceKeyEventArgs(
                    deviceId,
                    hardwareId,
                    ResolveDeviceName(deviceId),
                    stroke.Code,
                    isExtended,
                    keyName,
                    shouldStop));
        }
        finally
        {
            if (shouldForward)
            {
                InterceptionNative.interception_send(_context, device, ref stroke, 1);
            }
        }
    }

    private bool EnsureContext()
    {
        if (_context != IntPtr.Zero)
        {
            return true;
        }

        try
        {
            _context = InterceptionNative.interception_create_context();
            IsAvailable = _context != IntPtr.Zero;
            LastError = IsAvailable ? string.Empty : "Interception context could not be created.";
            return IsAvailable;
        }
        catch (DllNotFoundException)
        {
            IsAvailable = false;
            LastError = "interception.dll was not found. Install the driver package first.";
            return false;
        }
        catch (BadImageFormatException)
        {
            IsAvailable = false;
            LastError = "interception.dll architecture does not match this app build.";
            return false;
        }
        catch (EntryPointNotFoundException ex)
        {
            IsAvailable = false;
            LastError = $"Interception API mismatch: {ex.Message}";
            return false;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            LastError = ex.Message;
            return false;
        }
    }

    private string GetHardwareId(int device)
    {
        if (_context == IntPtr.Zero)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(512);
        var result = InterceptionNative.interception_get_hardware_id(_context, device, buffer, (uint)buffer.Capacity);
        return result > 0 ? buffer.ToString().TrimEnd('\0').Trim() : string.Empty;
    }

    private void RememberDevice(string deviceId, string hardwareId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(hardwareId))
        {
            return;
        }

        _deviceCache[deviceId] = new KeyboardDevice
        {
            Id = deviceId,
            DevicePath = hardwareId,
            DisplayName = displayName,
            DeviceType = "Interception Keyboard"
        };
    }

    private string ResolveDeviceName(string deviceId)
    {
        return _deviceCache.TryGetValue(deviceId, out var device)
            ? device.DisplayName
            : deviceId;
    }

    private void StopWorkerAndDestroyContext()
    {
        _cancellationTokenSource?.Cancel();

        if (_context != IntPtr.Zero)
        {
            InterceptionNative.interception_destroy_context(_context);
            _context = IntPtr.Zero;
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _workerTask = null;
    }

    private static string BuildDisplayName(int device, string hardwareId)
    {
        if (string.IsNullOrWhiteSpace(hardwareId))
        {
            return $"Keyboard {device}";
        }

        var upper = hardwareId.ToUpperInvariant();
        if (upper.Contains("ACPI") || upper.Contains("PNP0303") || upper.Contains("PNP0320"))
        {
            return $"Keyboard {device} - Built-in / Laptop Keyboard";
        }

        if (upper.Contains("VID_") || upper.Contains("HID\\"))
        {
            return $"Keyboard {device} - USB/HID Keyboard";
        }

        if (upper.Contains("BTH") || upper.Contains("BLUETOOTH"))
        {
            return $"Keyboard {device} - Bluetooth Keyboard";
        }

        return $"Keyboard {device} - {hardwareId}";
    }

    private static string BuildDeviceId(int device)
    {
        return $"interception:{device}";
    }

    private static string BuildRuleKey(KeyboardRule rule)
    {
        return BuildRuleKey(rule.DeviceId, rule.ScanCode, rule.IsExtendedKey);
    }

    private static string BuildRuleKey(string deviceId, ushort scanCode, bool isExtended)
    {
        return $"{deviceId}|{scanCode}|{isExtended}";
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        StopWorkerAndDestroyContext();
        _isDisposed = true;
    }
}
