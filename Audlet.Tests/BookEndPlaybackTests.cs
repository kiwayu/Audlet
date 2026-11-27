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
}
