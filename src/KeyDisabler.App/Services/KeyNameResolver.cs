namespace KeyDisabler.App.Services;

public static class KeyNameResolver
{
    public static string Resolve(ushort scanCode, bool isExtendedKey)
    {
        return (scanCode, isExtendedKey) switch
        {
            (0x39, false) => "Space",
            (0x1C, false) => "Enter",
            (0x1C, true) => "Numpad Enter",
            (0x0E, false) => "Backspace",
            (0x0F, false) => "Tab",
            (0x01, false) => "Escape",
            (0x1D, false) => "Left Ctrl",
            (0x1D, true) => "Right Ctrl",
            (0x38, false) => "Left Alt",
            (0x38, true) => "Right Alt",
            (0x2A, false) => "Left Shift",
            (0x36, false) => "Right Shift",
            (0x3A, false) => "Caps Lock",
            (0x53, true) => "Delete",
            (0x52, true) => "Insert",
            _ => isExtendedKey ? $"Scan {scanCode} Extended" : $"Scan {scanCode}"
        };
    }
}
