using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Audlet.Data;
using Audlet.Library;
using Audlet.Player;

namespace Audlet.ViewModels;

public enum Section { Library, Started, New, Finished, Queue, Bookmarks, Statistics, Settings, NowPlaying }

public sealed partial class MainViewModel : ObservableObject
{
    public AppStore Store { get; }
    public PlaybackController Controller { get; }
    public CoverCache Covers { get; }

    [ObservableProperty] private Section _section = Section.Library;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _scanStatus = "";
    [ObservableProperty] private string? _toast;
    [ObservableProperty] private bool _firstRun;
    [ObservableProperty] private UpdateInfo? _update;

    public event Action? LibraryChanged;
    public event Action<bool>? HotkeysToggled;

    public void SetHotkeys(bool on) { Store.Settings.GlobalHotkeys = on; HotkeysToggled?.Invoke(on); }

    public MainViewModel(AppStore store, PlaybackController controller, CoverCache covers)
    {
        Store = store; Controller = controller; Covers = covers;
        FirstRun = store.Settings.RootFolders.Count == 0 && store.Settings.LooseFiles.Count == 0;
        controller.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(PlaybackController.Toast) && controller.Toast is { } t) ShowToast(t); };
    }

    public async Task CheckForUpdatesAsync()
    {
        if (!Store.Settings.CheckForUpdates) return;
        await Task.Delay(3000);   // let the window settle first
        Update = await UpdateChecker.CheckAsync();
    }

    [RelayCommand] private void OpenUpdate()
    {
        if (Update is null) return;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Update.InstallerUrl ?? Update.PageUrl) { UseShellExecute = true });
    }
    [RelayCommand] private void DismissUpdate() => Update = null;

    [RelayCommand] public void ToggleNowPlaying() => Section = Section == Section.NowPlaying ? Section.Library : Section.NowPlaying;

    [RelayCommand]
    public async Task RescanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;
        var roots = Store.Settings.RootFolders.ToList();
        var progress = new Progress<string>(p => ScanStatus = Path.GetFileName(p));
        var loose = Store.Settings.LooseFiles.ToList();
        var scanned = await Task.Run(() => new LibraryScanner(MetadataReader.Read).Scan(roots, progress, loose));
        LibraryMerger.Merge(Store.Library.Books, scanned);
        SeriesInfo.Label(Store.Library.Books);
        await Task.Run(() =>
        {
            foreach (var b in Store.Library.Books.Where(b => !b.Missing))
            {
                // A cover path that no longer exists (the cache folder moved, or the file was cleaned up) is rebuilt.
                if (b.CoverPath is not null && !File.Exists(b.CoverPath)) b.CoverPath = null;
                b.CoverPath ??= Covers.Ensure(b);
            }
        });
        Store.SaveNow();
        IsScanning = false; ScanStatus = "";
        FirstRun = Store.Settings.RootFolders.Count == 0 && Store.Settings.LooseFiles.Count == 0;
        LibraryChanged?.Invoke();
    }

    // Dropped folders become root folders; dropped audio files become standalone books.
    public async Task AddDroppedAsync(IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                if (Store.Settings.RootFolders.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
                Store.Settings.RootFolders.Add(path); added++;
            }
            else if (File.Exists(path) && AudioExtensions.IsAudio(path))
            {
                if (Store.Settings.LooseFiles.Contains(path, StringComparer.OrdinalIgnoreCase)) continue;
                Store.Settings.LooseFiles.Add(path); added++;
            }
        }
        if (added == 0) { ShowToast("Nothing to add: drop folders or audio files"); return; }
        FirstRun = false;
        Store.SaveSoon();
        await RescanAsync();
        ShowToast(added == 1 ? "Added 1 item to the library" : "Added " + added + " items to the library");
    }

    public async void ShowToast(string text)
    {
        Toast = text;
        await Task.Delay(2500);
        if (Toast == text) Toast = null;
    }
}
