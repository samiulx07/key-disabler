using System.Windows;
using Forms = System.Windows.Forms;

namespace KeyDisabler.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _isDisposed;

    public TrayIconService(Window window)
    {
        _window = window;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowWindow());
        menu.Items.Add("Hide", null, (_, _) => _window.Hide());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Key Disabler",
            Icon = BrandAssetService.LoadTrayIconOrDefault(),
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
    }

    public void ShowBalloon(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(2500);
    }

    private void ShowWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private void ExitApplication()
    {
        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
        _isDisposed = true;
    }
}
