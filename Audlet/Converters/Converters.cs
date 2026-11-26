using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Audlet.Library;
using Audlet.ViewModels;

namespace Audlet.Converters;

public sealed class TimeConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => TimeText.Format(value is double d ? d : 0);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class RemainingConverter : IMultiValueConverter
{
    public object Convert(object[] v, Type t, object p, CultureInfo c) =>
        v.Length == 2 && v[0] is double pos && v[1] is double total ? "-" + TimeText.Format(total - pos) : "";
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class PercentConverter : IMultiValueConverter
{
    public object Convert(object[] v, Type t, object p, CultureInfo c) =>
        v.Length == 2 && v[0] is double pos && v[1] is double total && total > 0 ? Math.Clamp(pos / total * 100, 0, 100) : 0.0;
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class PositiveToVisibility : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is double d && d > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class PercentText : IMultiValueConverter
{
    public object Convert(object[] v, Type t, object p, CultureInfo c) =>
        v.Length == 2 && v[0] is double pos && v[1] is double total && total > 0 ? Math.Clamp(Math.Round(pos / total * 100), 0, 100) + "%" : "";
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class BoolToVisibility : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object v, Type t, object p, CultureInfo c) => (v is true) ^ Invert ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class NullToVisibility : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object v, Type t, object p, CultureInfo c) => (v is not null) ^ Invert ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class InverseBool : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is not true;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => v is not true;
}

public sealed class PlayGlyph : IValueConverter
{
    // Segoe MDL2 Assets glyphs: pause and play. The button sets that font family.
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? "\uE769" : "\uE768";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

// Sleep glyph for the player bar: crescent alone, or crescent plus the remaining time while a timer runs.
public sealed class SleepGlyph : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) =>
        v is TimeSpan ts && ts > TimeSpan.Zero ? " " + ts.ToString(@"mm\:ss") : "";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class LockGlyph : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? "Unlock" : "Lock";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class LocalTime : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => v is DateTime d ? d.ToLocalTime().ToString("HH:mm") : "";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

// Fraction (0..1) of a measured width, for bars that fill their container.
public sealed class FractionOfWidth : IMultiValueConverter
{
    public object Convert(object[] v, Type t, object p, CultureInfo c) =>
        v.Length == 2 && v[0] is double f && v[1] is double w ? Math.Max(0, f * w) : 0.0;
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class FractionWidth : IValueConverter
{
    public object Convert(object v, Type t, object p, CultureInfo c) => (v is double f ? f : 0) * double.Parse((string)p, CultureInfo.InvariantCulture);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

// Bypasses the WPF image cache so a replaced cover shows at once; returns null for a missing path.
// Card-size covers stay decoded by path and write time, so a card created again (another section, the rebuild after a
// rescan, scrolling back) reuses its bitmap, while a replaced cover file has a new write time and is decoded afresh.
public sealed class CoverImage : IValueConverter
{
    private static readonly LruCache<string, (DateTime Written, BitmapImage? Image)> Decoded = new(120);   // about 50 MB at 320 px

    public bool Full { get; set; }
    public object? Convert(object v, Type t, object p, CultureInfo c)
    {
        if (v is not string path) return null;
        var file = new FileInfo(path);
        if (!file.Exists) return null;
        if (Full) return Decode(path, full: true);
        if (Decoded.TryGet(path, out var known) && known.Written == file.LastWriteTimeUtc) return known.Image;
        var image = Decode(path, full: false);
        Decoded.Add(path, (file.LastWriteTimeUtc, image));
        return image;
    }

    private static BitmapImage? Decode(string path, bool full)
    {
        // A damaged file must show as "no cover", never take the window down with it.
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            img.UriSource = new Uri(path);
            if (!full) img.DecodePixelWidth = 320;
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch (Exception) { return null; }
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
