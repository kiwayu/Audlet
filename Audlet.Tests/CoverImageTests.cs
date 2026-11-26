using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Audlet.Converters;

namespace Audlet.Tests;

public class CoverImageTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("audlet-cover-").FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private string Jpeg(string name)
    {
        var path = Path.Combine(_dir, name);
        var image = BitmapSource.Create(64, 64, 96, 96, PixelFormats.Bgr32, null, new byte[64 * 64 * 4], 64 * 4);
        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var file = File.Create(path)) encoder.Save(file);
        return path;
    }

    private static object? Show(string path) => new CoverImage().Convert(path, typeof(ImageSource), null!, CultureInfo.InvariantCulture);

    [Fact]
    public void ACoverShownAgainIsReusedUntilItsFileIsReplaced()
    {
        var path = Jpeg("cover.jpg");
        CoverImage.Prefetch([path]);
        var first = Assert.IsType<BitmapImage>(Show(path));
        Assert.Same(first, Show(path));
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(5));   // replacing a cover writes a new file
        Assert.NotSame(first, Show(path));
    }

    [Fact]
    public void ACoverThatCouldNotBeReadIsReadAgainWhenItShowsNextTime()
    {
        var path = Jpeg("locked.jpg");
        using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))   // another program holds it
        {
            CoverImage.Prefetch([path]);
            Assert.Null(Show(path));
        }
        Assert.IsType<BitmapImage>(Show(path));
    }

    [Fact]
    public void AMissingCoverShowsNothing() => Assert.Null(Show(Path.Combine(_dir, "none.jpg")));
}
