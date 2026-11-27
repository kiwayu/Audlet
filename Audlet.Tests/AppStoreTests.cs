using System.Reflection;
using System.Windows.Threading;
using Audlet.Data;
using Audlet.Models;

namespace Audlet.Tests;

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
    public async Task ACopyStillQueuedWhenTheAppSavesOnExitDoesNotOverwriteThatSave()
    {
        var store = new AppStore(_dir);
        store.Load();
        store.Settings.WindowWidth = 1000;
        // Holding the store's lock keeps the rescan's write queued, as a busy thread pool would; the lock lets the
        // same thread in again, so the exit save below still runs first.
        var gate = typeof(AppStore).GetField("_gate", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(store)!;
        Task rescanSave;
        lock (gate)
        {
            rescanSave = store.SaveCopyAsync();
            store.Settings.WindowWidth = 1400;
            store.SaveNow();
        }
        await rescanSave;

        var again = new AppStore(_dir);
        again.Load();
        Assert.Equal(1400, again.Settings.WindowWidth);
    }

    [Fact]
    public void ADelayedSaveWritesTheFilesAsTheyWereWhileTheUiThreadKeepsChangingThem() => RescanTests.OnUiThread(async () =>
    {
        var store = new AppStore(_dir) { DebounceMs = 10 };
        store.Load();
        var days = Enumerable.Range(0, 100_000).Select(i => "day " + i).ToList();
        store.Stats.Days.AddRange(days.Select(d => new DayStats { Day = d }));
        store.SaveSoon();
        // Until the delayed save has written its files, the UI thread keeps changing a list it writes out; each change
        // is undone in the same step, so between steps the list is as it was when the save was asked for.
        var stats = Path.Combine(_dir, "stats.json");
        var extra = new DayStats { Day = "extra" };
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!File.Exists(stats) && DateTime.UtcNow < deadline)
        {
            store.Stats.Days.Insert(0, extra); store.Stats.Days.RemoveAt(0);
            await Dispatcher.Yield(DispatcherPriority.Background);
        }

        var again = new AppStore(_dir);
        again.Load();
        Assert.Equal(days, again.Stats.Days.Select(d => d.Day));
    });

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
