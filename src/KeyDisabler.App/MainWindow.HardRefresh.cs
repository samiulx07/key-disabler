using System.Windows;

namespace KeyDisabler.App;

public partial class MainWindow
{
    private void HardRefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        UpdateStatus("Scanning keyboards...");

        var devices = _deviceBlockerService.HardRefreshKeyboardDevices();

        RefreshDevices();
        UpdateDeviceBlocker();
        UpdateSelectedDeviceStatus();

        if (devices.Count > 0 || _devices.Count > 0)
        {
            UpdateStatus($"Keyboard scan completed: {_devices.Count} controllable keyboard(s) found.");
            return;
        }

        var detail = string.IsNullOrWhiteSpace(_deviceBlockerService.LastError)
            ? "No controllable keyboards found. Restart Windows after driver install, then press Refresh again."
            : _deviceBlockerService.LastError;

        UpdateStatus(detail);
    }
}
