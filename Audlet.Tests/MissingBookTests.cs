using Audlet.Audio;
using Audlet.Data;
using Audlet.Library;
using Audlet.Models;
using Audlet.Player;
using Audlet.Tests.E2E;
using Audlet.ViewModels;
using Audlet.Views;

namespace Audlet.Tests;

// A book whose files are missing (its drive is unplugged) can still be opened, but nothing plays for it.
public class MissingBookTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-missing-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    [Fact]
    public void OpeningAMissingBookFromItsCardNeitherPlaysNorSavesTheBookBefore()
    {
        var store = new AppStore(Path.Combine(_dir, "data")) { DebounceMs = Timeout.Infinite };
        store.Load();
        var file = Path.Combine(_dir, "library", "One", "01.wav");
        AppSession.WriteTone(file, 2, 440);
        var before = new Book { Id = Path.GetDirectoryName(file)!, Title = "One", State = BookState.Started, Files = [new AudioFile(file, 2, "01")] };
        var gone = Path.Combine(_dir, "library", "Gone");
        var missing = new Book
        {
            Id = gone, Title = "Gone", State = BookState.Started, Missing = true, FileIndex = 1, PositionSec = 0.7,
            Files = [new AudioFile(Path.Combine(gone, "01.wav"), 2, "01"), new AudioFile(Path.Combine(gone, "02.wav"), 2, "02")],
        };
        store.Library.Books.AddRange([before, missing]);
        var player = new BassPlayer { Volume = 0 };   // the controller disposes it on quit
        var main = new MainViewModel(store, new PlaybackController(player, store, UiTimer.Every), new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec()), new WpfShell());
        main.Controller.LoadBook(before, play: false);
        main.Controller.SeekFile(0, 1.5);

        new LibraryViewModel(main, Section.Library).OpenCommand.Execute(missing);   // a click on its card
        main.Controller.Play();

        Assert.Same(missing, main.Controller.CurrentBook);
        Assert.False(main.Controller.IsPlaying);
        main.Controller.Shutdown();
        var saved = new AppStore(Path.Combine(_dir, "data"));
        saved.Load();
        Assert.Equal((1, 0.7), saved.Library.Books.Where(b => b.Title == "Gone").Select(b => (b.FileIndex, b.PositionSec)).Single());
        Assert.Equal(1.5, saved.Library.Books.Single(b => b.Title == "One").PositionSec, 2);
    }
}
