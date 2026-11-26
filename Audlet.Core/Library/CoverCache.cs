using System.Security.Cryptography;
using System.Text;
using Audlet.Models;

namespace Audlet.Library;

public sealed class CoverCache
{
    public const int MaxSide = 600;
    private readonly string _folder;
    private readonly ICoverCodec _codec;
    private readonly Func<string, byte[]?> _embedded;

    public CoverCache(string folder, ICoverCodec codec, Func<string, byte[]?>? embedded = null)
    {
        _folder = folder;
        _codec = codec;
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
                Save(_codec.ToJpeg(bytes, MaxSide), target);
                return target;
            }
            catch (Exception) { }
            // A JPEG with a damaged tail still decodes when scaled during decoding, so try that before giving up on it.
            try { Save(_codec.ToJpegScaled(bytes, MaxSide), target); return target; }
            catch (Exception) { }
        }
        return null;
    }

    private IEnumerable<byte[]> Candidates(Book book)
    {
        if (Directory.Exists(book.Id) && AudioExtensions.PickCoverFile(book.Id) is { } file) yield return File.ReadAllBytes(file);
        if (book.Files.Count > 0 && _embedded(book.Files[0].Path) is { } embedded) yield return embedded;
    }

    public void Replace(Book book, string imagePath) => Save(_codec.ToJpeg(imagePath, MaxSide), PathFor(book));

    public void Replace(Book book, byte[] imageBytes) => Save(_codec.ToJpeg(imageBytes, MaxSide), PathFor(book));

    private void Save(byte[] jpeg, string target)
    {
        Directory.CreateDirectory(_folder);
        // Write beside the target and move into place, so a crash mid-write never leaves a half file behind.
        var temp = target + ".part";
        try
        {
            File.WriteAllBytes(temp, jpeg);
            File.Move(temp, target, overwrite: true);
        }
        catch (Exception) { File.Delete(temp); throw; }
    }
}
