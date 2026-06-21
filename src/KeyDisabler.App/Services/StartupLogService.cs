using System.IO;
using System.Text;
using System.Windows;

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
            MessageBox.Show(
                $"Key Disabler failed to start.\n\nError: {exception.Message}\n\nA log was saved here:\n{LogPath}",
                "Key Disabler startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // UI error reporting is best effort only.
        }
    }
}
