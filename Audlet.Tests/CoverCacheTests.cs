using System.Windows.Media;
using System.Windows.Media.Imaging;
using Audlet.Library;
using Audlet.Models;

namespace Audlet.Tests;

public class CoverCacheTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-cover-" + Guid.NewGuid().ToString("N"));
    public CoverCacheTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);

    private static string MakePng(string folder, string name, int size)
    {
        Directory.CreateDirectory(folder);
        var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) dc.DrawRectangle(Brushes.Orange, null, new System.Windows.Rect(0, 0, size, size));
        bmp.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        var path = Path.Combine(folder, name);
        using var fs = File.Create(path);
        enc.Save(fs);
        return path;
    }

    [Fact]
    public void FolderImageIsResizedAndCached()
    {
        var bookDir = Path.Combine(_dir, "book");
        MakePng(bookDir, "cover.png", 1200);
        File.WriteAllText(Path.Combine(bookDir, "01.mp3"), "");
        var cache = new CoverCache(Path.Combine(_dir, "covers"), embedded: _ => null);
        var book = new Book { Id = bookDir, Files = [new AudioFile(Path.Combine(bookDir, "01.mp3"), 1, "x")] };

        var path = cache.Ensure(book);

        Assert.NotNull(path);
        Assert.EndsWith(".jpg", path);
        var frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
        Assert.Equal(600, frame.PixelWidth);
    }

    [Fact]
    public void DamagedCoverFileShowsAsNoCover()
    {
        // A JPEG header followed by rubbish, like a file cut off mid-write.
        var path = Path.Combine(_dir, "broken.jpg");
        File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[4000]]);
        var converter = new Audlet.Converters.CoverImage();
        Assert.Null(converter.Convert(path, typeof(object), null!, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void DamagedFolderCoverFallsBackToEmbedded()
    {
        var bookDir = Path.Combine(_dir, "damaged");
        Directory.CreateDirectory(bookDir);
        File.WriteAllBytes(Path.Combine(bookDir, "cover.jpg"), [0xFF, 0xD8, 0xFF, 0xE0, .. new byte[4000]]);
        File.WriteAllText(Path.Combine(bookDir, "01.mp3"), "");
        var good = File.ReadAllBytes(MakePng(_dir, "embedded.png", 200));
        var cache = new CoverCache(Path.Combine(_dir, "covers"), embedded: _ => good);
        var book = new Book { Id = bookDir, Files = [new AudioFile(Path.Combine(bookDir, "01.mp3"), 1, "x")] };

        var path = cache.Ensure(book);

        Assert.NotNull(path);
        Assert.Empty(Directory.GetFiles(Path.Combine(_dir, "covers"), "*.part"));
    }

    [Fact]
    public void NoImageGivesNull()
    {
        var bookDir = Path.Combine(_dir, "plain");
        Directory.CreateDirectory(bookDir);
        File.WriteAllText(Path.Combine(bookDir, "01.mp3"), "");
        var cache = new CoverCache(Path.Combine(_dir, "covers"), embedded: _ => null);
        var book = new Book { Id = bookDir, Files = [new AudioFile(Path.Combine(bookDir, "01.mp3"), 1, "x")] };
        Assert.Null(cache.Ensure(book));
    }
}
