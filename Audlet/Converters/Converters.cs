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
// Prefetch decodes the covers of cards about to be created on worker threads, so a card rarely waits for its cover.
public sealed class CoverImage : IValueConverter
{
    // Used on the UI thread only; the decoding behind each task runs on a worker thread.
    private static readonly LruCache<string, Task<(DateTime Written, BitmapImage? Image)>> Decoded = new(120);   // about 50 MB at 320 px; more in a large window

    public bool Full { get; set; }
    public object? Convert(object v, Type t, object p, CultureInfo c)
    {
        if (v is not string path) return null;
        var file = new FileInfo(path);
        if (!file.Exists) return null;
        if (Full) return Decode(path, full: true);
        if (Decoded.TryGet(path, out var known) && known.Result is { Image: { } cached } && known.Result.Written == file.LastWriteTimeUtc) return cached;
        // A cover that could not be decoded (another program had it open, say) is tried again the next time it shows.
        var image = Decode(path, full: false);
        if (image is not null) Decoded.Add(path, Task.FromResult((file.LastWriteTimeUtc, image)));
        return image;
    }

    public static void Prefetch(IEnumerable<string?> paths)
    {
        var wanted = paths.OfType<string>().ToList();
        // A large window asks for more covers than the cache held; it must keep them all, or each layout pass would
        // drop covers it had just queued and queue them again. Marking every wanted cover as used before adding any
        // makes adding drop only covers that are not wanted.
        Decoded.Capacity = Math.Max(Decoded.Capacity, wanted.Count);
        foreach (var path in wanted.Where(p => !Decoded.TryGet(p, out _)).ToList())
            Decoded.Add(path, Task.Run(() => (File.GetLastWriteTimeUtc(path), Decode(path, full: false))));
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
