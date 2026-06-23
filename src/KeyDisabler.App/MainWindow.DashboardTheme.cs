using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace KeyDisabler.App;

internal static class MainWindowDashboardThemeBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            Window.LoadedEvent,
            new RoutedEventHandler(OnMainWindowLoaded));
    }

    private static void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is MainWindow window)
        {
            window.Dispatcher.BeginInvoke(new Action(window.InitializeDashboardThemeFeatures), DispatcherPriority.ApplicationIdle);
        }
    }
}

public partial class MainWindow
{
    private readonly Stopwatch _dashboardUptime = Stopwatch.StartNew();
    private DispatcherTimer? _dashboardTimer;
    private bool _dashboardThemeInitialized;

    private void InitializeDashboardThemeFeatures()
    {
        if (_dashboardThemeInitialized)
        {
            return;
        }

        _dashboardThemeInitialized = true;

        _devices.CollectionChanged += DashboardCollections_CollectionChanged;
        _rules.CollectionChanged += DashboardCollections_CollectionChanged;
        _disabledKeyboards.CollectionChanged += DashboardCollections_CollectionChanged;
        _remapRules.CollectionChanged += DashboardCollections_CollectionChanged;

        ConfigureSingleThemeSettingsUi();
        UpdateApplicationVersionText();
        UpdateWindowsInfoText();
        UpdateDashboardFeatureStats();

        _dashboardTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _dashboardTimer.Tick += (_, _) =>
        {
            UpdateUptimeText();
            UpdateDriverStatusText();
        };
        _dashboardTimer.Start();
    }

    private void DashboardCollections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(UpdateDashboardFeatureStats), DispatcherPriority.ApplicationIdle);
    }

    private void ConfigureSingleThemeSettingsUi()
    {
        if (FindName("ThemeComboBox") is ComboBox themeComboBox)
        {
            themeComboBox.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateDashboardFeatureStats()
    {
        SetText("ConnectedDevicesCountText", _devices.Count.ToString());

        var activeBlockRules = _rules.Count(rule => rule.IsEnabled);
        var activeRemapRules = _remapRules.Count(rule => rule.IsEnabled);
        var disabledKeyboards = _disabledKeyboards.Count(rule => rule.IsEnabled);

        SetText("ActiveBlockRulesCountText", activeBlockRules.ToString());
        SetText("ActiveRemapRulesCountText", activeRemapRules.ToString());
        SetText("DisabledKeyboardsCountText", disabledKeyboards.ToString());

        var protectionSummary = $"{activeBlockRules} block rules, {activeRemapRules} remap rules, {disabledKeyboards} disabled keyboards";
        SetText("RuleCountText", protectionSummary);

        UpdateDriverStatusText();
        UpdateUptimeText();
    }

    private void UpdateDriverStatusText()
    {
        var loaded = _deviceBlockerService.IsAvailable;
        SetText("DriverStatusValueText", loaded ? "Loaded" : "Not loaded");
        SetText("DriverStatusDetailText", loaded ? "Driver is running" : "Install or restart required");
        SetText("DriverFooterStatusText", loaded ? "Driver Loaded" : "Driver Not Loaded");

        SetForeground("DriverStatusValueText", loaded ? "SuccessBrush" : "PrimaryBrush");
        SetForeground("DriverFooterStatusText", loaded ? "TextPrimary" : "PrimaryBrush");
    }

    private void UpdateUptimeText()
    {
        var uptime = _dashboardUptime.Elapsed;
        SetText("UptimeText", $"{(int)uptime.TotalHours:00}h {uptime.Minutes:00}m {uptime.Seconds:00}s");
    }

    private void UpdateWindowsInfoText()
    {
        try
        {
            var version = Environment.OSVersion.Version;
            SetText("WindowsVersionText", "Windows");
            SetText("WindowsBuildText", $"Build {version.Build}.{version.Revision}");
        }
        catch
        {
            SetText("WindowsVersionText", "Windows");
            SetText("WindowsBuildText", "Build unknown");
        }
    }

    private void UpdateApplicationVersionText()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var displayVersion = version is null
            ? "v1.0.0"
            : $"v{version.Major}.{version.Minor}.{version.Build}";

        SetText("AppVersionText", displayVersion);
        SetText("DevAppVersionText", displayVersion);
    }

    private void OpenRemapTab_Click(object sender, RoutedEventArgs e)
    {
        RemapTab.IsSelected = true;
    }

    private void SetText(string elementName, string value)
    {
        if (FindName(elementName) is TextBlock textBlock)
        {
            textBlock.Text = value;
        }
        else if (FindName(elementName) is TextBox textBox)
        {
            textBox.Text = value;
        }
    }

    private void SetForeground(string elementName, string resourceKey)
    {
        if (FindName(elementName) is TextBlock textBlock)
        {
            textBlock.SetResourceReference(ForegroundProperty, resourceKey);
        }
    }
}
