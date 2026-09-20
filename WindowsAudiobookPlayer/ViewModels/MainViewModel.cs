using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsAudiobookPlayer.Data;
using WindowsAudiobookPlayer.Library;
using WindowsAudiobookPlayer.Player;

namespace WindowsAudiobookPlayer.ViewModels;

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

    public event Action? LibraryChanged;

    public MainViewModel(AppStore store, PlaybackController controller, CoverCache covers)
    {
        Store = store; Controller = controller; Covers = covers;
        FirstRun = store.Settings.RootFolders.Count == 0;
        controller.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(PlaybackController.Toast) && controller.Toast is { } t) ShowToast(t); };
    }

    [RelayCommand] public void ToggleNowPlaying() => Section = Section == Section.NowPlaying ? Section.Library : Section.NowPlaying;

    [RelayCommand]
    public async Task RescanAsync()
    {
        if (IsScanning) return;
        IsScanning = true;
        var roots = Store.Settings.RootFolders.ToList();
        var progress = new Progress<string>(p => ScanStatus = Path.GetFileName(p));
        var scanned = await Task.Run(() => new LibraryScanner(MetadataReader.Read).Scan(roots, progress));
        LibraryMerger.Merge(Store.Library.Books, scanned);
        await Task.Run(() => { foreach (var b in Store.Library.Books.Where(b => !b.Missing && b.CoverPath is null)) b.CoverPath = Covers.Ensure(b); });
        Store.SaveNow();
        IsScanning = false; ScanStatus = "";
        FirstRun = Store.Settings.RootFolders.Count == 0;
        LibraryChanged?.Invoke();
    }

    public async void ShowToast(string text)
    {
        Toast = text;
        await Task.Delay(2500);
        if (Toast == text) Toast = null;
    }
}
