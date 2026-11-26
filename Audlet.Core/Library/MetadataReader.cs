using ATL;
using Audlet.Models;

namespace Audlet.Library;

public static class MetadataReader
{
    public static FileMeta Read(string path)
    {
        try
        {
            var track = new Track(path);
            var duration = track.DurationMs > 0 ? track.DurationMs / 1000.0 : track.Duration;
            var chapters = track.Chapters?
                .Where(c => c.EndTime > c.StartTime)
                .Select(c => new Chapter(c.Title ?? "", 0, c.StartTime / 1000.0, c.EndTime / 1000.0))
                .ToList() ?? [];
            if (chapters.Count == 1) chapters = [];
            double? part = double.TryParse(track.SeriesPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sp) ? sp : null;
            return new FileMeta(duration, Nul(track.Title), Nul(track.Album), Nul(track.Artist) ?? Nul(track.AlbumArtist), chapters, Nul(track.SeriesTitle), part);
        }
        catch (Exception)
        {
            return new FileMeta(0, null, null, null, []);
        }
    }

    public static byte[]? ReadEmbeddedCover(string path)
    {
        try
        {
            var pic = new Track(path).EmbeddedPictures.FirstOrDefault();
            return pic?.PictureData is { Length: > 0 } data ? data : null;
        }
        catch (Exception) { return null; }
    }

    private static string? Nul(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
