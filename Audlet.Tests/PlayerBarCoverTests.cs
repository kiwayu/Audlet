using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Audlet.Audio;
using Audlet.Converters;
using Audlet.Data;
using Audlet.Library;
using Audlet.Models;
using Audlet.Player;
using Audlet.ViewModels;
using Audlet.Views;

namespace Audlet.Tests;

// The player bar shows the current book's cover through the binding MainWindow.xaml gives it.
public class PlayerBarCoverTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-barcover-" + Guid.NewGuid().ToString("N"));
    private readonly BassPlayer _player = new();

    public void Dispose()
    {
        _player.Dispose();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private sealed class PickingShell(string file) : IShell
    {
        public string? PickFile(string? title, string filter) => file;
        public string? Ask(string title, string prompt, string initial = "") => throw new NotSupportedException();
        public string? PickFolder(string title, string? initialDirectory = null) => throw new NotSupportedException();
        public string? PickSaveFile(string title, string filter, string fileName) => throw new NotSupportedException();
        public (CoverCandidate Pick, bool ApplyDescription)? FindCover(string title, string author) => throw new NotSupportedException();
        public bool Confirm(string text, string caption) => throw new NotSupportedException();
        public void ApplyTheme(string name) => throw new NotSupportedException();
        public void Shutdown() => throw new NotSupportedException();
    }

    private string Solid(string name, Color color)
    {
        var path = Path.Combine(_dir, name);
        var pixels = Enumerable.Repeat(new[] { color.B, color.G, color.R, (byte)255 }, 64 * 64).SelectMany(p => p).ToArray();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(64, 64, 96, 96, PixelFormats.Bgra32, null, pixels, 64 * 4)));
        Directory.CreateDirectory(_dir);
        using (var file = File.Create(path)) encoder.Save(file);
        return path;
    }

    private static Color Centre(ImageSource? source)
    {
        var image = new FormatConvertedBitmap((BitmapSource)source!, PixelFormats.Bgra32, null, 0);
        var pixel = new byte[4];
        image.CopyPixels(new Int32Rect(image.PixelWidth / 2, image.PixelHeight / 2, 1, 1), pixel, 4, 0);
        return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
    }

    [Fact]
    public void SettingTheCoverOfTheBookPlayingShowsTheNewCoverInThePlayerBar() => RescanTests.OnUiThread(async () =>
    {
        var store = new AppStore(Path.Combine(_dir, "data")) { DebounceMs = Timeout.Infinite };
        store.Load();
        var covers = new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec());
        var book = new Book { Id = Path.Combine(_dir, "Dune"), Title = "Dune" };
        covers.Replace(book, Solid("red.png", Colors.Red));
        book.CoverPath = covers.PathFor(book);
        store.Library.Books.Add(book);
        var main = new MainViewModel(store, new PlaybackController(_player, store, UiTimer.Every), covers, new PickingShell(Solid("blue.png", Colors.Blue)));
        main.Controller.LoadBook(book, play: false);
        var bar = new Image();
        bar.SetBinding(Image.SourceProperty, new Binding("Controller.CurrentBook.CoverPath") { Source = main, Converter = new CoverImage() });
        Assert.True(Centre(bar.Source).R > 200);

        new LibraryViewModel(main, Section.Library).SetCoverCommand.Execute(book);
        await Dispatcher.Yield(DispatcherPriority.Background);

        var shown = Centre(bar.Source);
        Assert.True(shown.B > 200 && shown.R < 60, $"the player bar shows {shown}");
    });
}
