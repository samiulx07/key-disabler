using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using KeyDisabler.App.Services;

namespace KeyDisabler.App;

public partial class App : System.Windows.Application
{
    private const string AppUserModelId = "Samslab.KeyDisabler";

    private SingleInstanceService? _singleInstanceService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        ApplyWindowsAppIdentity();

        try
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

            window.Show();

            if (startMinimized)
            {
                window.Dispatcher.BeginInvoke(new Action(() =>
                {
                    window.Hide();
                }), DispatcherPriority.ApplicationIdle);
            }
        }
        catch (Exception ex)
        {
            StartupLogService.ShowStartupError(ex);
            Shutdown(-1);
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        StartupLogService.ShowStartupError(e.Exception);
        e.Handled = true;
        System.Windows.Application.Current.Shutdown(-1);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            StartupLogService.Write(exception.ToString());
        }
        else
        {
            StartupLogService.Write($"Unhandled non-exception startup error: {e.ExceptionObject}");
        }
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _singleInstanceService?.Dispose();
        base.OnExit(e);
    }

    private static void ApplyWindowsAppIdentity()
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        }
        catch
        {
            // Non-fatal. The tray icon still works if Windows refuses the explicit identity.
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
