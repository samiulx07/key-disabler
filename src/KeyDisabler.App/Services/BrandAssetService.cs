using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KeyDisabler.App.Services;

public static class BrandAssetService
{
    private const string AppIconBase64Path = "Assets/AppIcon.ico.b64";

    public static Icon LoadTrayIconOrDefault()
    {
        try
        {
            var iconBytes = LoadIconBytes();
            if (iconBytes is null)
            {
                return SystemIcons.Application;
            }

            using var stream = new MemoryStream(iconBytes);
            using var icon = new Icon(stream);
            return (Icon)icon.Clone();
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public static ImageSource? LoadWindowIcon()
    {
        try
        {
            using var icon = LoadTrayIconOrDefault();
            return Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(256, 256));
        }
        catch
        {
            return null;
        }
    }

    public static ImageSource? LoadAboutIcon()
    {
        return LoadWindowIcon();
    }

    private static byte[]? LoadIconBytes()
    {
        var path = Path.Combine(AppContext.BaseDirectory, AppIconBase64Path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return null;
        }

        var base64 = File.ReadAllText(path);
        return Convert.FromBase64String(base64);
    }
}
