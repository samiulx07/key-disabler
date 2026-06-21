using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

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
            var appIconPath = BuildAssetPath(AppIconIcoPath);
            if (File.Exists(appIconPath))
            {
                return new Icon(appIconPath, Forms.SystemInformation.SmallIconSize);
            }

            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon != null)
                {
                    return (Icon)icon.Clone();
                }
            }

            return (Icon)SystemIcons.Application.Clone();
        }
        catch
        {
            return (Icon)SystemIcons.Application.Clone();
        }
    }

    public static ImageSource? LoadWindowIcon()
    {
        try
        {
            var appIconPath = BuildAssetPath(AppIconIcoPath);
            if (File.Exists(appIconPath))
            {
                using var icon = new Icon(appIconPath);
                return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }

            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon != null)
                {
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
