using System.Text;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public sealed class DeviceKeyboardBlockerService : IDisposable
{
    private readonly object _syncRoot = new();
    private readonly InterceptionNative.InterceptionPredicate _keyboardPredicate;
    private readonly Dictionary<string, KeyboardRule> _rulesByDeviceAndKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, KeyRemapRule> _remapRulesByDeviceAndKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, KeyboardDevice> _deviceCache = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;
    private IntPtr _context = IntPtr.Zero;
    private bool _isDisposed;
    private bool _hasActiveRules;

    public DeviceKeyboardBlockerService()
    {
        _keyboardPredicate = device => device is >= InterceptionNative.KeyboardMinDevice and <= InterceptionNative.KeyboardMaxDevice ? 1 : 0;
    }

    public event EventHandler<DeviceKeyEventArgs>? KeyReceived;

    public bool IsAvailable { get; private set; }
    public string LastError { get; private set; } = string.Empty;
    public bool IsRunning => _workerTask is { IsCompleted: false };
    public bool HasActiveRules
    {
        get
        {
            lock (_syncRoot)
            {
                return _hasActiveRules;
            }
        }
    }

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
        if (!HasActiveRules)
        {
            StopWorkerAndDestroyContext();
            LastError = "No active device-level rules. Blocker is paused for safety.";
            return;
        }

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

    public void UpdateRules(IEnumerable<KeyboardRule> rules, IEnumerable<DisabledKeyboardRule> disabledKeyboardRules)
    {
        var activeRules = rules
            .Where(rule => rule.IsEnabled)
            .Where(rule => !string.IsNullOrWhiteSpace(rule.DeviceId))
            .Where(rule => rule.ScanCode > 0)
            .GroupBy(BuildRuleKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(BuildRuleKey, rule => rule, StringComparer.OrdinalIgnoreCase);

        // Full-keyboard blocking is intentionally paused. A bad saved full-keyboard rule can
        // lock the user out of all input devices, so only captured per-key rules are enforced.
        // The disabledKeyboardRules collection is still saved and shown in the UI, but it is
        // not applied until a timed test + auto-restore flow exists.
        _ = disabledKeyboardRules;

        lock (_syncRoot)
        {
            _rulesByDeviceAndKey.Clear();
            foreach (var pair in activeRules)
            {
                _rulesByDeviceAndKey[pair.Key] = pair.Value;
            }

            _hasActiveRules = _rulesByDeviceAndKey.Count > 0 || _remapRulesByDeviceAndKey.Count > 0;
        }

        if (!HasActiveRules)
        {
            StopWorkerAndDestroyContext();
        }
    }

    public void UpdateRemapRules(IEnumerable<KeyRemapRule> remapRules)
    {
        var activeRemapRules = remapRules
            .Where(rule => rule.IsEnabled)
            .Where(rule => rule.FromScanCode > 0)
            .Where(rule => rule.ToScanCode > 0)
            .ToList();

        lock (_syncRoot)
        {
            _remapRulesByDeviceAndKey.Clear();

            foreach (var rule in activeRemapRules)
            {
                if (!string.IsNullOrWhiteSpace(rule.DeviceId))
                {
                    _remapRulesByDeviceAndKey[BuildRuleKey(rule.DeviceId, rule.FromScanCode, rule.FromIsExtendedKey)] = rule;
                }

                var hardwareId = NormalizeHardwareId(rule.DeviceHardwareId);
                if (!string.IsNullOrWhiteSpace(hardwareId))
                {
                    _remapRulesByDeviceAndKey[BuildRuleKey(hardwareId, rule.FromScanCode, rule.FromIsExtendedKey)] = rule;
                }
            }

            _hasActiveRules = _rulesByDeviceAndKey.Count > 0 || _remapRulesByDeviceAndKey.Count > 0;
        }

        if (!HasActiveRules)
        {
            StopWorkerAndDestroyContext();
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
            var normalizedHardwareId = NormalizeHardwareId(hardwareId);
            var displayName = BuildDisplayName(device, hardwareId);
            RememberDevice(deviceId, hardwareId, displayName);

            var isExtended = (stroke.State & InterceptionNative.KeyStateE0) == InterceptionNative.KeyStateE0;
            var originalScanCode = stroke.Code;
            var keyName = KeyNameResolver.Resolve(originalScanCode, isExtended);
            var shouldStop = false;
            KeyRemapRule? remapRule = null;

            lock (_syncRoot)
            {
                var deviceRuleKey = BuildRuleKey(deviceId, originalScanCode, isExtended);
                var hardwareRuleKey = BuildRuleKey(normalizedHardwareId, originalScanCode, isExtended);

                if (_rulesByDeviceAndKey.ContainsKey(deviceRuleKey) || _rulesByDeviceAndKey.ContainsKey(hardwareRuleKey))
                {
                    shouldStop = true;
                }
                else if (_remapRulesByDeviceAndKey.TryGetValue(deviceRuleKey, out var deviceRemapRule))
                {
                    remapRule = deviceRemapRule;
                }
                else if (_remapRulesByDeviceAndKey.TryGetValue(hardwareRuleKey, out var hardwareRemapRule))
                {
                    remapRule = hardwareRemapRule;
                }
            }

            if (!shouldStop && remapRule is not null)
            {
                stroke.Code = remapRule.ToScanCode;
                stroke.State = remapRule.ToIsExtendedKey
                    ? (ushort)(stroke.State | InterceptionNative.KeyStateE0)
                    : (ushort)(stroke.State & ~InterceptionNative.KeyStateE0);
            }

            shouldForward = !shouldStop;

            KeyReceived?.Invoke(
                this,
                new DeviceKeyEventArgs(
                    deviceId,
                    hardwareId,
                    ResolveDeviceName(deviceId),
                    originalScanCode,
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

    private static string BuildRuleKey(string deviceIdOrHardwareId, ushort scanCode, bool isExtended)
    {
        return string.IsNullOrWhiteSpace(deviceIdOrHardwareId)
            ? string.Empty
            : $"{deviceIdOrHardwareId}|{scanCode}|{isExtended}";
    }

    private static string NormalizeHardwareId(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
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
