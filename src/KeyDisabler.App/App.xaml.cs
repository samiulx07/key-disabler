using KeyDisabler.App.Services;

namespace KeyDisabler.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        if (e.Args.Any(arg => string.Equals(arg, "--clear-settings", StringComparison.OrdinalIgnoreCase)))
        {
            new SettingsService().Reset();
        }

        base.OnStartup(e);
    }
}
