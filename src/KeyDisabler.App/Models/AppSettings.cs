namespace KeyDisabler.App.Models;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public List<KeyboardRule> Rules { get; set; } = new();
    public List<DisabledKeyboardRule> DisabledKeyboards { get; set; } = new();
}
