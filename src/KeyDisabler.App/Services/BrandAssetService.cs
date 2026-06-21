using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KeyDisabler.App.Services;

public static class BrandAssetService
{
    private const string AppIconPngPath = "Assets/AppIcon.png";
    private const string AppIconIcoPath = "Assets/AppIcon.ico";
    private const string AboutLogoPath = "Assets/AboutLogo.png";

    public static Icon LoadTrayIconOrDefault()
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon != null)
                {
                    // The system tray strictly requires SmallIconSize (usually 16x16). 
                    // Since our AppIcon.ico is 256x256, it must be resized.
                    using var bitmap = icon.ToBitmap();
                    using var resized = new Bitmap(bitmap, System.Windows.Forms.SystemInformation.SmallIconSize);
                    return Icon.FromHandle(resized.GetHicon());
                }
            }
            return SystemIcons.Application;
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
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon != null)
                {
                    // Keep the high-resolution icon for the Window (Taskbar, Alt+Tab)
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static ImageSource? LoadAboutLogo()
    {
        try
        {
            var path = BuildAssetPath(AboutLogoPath);
            if (!File.Exists(path))
            {
                return null;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public static ImageSource? LoadAppIconImage()
    {
        try
        {
            var path = BuildAssetPath(AppIconPngPath);
            if (!File.Exists(path))
            {
                return null;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildAssetPath(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
