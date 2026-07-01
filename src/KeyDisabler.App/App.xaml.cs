using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using KeyDisabler.App.Services;
using Velopack;

namespace KeyDisabler.App;

public partial class App : System.Windows.Application
{
    private const string AppUserModelId = "Samslab.KeyDisabler";

    private SingleInstanceService? _singleInstanceService;
    private UpdateService? _updateService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // ── Velopack auto-update bootstrap ──────────────────────────
        // This must run before anything else so Velopack can handle
        // --velopack bootstrap args (first-run, updated, etc.)
        try
        {
            VelopackApp.Build().Run();
        }
        catch (Exception ex)
        {
            // Velopack may fail in debug/dev builds — non-fatal
            Debug.WriteLine($"[Velopack] Bootstrap error: {ex.Message}");
        }

        ApplyWindowsAppIdentity();

        try
        {
            if (e.Args.Any(arg => string.Equals(arg, "--clear-settings", StringComparison.OrdinalIgnoreCase)))
            {
                new SettingsService().Reset();
            }

            var startMinimized = e.Args.Any(arg => 
                string.Equals(arg, "--minimized", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-minimized", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "/minimized", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--min", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-min", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "/min", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-m", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "/m", StringComparison.OrdinalIgnoreCase));

            _singleInstanceService = new SingleInstanceService();
            if (!_singleInstanceService.IsFirstInstance)
            {
                if (!startMinimized)
                {
                    SingleInstanceService.SignalExistingInstance();
                }
                _singleInstanceService.Dispose();
                Environment.Exit(0);
                return;
            }

            base.OnStartup(e);

            // ── Start background update check ──────────────────────
            _updateService = new UpdateService();
            CheckForUpdatesAsync();

            var window = new MainWindow();
            MainWindow = window;

            if (startMinimized)
            {
                window.EnsureInitialized();
            }
            else
            {
                window.Show();
            }
        }
        catch (Exception ex)
        {
            StartupLogService.ShowStartupError(ex);
            Shutdown(-1);
        }
    }

    /// <summary>
    /// Returns the shared UpdateService instance for use by the UI layer.
    /// </summary>
    public UpdateService? UpdateService => _updateService;

    /// <summary>
    /// Background update check. Stores the result so the UI can pick it up.
    /// </summary>
    private async void CheckForUpdatesAsync()
    {
        if (_updateService is null) return;

        try
        {
            var updateInfo = await _updateService.CheckForUpdatesAsync();
            if (updateInfo is not null)
            {
                Debug.WriteLine($"[Velopack] Update available: {updateInfo.TargetFullRelease.Version}");

                // Show the update notification on the UI thread
                MainWindow?.Dispatcher.Invoke(() =>
                {
                    if (MainWindow is MainWindow window)
                    {
                        window.OnUpdateAvailable(updateInfo);
                    }
                });
            }
            else
            {
                Debug.WriteLine("[Velopack] No updates available.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Velopack] Update check error: {ex.Message}");
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
