using System.Windows;

namespace KeyDisabler.App;

public partial class MainWindow
{
    private void HardRefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatus("Scanning keyboards...");

        var interceptionDevices = _deviceBlockerService.HardRefreshKeyboardDevices();

        RefreshDevices();
        UpdateDeviceBlocker();
        UpdateSelectedDeviceStatus();

        if (interceptionDevices.Count > 0)
        {
            UpdateStatus($"Keyboard scan completed: {_devices.Count} controllable keyboard(s) found.");
            return;
        }

        // Interception found nothing — check if Raw Input fallback populated the list
        if (_devices.Count > 0)
        {
            UpdateStatus($"Detected {_devices.Count} keyboard(s) via Windows. Restart your PC after driver install to enable key blocking.");
            FooterText.Text = "Keyboards detected but driver not loaded yet. Restart Windows after installing the Interception driver, then press Refresh.";
            return;
        }

        var detail = string.IsNullOrWhiteSpace(_deviceBlockerService.LastError)
            ? "No keyboards found. Restart Windows after driver install, then press Refresh again."
            : _deviceBlockerService.LastError;

        UpdateStatus(detail);
    }
}
