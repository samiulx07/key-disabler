namespace KeyDisabler.App.Models;

public sealed class KeyboardDevice
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DevicePath { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Keyboard";

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? DevicePath : DisplayName;
    }
}
