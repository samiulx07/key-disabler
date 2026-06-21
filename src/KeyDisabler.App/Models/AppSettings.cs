namespace KeyDisabler.App.Models;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public AppThemeMode Theme { get; set; } = AppThemeMode.System;
    public List<KeyboardRule> Rules { get; set; } = new();
    public List<DisabledKeyboardRule> DisabledKeyboards { get; set; } = new();
}
