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
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _singleInstanceService?.Dispose();
        base.OnExit(e);
    }
}
