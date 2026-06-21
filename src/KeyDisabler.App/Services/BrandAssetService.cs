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
            // Prefer the PNG icon, convert it to an Icon for the tray
            var pngPath = BuildAssetPath(AppIconPngPath);
            if (File.Exists(pngPath))
            {
                using var bitmap = new Bitmap(pngPath);
                var hIcon = bitmap.GetHicon();
                return Icon.FromHandle(hIcon);
            }

            // Fall back to .ico
            var icoPath = BuildAssetPath(AppIconIcoPath);
            if (File.Exists(icoPath))
            {
                return new Icon(icoPath);
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
            // Prefer the PNG icon for window title bar
            var pngPath = BuildAssetPath(AppIconPngPath);
            if (File.Exists(pngPath))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(pngPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }

            // Fall back to .ico
            using var icon = LoadTrayIconOrDefault();
            return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(256, 256));
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
