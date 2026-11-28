using Audlet.Audio;
using Audlet.Data;
using Audlet.Models;
using Audlet.Player;
using Audlet.Tests.E2E;
using Audlet.Views;

namespace Audlet.Tests;

// The end of a book as the controller meets it, with the audio engine open on short tones.
public class BookEndPlaybackTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-bookend-" + Guid.NewGuid().ToString("N"));
    private readonly BassPlayer _player = new();

    public void Dispose()
    {
        _player.Dispose();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private Book Tone(string title)
    {
        var path = Path.Combine(_dir, "library", title, "01.wav");
        AppSession.WriteTone(path, 2, 440);
        return new Book { Id = Path.GetDirectoryName(path)!, Title = title, State = BookState.Started, Files = [new AudioFile(path, 2, "01")] };
    }

    [Fact]
    public void AFinishedBookRestsAtItsStartWhenTheNextBookLoadsOnItsOwn()
    {
        var store = new AppStore(Path.Combine(_dir, "data")) { DebounceMs = Timeout.Infinite };
        store.Load();
        var finished = Tone("One");
        var next = Tone("Two");
        store.Library.Books.AddRange([finished, next]);
        store.Settings.Queue = [next.Id];
        store.Settings.EndOfBookAction = EndOfBookAction.NextBook;
        var controller = new PlaybackController(_player, store, UiTimer.Every);
        controller.LoadBook(finished, play: false);
        controller.SeekFile(0, 1.5);

        controller.NextChapter();   // from the last chapter: the book ends

        Assert.Same(next, controller.CurrentBook);
        Assert.Equal((BookState.Finished, 0, 0.0), (finished.State, finished.FileIndex, finished.PositionSec));
    }

    // With Stop, the default, the finished book stays open; quitting, opening another book or Play must not start from its end.
    [Fact]
    public void AFinishedBookThatStaysOpenIsBackAtItsStart()
    {
        var store = new AppStore(Path.Combine(_dir, "data")) { DebounceMs = Timeout.Infinite };
        store.Load();
        var book = Tone("One");
        store.Library.Books.Add(book);
        var player = new BassPlayer();   // the controller disposes it on quit
        var controller = new PlaybackController(player, store, UiTimer.Every);
        controller.LoadBook(book, play: false);
        controller.SeekFile(0, 1.5);

        controller.NextChapter();

        Assert.Equal(0, controller.PositionSec);
        controller.Shutdown();
        var saved = new AppStore(Path.Combine(_dir, "data"));
        saved.Load();
        var b = saved.Library.Books.Single();
        Assert.Equal((BookState.Finished, 0, 0.0), (b.State, b.FileIndex, b.PositionSec));
    }

    // As at start: the last book loads paused, so nothing has played yet when next chapter ends it.
    [Fact]
    public void ABookEndedBeforeItPlayedSavesNoListeningTime()
    {
        var store = new AppStore(Path.Combine(_dir, "data")) { DebounceMs = Timeout.Infinite };
        store.Load();
        var book = Tone("One");
        store.Library.Books.Add(book);
        var controller = new PlaybackController(_player, store, UiTimer.Every);
        controller.LoadBook(book, play: false);

        controller.NextChapter();

        var saved = new AppStore(Path.Combine(_dir, "data"));
        saved.Load();
        Assert.Equal(0, saved.Library.Books.Single().ListenedSec);
        Assert.Empty(saved.Stats.Days);
        Assert.Empty(saved.Stats.Months);
    }

    [Fact]
    public void ABookEndedWhilePausedSavesNoListeningTimeForThePause()
    {
        var store = new AppStore(Path.Combine(_dir, "data")) { DebounceMs = Timeout.Infinite };
        store.Load();
        var book = Tone("One");
        store.Library.Books.Add(book);
        var controller = new PlaybackController(_player, store, UiTimer.Every);
        _player.Volume = 0;
        controller.LoadBook(book, play: true);
        controller.Pause();
        Thread.Sleep(1500);

        controller.NextChapter();

        var saved = new AppStore(Path.Combine(_dir, "data"));
        saved.Load();
        Assert.Equal(0, saved.Library.Books.Single().ListenedSec);
        Assert.Empty(saved.Stats.Days);
    }
}
