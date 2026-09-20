using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace WindowsAudiobookPlayer.Converters;

public sealed class TimeConverter : IValueConverter
{
    public static string Format(double seconds)
    {
        if (double.IsNaN(seconds) || seconds < 0) seconds = 0;
        var t = TimeSpan.FromSeconds(Math.Round(seconds));
        return t.TotalHours >= 1 ? string.Format("{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds) : string.Format("{0}:{1:00}", t.Minutes, t.Seconds);
    }
    public object Convert(object value, Type t, object p, CultureInfo c) => Format(value is double d ? d : 0);
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class RemainingConverter : IMultiValueConverter
{
    public object Convert(object[] v, Type t, object p, CultureInfo c) =>
        v.Length == 2 && v[0] is double pos && v[1] is double total ? "-" + TimeConverter.Format(total - pos) : "";
    public object[] ConvertBack(object v, Type[] t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class PercentConverter : IMultiValueConverter
{
    public object Convert(object[] v, Type t, object p, CultureInfo c) =>
        v.Length == 2 && v[0] is double pos && v[1] is double total && total > 0 ? Math.Clamp(pos / total * 100, 0, 100) : 0.0;
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
    public object Convert(object v, Type t, object p, CultureInfo c) => v is true ? "⏸" : "▶";
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
public sealed class CoverImage : IValueConverter
{
    public bool Full { get; set; }
    public object? Convert(object v, Type t, object p, CultureInfo c)
    {
        if (v is not string path || !File.Exists(path)) return null;
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        img.UriSource = new Uri(path);
        if (!Full) img.DecodePixelWidth = 320;
        img.EndInit();
        img.Freeze();
        return img;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
