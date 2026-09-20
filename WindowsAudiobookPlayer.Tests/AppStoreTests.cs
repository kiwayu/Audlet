using WindowsAudiobookPlayer.Data;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Tests;

public class AppStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-tests-" + Guid.NewGuid().ToString("N"));

    public AppStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void LoadOnEmptyFolderGivesDefaults()
    {
        var store = new AppStore(_dir);
        store.Load();
        Assert.Empty(store.Library.Books);
        Assert.Equal(10, store.Settings.SmallRewindSec);
        Assert.Empty(store.Stats.Months);
    }

    [Fact]
    public void SaveNowThenLoadRoundTrips()
    {
        var store = new AppStore(_dir);
        store.Load();
        store.Library.Books.Add(new Book { Id = @"C:\b\one", Title = "One" });
        store.Settings.BigRewindSec = 90;
        store.Stats.Months.Add(new MonthStats { Month = "2026-09", ListenedSec = 5 });
        store.SaveNow();

        var again = new AppStore(_dir);
        again.Load();
        Assert.Equal("One", again.Library.Books.Single().Title);
        Assert.Equal(90, again.Settings.BigRewindSec);
        Assert.Equal(5, again.Stats.Months.Single().ListenedSec);
    }

    [Fact]
    public void CorruptFileIsRenamedAndDefaultsUsed()
    {
        File.WriteAllText(Path.Combine(_dir, "library.json"), "{ not json");
        var store = new AppStore(_dir);
        store.Load();
        Assert.Empty(store.Library.Books);
        Assert.True(File.Exists(Path.Combine(_dir, "library.json.bad")));
    }

    [Fact]
    public async Task SaveSoonWritesAfterDebounce()
    {
        var store = new AppStore(_dir) { DebounceMs = 50 };
        store.Load();
        store.Settings.BigRewindSec = 120;
        store.SaveSoon();
        Assert.False(File.Exists(Path.Combine(_dir, "settings.json")));
        await Task.Delay(400);
        Assert.True(File.Exists(Path.Combine(_dir, "settings.json")));
    }
}
