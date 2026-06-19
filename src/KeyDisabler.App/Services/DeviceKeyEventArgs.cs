namespace KeyDisabler.App.Services;

public sealed class DeviceKeyEventArgs : EventArgs
{
    public DeviceKeyEventArgs(string deviceId, string deviceHardwareId, string deviceName, ushort scanCode, bool isExtendedKey, string keyName, bool wasBlocked)
    {
        DeviceId = deviceId;
        DeviceHardwareId = deviceHardwareId;
        DeviceName = deviceName;
        ScanCode = scanCode;
        IsExtendedKey = isExtendedKey;
        KeyName = keyName;
        WasBlocked = wasBlocked;
    }

    public string DeviceId { get; }
    public string DeviceHardwareId { get; }
    public string DeviceName { get; }
    public ushort ScanCode { get; }
    public bool IsExtendedKey { get; }
    public string KeyName { get; }
    public bool WasBlocked { get; }
}
