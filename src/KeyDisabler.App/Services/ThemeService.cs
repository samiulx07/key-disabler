using System.Windows;
using KeyDisabler.App.Models;

namespace KeyDisabler.App.Services;

public static class ThemeService
{
    private const string SingleThemeName = "LightTheme.xaml";

    public static void Initialize(AppThemeMode initialTheme)
    {
        ApplyTheme(initialTheme);
    }

    public static void ApplyTheme(AppThemeMode themeMode)
    {
        var uri = new Uri($"pack://application:,,,/Themes/{SingleThemeName}", UriKind.Absolute);

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
        return true;
    }
}
