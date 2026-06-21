using System.IO;
using System.Text;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;

namespace KeyDisabler.App.Services;

public static class StartupLogService
{
    private static readonly object SyncRoot = new();

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyDisabler",
        "logs");

    public static string LogPath { get; } = Path.Combine(LogDirectory, "startup-errors.log");

    public static void Write(string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectory);
                File.AppendAllText(
                    LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }

    public static void ShowStartupError(Exception exception)
    {
        Write(exception.ToString());

        try
        {
            WpfMessageBox.Show(
                $"Key Disabler could not open.\n\nError: {exception.Message}\n\nLog file:\n{LogPath}",
                "Key Disabler startup error",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Error);
        }
        catch
        {
            // UI error reporting is best effort only.
        }
    }
}
