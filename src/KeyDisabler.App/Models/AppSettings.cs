namespace KeyDisabler.App.Models;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public List<KeyboardRule> Rules { get; set; } = new();
}
