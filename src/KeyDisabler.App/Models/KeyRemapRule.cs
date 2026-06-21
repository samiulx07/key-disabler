namespace KeyDisabler.App.Models;

public sealed class KeyRemapRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceHardwareId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;

    public ushort FromScanCode { get; set; }
    public bool FromIsExtendedKey { get; set; }
    public string FromKeyName { get; set; } = string.Empty;

    public ushort ToScanCode { get; set; }
    public bool ToIsExtendedKey { get; set; }
    public string ToKeyName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string RuleType => "Remap";
    public string TargetText => ToKeyName;
}
