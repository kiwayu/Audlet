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
        // A damaged folder image falls through to the embedded one rather than leaving the book without a cover.
        foreach (var bytes in Candidates(book))
        {
            try
            {
                var decoder = BitmapDecoder.Create(new MemoryStream(bytes), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                Save(decoder.Frames[0], target);
                return target;
            }
            catch (Exception) { }
            // A JPEG with a damaged tail still decodes when scaled during decoding, so try that before giving up on it.
            try { Save(DecodeScaled(bytes), target); return target; }
            catch (Exception) { }
        }
        return null;
    }

    private static BitmapSource DecodeScaled(byte[] bytes)
    {
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = new MemoryStream(bytes);
        img.DecodePixelWidth = MaxSide;
        img.EndInit();
        img.Freeze();
        return img;
    }

    private IEnumerable<byte[]> Candidates(Book book)
    {
        if (Directory.Exists(book.Id) && AudioExtensions.PickCoverFile(book.Id) is { } file) yield return File.ReadAllBytes(file);
        if (book.Files.Count > 0 && _embedded(book.Files[0].Path) is { } embedded) yield return embedded;
    }

    public void Replace(Book book, string imagePath)
    {
        var decoder = BitmapDecoder.Create(new Uri(imagePath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        Save(decoder.Frames[0], PathFor(book));
    }

    public void Replace(Book book, byte[] imageBytes)
    {
        var decoder = BitmapDecoder.Create(new MemoryStream(imageBytes), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        Save(decoder.Frames[0], PathFor(book));
    }

    private void Save(BitmapSource frame, string target)
    {
        Directory.CreateDirectory(_folder);
        var scale = Math.Min(1.0, (double)MaxSide / Math.Max(frame.PixelWidth, frame.PixelHeight));
        BitmapSource src = scale < 1.0 ? new TransformedBitmap(frame, new ScaleTransform(scale, scale)) : frame;
        var enc = new JpegBitmapEncoder { QualityLevel = 85 };
        enc.Frames.Add(BitmapFrame.Create(src));
        // Write beside the target and move into place, so a crash mid-write never leaves a half file behind.
        var temp = target + ".part";
        try
        {
            using (var fs = File.Create(temp)) enc.Save(fs);
            File.Move(temp, target, overwrite: true);
        }
        catch (Exception) { File.Delete(temp); throw; }
    }
}
