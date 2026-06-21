using KeyDisabler.App.Services;

namespace KeyDisabler.App;

public partial class App : System.Windows.Application
{
    private SingleInstanceService? _singleInstanceService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (e.Args.Any(arg => string.Equals(arg, "--clear-settings", StringComparison.OrdinalIgnoreCase)))
        {
            new SettingsService().Reset();
        }

        _singleInstanceService = new SingleInstanceService();
        if (!_singleInstanceService.IsFirstInstance)
        {
            SingleInstanceService.SignalExistingInstance();
            _singleInstanceService.Dispose();
            Environment.Exit(0);
            return;
        }

        base.OnStartup(e);

        var startMinimized = e.Args.Any(arg => string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase));

        var window = new MainWindow();
        MainWindow = window;

        if (startMinimized)
        {
            // Load the window (triggers Window_Loaded, which creates the tray icon and starts the blocker),
            // but keep it hidden so the app runs silently in the background.
            window.WindowState = System.Windows.WindowState.Minimized;
            window.ShowInTaskbar = false;
            window.Show();
            window.Hide();
            window.ShowInTaskbar = true;
            window.WindowState = System.Windows.WindowState.Normal;
        }
        else
        {
            window.Show();
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _singleInstanceService?.Dispose();
        base.OnExit(e);
    }
}
