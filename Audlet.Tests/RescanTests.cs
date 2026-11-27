using System.Runtime.ExceptionServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Audlet.Audio;
using Audlet.Data;
using Audlet.Library;
using Audlet.Models;
using Audlet.Player;
using Audlet.Views;
using Audlet.Tests.E2E;
using Audlet.ViewModels;

namespace Audlet.Tests;

// What a rescan leaves behind: the merged library, covers, the saved files and the first-run state.
public class RescanTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-rescan-" + Guid.NewGuid().ToString("N"));
    private string Data => Path.Combine(_dir, "data");
    private string Root => Path.Combine(_dir, "library");
    private readonly BassPlayer _player = new();

    public void Dispose()
    {
        _player.Dispose();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private static void Png(string path)
    {
        var bmp = new RenderTargetBitmap(64, 64, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen()) dc.DrawRectangle(Brushes.Teal, null, new System.Windows.Rect(0, 0, 64, 64));
        bmp.Render(dv);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    [Fact]
    public async Task ARescanMergesCoversSavesAndRaisesLibraryChanged()
    {
        AppSession.WriteTone(Path.Combine(Root, "Frank Herbert", "Dune", "01.wav"), 1, 330);
        Png(Path.Combine(Root, "Frank Herbert", "Dune", "cover.png"));
        AppSession.WriteTone(Path.Combine(Root, "Piranesi", "01.wav"), 1, 500);
        var store = new AppStore(Data) { DebounceMs = Timeout.Infinite };
        store.Load();
        store.Settings.RootFolders.Add(Root);
        // Known from an earlier scan, with a cover file that has since been deleted.
        var piranesiId = Path.Combine(Root, "Piranesi");
        store.Library.Books.Add(new Book { Id = piranesiId, Title = "Piranesi", PositionSec = 4, CoverPath = Path.Combine(_dir, "gone.jpg") });
        var covers = new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec());
        var vm = new MainViewModel(store, new PlaybackController(_player, store, UiTimer.Every), covers, new WpfShell());
        var changed = 0;
        vm.LibraryChanged += () => changed++;

        await vm.RescanAsync();

        Assert.Equal(1, changed);
        Assert.False(vm.IsScanning);
        Assert.False(vm.FirstRun);
        Assert.Equal(["Dune", "Piranesi"], store.Library.Books.Select(b => b.Title).OrderBy(t => t));
        var dune = store.Library.Books.Single(b => b.Title == "Dune");
        Assert.Equal(covers.PathFor(dune), dune.CoverPath);
        Assert.True(File.Exists(dune.CoverPath));
        var piranesi = store.Library.Books.Single(b => b.Id == piranesiId);
        Assert.Null(piranesi.CoverPath);
        Assert.Equal(4, piranesi.PositionSec);
        Assert.Single(piranesi.Files);

        var saved = new AppStore(Data);
        saved.Load();
        Assert.Equal(["Dune", "Piranesi"], saved.Library.Books.Select(b => b.Title).OrderBy(t => t));
        Assert.Equal([Root], saved.Settings.RootFolders);
    }

    [Fact]
    public async Task RemovingTheOnlyRootEmptiesTheLibraryAndShowsFirstRun()
    {
        AppSession.WriteTone(Path.Combine(Root, "Piranesi", "01.wav"), 1, 500);
        var store = new AppStore(Data) { DebounceMs = Timeout.Infinite };
        store.Load();
        store.Settings.RootFolders.Add(Root);
        var vm = new MainViewModel(store, new PlaybackController(_player, store, UiTimer.Every), new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec()), new WpfShell());
        await vm.RescanAsync();
        Assert.Single(store.Library.Books);

        store.Settings.RootFolders.Clear();
        await vm.RescanAsync();

        Assert.Empty(store.Library.Books);
        Assert.True(vm.FirstRun);
    }

    // Runs work on a thread of its own with a dispatcher, as the window's thread runs it: awaits come back to that thread.
    internal static void OnUiThread(Func<Task> work)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.InvokeAsync(async () =>
            {
                try { await work(); } catch (Exception ex) { failure = ex; }
                dispatcher.InvokeShutdown();
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) ExceptionDispatchInfo.Throw(failure);
    }

    [Fact]
    public void ARescanSavesTheFilesAsTheyWereWhileTheUiThreadKeepsChangingThem() => OnUiThread(async () =>
    {
        AppSession.WriteTone(Path.Combine(Root, "Piranesi", "01.wav"), 1, 500);
        var store = new AppStore(Data) { DebounceMs = Timeout.Infinite };
        store.Load();
        store.Settings.RootFolders.Add(Root);
        var days = Enumerable.Range(0, 100_000).Select(i => "day " + i).ToList();
        store.Stats.Days.AddRange(days.Select(d => new DayStats { Day = d }));
        var vm = new MainViewModel(store, new PlaybackController(_player, store, UiTimer.Every), new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec()), new WpfShell());
        // While the rescan runs, the UI thread keeps changing a list the save writes out, as it may change the queue,
        // bookmarks or listening days; each change is undone in the same step, so between steps the list is as it was.
        var stop = false;
        async Task Change()
        {
            var extra = new DayStats { Day = "extra" };
            while (!stop) { store.Stats.Days.Insert(0, extra); store.Stats.Days.RemoveAt(0); await Dispatcher.Yield(DispatcherPriority.Background); }
        }
        var changing = Change();

        await vm.RescanAsync();
        stop = true;
        await changing;

        Assert.False(vm.IsScanning);
        var saved = new AppStore(Data);
        saved.Load();
        Assert.Equal(days, saved.Stats.Days.Select(d => d.Day));
        Assert.Equal("Piranesi", saved.Library.Books.Single().Title);
    });

    [Fact]
    public async Task ARescanStartedWhileOneRunsDoesNothing()
    {
        AppSession.WriteTone(Path.Combine(Root, "Piranesi", "01.wav"), 1, 500);
        var store = new AppStore(Data) { DebounceMs = Timeout.Infinite };
        store.Load();
        store.Settings.RootFolders.Add(Root);
        var vm = new MainViewModel(store, new PlaybackController(_player, store, UiTimer.Every), new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec()), new WpfShell());
        var changed = 0;
        vm.LibraryChanged += () => changed++;

        await Task.WhenAll(vm.RescanAsync(), vm.RescanAsync());

        Assert.Equal(1, changed);
        Assert.Single(store.Library.Books);
    }
}
