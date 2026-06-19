namespace KeyDisabler.App.Services;

public sealed class RawKeyEventArgs : EventArgs
{
    public RawKeyEventArgs(string deviceId, ushort virtualKey, string keyName)
    {
        DeviceId = deviceId;
        VirtualKey = virtualKey;
        KeyName = keyName;
    }

    public string DeviceId { get; }
    public ushort VirtualKey { get; }
    public string KeyName { get; }
}
