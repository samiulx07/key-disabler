using System.Windows;
using Microsoft.Win32;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public static class ThemeService
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValueName = "AppsUseLightTheme";

    public static void Initialize(ThemeMode initialTheme)
    {
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // Re-apply current theme setting (it will detect system change if mode is System)
                // Note: We need a way to know the current setting. For simplicity, we just trigger an event
                // and let the MainWindow re-apply. But actually, we can just pass the setting in ApplyTheme.
            }
        };

        ApplyTheme(initialTheme);
    }

    public static void ApplyTheme(ThemeMode themeMode)
    {
        bool useDarkTheme = false;

        if (themeMode == ThemeMode.Dark)
        {
            useDarkTheme = true;
        }
        else if (themeMode == ThemeMode.Light)
        {
            useDarkTheme = false;
        }
        else // System
        {
            useDarkTheme = IsSystemInDarkMode();
        }

        var themeName = useDarkTheme ? "DarkTheme.xaml" : "LightTheme.xaml";
        var uri = new Uri($"pack://application:,,,/KeyDisabler.App;component/Themes/{themeName}", UriKind.Absolute);

        try
        {
            var dict = new ResourceDictionary { Source = uri };
            
            // Remove existing theme dictionaries
            var appResources = Application.Current.Resources.MergedDictionaries;
            var oldTheme = appResources.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme.xaml"));
            
            if (oldTheme != null)
            {
                appResources.Remove(oldTheme);
            }
            
            appResources.Add(dict);
        }
        catch (Exception)
        {
            // Ignore error if dictionary not found
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
