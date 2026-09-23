using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Audlet.Models;

namespace Audlet.Library;

public sealed class CoverCache
{
    public const int MaxSide = 600;
    private readonly string _folder;
    private readonly Func<string, byte[]?> _embedded;

    public CoverCache(string folder, Func<string, byte[]?>? embedded = null)
    {
        _folder = folder;
        _embedded = embedded ?? MetadataReader.ReadEmbeddedCover;
    }

    public string PathFor(Book book) =>
        Path.Combine(_folder, Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(book.Id.ToLowerInvariant()))) + ".jpg");

    public string? Ensure(Book book)
    {
        var target = PathFor(book);
        if (File.Exists(target)) return target;
        byte[]? bytes = null;
        if (Directory.Exists(book.Id) && AudioExtensions.PickCoverFile(book.Id) is { } file)
            bytes = File.ReadAllBytes(file);
        bytes ??= book.Files.Count > 0 ? _embedded(book.Files[0].Path) : null;
        if (bytes is null) return null;
        try
        {
            var decoder = BitmapDecoder.Create(new MemoryStream(bytes), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            Save(decoder.Frames[0], target);
            return target;
        }
        catch (Exception) { return null; }
    }

    public void Replace(Book book, string imagePath)
    {
        var decoder = BitmapDecoder.Create(new Uri(imagePath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        Save(decoder.Frames[0], PathFor(book));
    }

    private void Save(BitmapFrame frame, string target)
    {
        Directory.CreateDirectory(_folder);
        var scale = Math.Min(1.0, (double)MaxSide / Math.Max(frame.PixelWidth, frame.PixelHeight));
        BitmapSource src = scale < 1.0 ? new TransformedBitmap(frame, new ScaleTransform(scale, scale)) : frame;
        var enc = new JpegBitmapEncoder { QualityLevel = 85 };
        enc.Frames.Add(BitmapFrame.Create(src));
        using var fs = File.Create(target);
        enc.Save(fs);
    }
}
