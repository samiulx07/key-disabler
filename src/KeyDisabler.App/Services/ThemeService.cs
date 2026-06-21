using System.Windows;
using Microsoft.Win32;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public static class ThemeService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";

    public static void Initialize(AppThemeMode initialTheme)
    {
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                ApplyTheme(initialTheme);
            }
        };

        ApplyTheme(initialTheme);
    }

    public static void ApplyTheme(AppThemeMode themeMode)
    {
        bool useDarkTheme = false;

        if (themeMode == AppThemeMode.Dark)
        {
            useDarkTheme = true;
        }
        else if (themeMode == AppThemeMode.Light)
        {
            useDarkTheme = false;
        }
        else
        {
            useDarkTheme = IsSystemInDarkMode();
        }

        var themeName = useDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
        var uri = new Uri($"pack://application:,,,/Themes/{themeName}", UriKind.Absolute);

        try
        {
            var dict = new ResourceDictionary { Source = uri };
            var appResources = System.Windows.Application.Current.Resources.MergedDictionaries;
            var oldThemes = appResources
                .Where(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var oldTheme in oldThemes)
            {
                appResources.Remove(oldTheme);
            }

            appResources.Add(dict);
        }
        catch (Exception ex)
        {
            StartupLogService.Write($"Theme load failed: {ex}");
        }
    }

    public static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            var registryValueObject = key?.GetValue(RegistryValueName);

            if (registryValueObject == null)
            {
                return false;
            }

            int registryValue = (int)registryValueObject;
            return registryValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
