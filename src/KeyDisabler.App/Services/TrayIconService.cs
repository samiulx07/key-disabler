using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace KeyDisabler.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Window _window;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Icon _trayIcon;
    private bool _isDisposed;

    public TrayIconService(Window window)
    {
        _window = window;
        _trayIcon = BrandAssetService.LoadTrayIconOrDefault();

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowWindow());
        menu.Items.Add("Hide", null, (_, _) => _window.Hide());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Key Disabler",
            Icon = _trayIcon,
            ContextMenuStrip = menu,
            BalloonTipIcon = Forms.ToolTipIcon.None
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();
        RefreshNotificationIcon();
    }

    public void ShowBalloon(string title, string message)
    {
        RefreshNotificationIcon();
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.None;
        _notifyIcon.ShowBalloonTip(2500);
    }

    private void RefreshNotificationIcon()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_notifyIcon.Icon != _trayIcon)
        {
            _notifyIcon.Icon = _trayIcon;
        }

        if (!_notifyIcon.Visible)
        {
            _notifyIcon.Visible = true;
        }
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
        _notifyIcon.Dispose();
        _trayIcon.Dispose();
        _isDisposed = true;
    }
}
