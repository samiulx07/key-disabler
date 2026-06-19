namespace KeyDisabler.App.Services;

public sealed class DeviceKeyEventArgs : EventArgs
{
    public DeviceKeyEventArgs(string deviceId, string deviceName, ushort scanCode, bool isExtendedKey, string keyName, bool wasBlocked)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        ScanCode = scanCode;
        IsExtendedKey = isExtendedKey;
        KeyName = keyName;
        WasBlocked = wasBlocked;
    }

    public string DeviceId { get; }
    public string DeviceName { get; }
    public ushort ScanCode { get; }
    public bool IsExtendedKey { get; }
    public string KeyName { get; }
    public bool WasBlocked { get; }
}
