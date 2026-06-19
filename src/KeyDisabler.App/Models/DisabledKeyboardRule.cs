namespace KeyDisabler.App.Models;

public sealed class DisabledKeyboardRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
