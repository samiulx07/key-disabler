using System.IO;
using System.Text.Json;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public string SettingsDirectory { get; }
    public string SettingsPath { get; }

    public SettingsService()
    {
        SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyDisabler");

        SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            StartupLogService.Write($"Settings save failed: {ex}");
        }
    }

    public void Reset()
    {
        Directory.CreateDirectory(SettingsDirectory);
        Save(new AppSettings
        {
            StartWithWindows = false,
            MinimizeToTray = true,
            Rules = new List<KeyboardRule>(),
            RemapRules = new List<KeyRemapRule>(),
            DisabledKeyboards = new List<DisabledKeyboardRule>()
        });
    }
}
