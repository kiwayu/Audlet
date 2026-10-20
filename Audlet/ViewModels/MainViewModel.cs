using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Audlet.Data;
using Audlet.Library;
using Audlet.Player;

namespace Audlet.ViewModels;

public enum Section { Library, Started, New, Finished, Queue, RecentlyAdded, NearlyDone, Paused, Bookmarks, Statistics, Settings, NowPlaying }

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
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(OpenUpdateCommand))] private bool _updateBusy;
    [ObservableProperty] private string _updateLabel = "Download";

    public event Action? LibraryChanged;
    public event Action<bool>? HotkeysToggled;
    public event Action? HotkeysChanged;
    public void ReloadHotkeys() => HotkeysChanged?.Invoke();

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

    private bool CanOpenUpdate() => !UpdateBusy;

    // The installer is fetched and checked against the size and checksum GitHub publishes before it is started.
    [RelayCommand(CanExecute = nameof(CanOpenUpdate))]
    private async Task OpenUpdate()
    {
        if (Update is null) return;
        if (Update.InstallerUrl is null) { OpenPage(Update.PageUrl); return; }
        UpdateBusy = true; UpdateLabel = "Downloading...";
        try
        {
            var path = await UpdateChecker.DownloadInstallerAsync(Update);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            Update = null;
        }
        catch (Exception ex)
        {
            ErrorLog.Write(AppPaths.Root, "update", ex);
            ShowToast("Could not download the update: " + ex.Message);
            OpenPage(Update.PageUrl);
        }
        finally { UpdateBusy = false; UpdateLabel = "Download"; }
    }

    private static void OpenPage(string url) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
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
        LibraryMerger.Merge(Store.Library.Books, scanned, roots, loose);
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
