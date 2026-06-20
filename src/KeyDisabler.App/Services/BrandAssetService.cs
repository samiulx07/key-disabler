using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KeyDisabler.App.Services;

public static class BrandAssetService
{
    private const string AppIconPath = "Assets/AppIcon.ico";
    private const string AboutLogoPath = "Assets/AboutLogo.png";

    public static Icon LoadTrayIconOrDefault()
    {
        try
        {
            var path = BuildAssetPath(AppIconPath);
            return File.Exists(path) ? new Icon(path) : SystemIcons.Application;
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

    private static string BuildAssetPath(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
