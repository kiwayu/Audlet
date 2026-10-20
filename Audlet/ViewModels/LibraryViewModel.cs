using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Audlet.Library;
using Audlet.Models;
using Audlet.Views;

namespace Audlet.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Section _section;

    public ObservableCollection<Book> Books { get; } = [];
    public IReadOnlyList<SortMode> SortModes { get; } = Enum.GetValues<SortMode>();
    public string Heading => _section switch { Section.Started => "Started", Section.New => "New", Section.Finished => "Finished", Section.Queue => "Queue", _ => "Library" };

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private SortMode _sort;

    public LibraryViewModel(MainViewModel main, Section section)
    {
        _main = main; _section = section;
        _sort = main.Store.Settings.SortMode;
        main.LibraryChanged += Refresh;
        Refresh();
    }

    partial void OnSearchChanged(string value) => Refresh();
    partial void OnSortChanged(SortMode value) { _main.Store.Settings.SortMode = value; _main.Store.SaveSoon(); Refresh(); }

    public void Refresh()
    {
        IEnumerable<Book> q = _main.Store.Library.Books;
        q = _section switch
        {
            Section.Started => q.Where(b => b.State == BookState.Started),
            Section.New => q.Where(b => b.State == BookState.New),
            Section.Finished => q.Where(b => b.State == BookState.Finished),
            Section.Queue => _main.Store.Settings.Queue
                .Select(id => _main.Store.Library.Books.FirstOrDefault(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                .Where(b => b is not null)!,
            _ => q,
        };
        if (!string.IsNullOrWhiteSpace(Search))
            q = q.Where(b => b.Title.Contains(Search, StringComparison.OrdinalIgnoreCase) || b.Author.Contains(Search, StringComparison.OrdinalIgnoreCase));
        if (_section != Section.Queue)
            q = Sort switch
            {
                SortMode.Title => q.OrderBy(b => b.Title, StringComparer.CurrentCultureIgnoreCase),
                SortMode.Author => q.OrderBy(b => b.Author, StringComparer.CurrentCultureIgnoreCase).ThenBy(b => b.Title),
                SortMode.Series => q.OrderBy(b => b.Series ?? b.Title, StringComparer.CurrentCultureIgnoreCase).ThenBy(b => b.SeriesIndex ?? 0).ThenBy(b => b.Title),
                SortMode.Length => q.OrderByDescending(b => b.TotalDurationSec),
                SortMode.DateAdded => q.OrderByDescending(b => b.AddedUtc),
                _ => q.OrderByDescending(b => b.LastPlayedUtc ?? DateTime.MinValue).ThenBy(b => b.Title),
            };
        Books.Clear();
        foreach (var b in q) Books.Add(b);
    }

    [RelayCommand] private void Open(Book b) { _main.Controller.LoadBook(b, play: true); _main.Section = Section.NowPlaying; }
    [RelayCommand] private void MarkNew(Book b) { _main.Controller.MarkState(b, BookState.New); Refresh(); }
    [RelayCommand] private void MarkStarted(Book b) { _main.Controller.MarkState(b, BookState.Started); Refresh(); }
    [RelayCommand] private void MarkFinished(Book b) { _main.Controller.MarkState(b, BookState.Finished); Refresh(); }
    [RelayCommand] private void AddToQueue(Book b) { if (!_main.Store.Settings.Queue.Contains(b.Id)) _main.Store.Settings.Queue.Add(b.Id); _main.Store.SaveSoon(); _main.ShowToast("Added to queue"); Refresh(); }
    [RelayCommand] private void RemoveFromQueue(Book b) { _main.Store.Settings.Queue.Remove(b.Id); _main.Store.SaveSoon(); Refresh(); }
    [RelayCommand] private void OpenFolder(Book b) => Process.Start("explorer.exe", Directory.Exists(b.Id) ? b.Id : Path.GetDirectoryName(b.Id)!);
    [RelayCommand] private async Task Rescan() => await _main.RescanAsync();

    private string? RootOf(string id) =>
        _main.Store.Settings.RootFolders.FirstOrDefault(r => id.StartsWith(r.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));

    private static bool ValidName(string name) => name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private async Task RelocateAsync(Book b, Func<Book, string> move)
    {
        var wasLoose = _main.Store.Settings.LooseFiles.Contains(b.Id, StringComparer.OrdinalIgnoreCase);
        var oldId = b.Id;
        try { move(b); }
        catch (Exception ex) { _main.ShowToast(ex.Message); return; }
        if (wasLoose) { _main.Store.Settings.LooseFiles.Remove(oldId); _main.Store.Settings.LooseFiles.Add(b.Id); }
        if (_main.Store.Settings.LastBookId == oldId) _main.Store.Settings.LastBookId = b.Id;
        _main.Store.SaveSoon();
        await _main.RescanAsync();
    }

    [RelayCommand]
    private async Task Rename(Book b)
    {
        var name = InputDialog.Ask("Rename", "New name for this book", b.Title);
        if (name is null || name == b.Title) return;
        if (!ValidName(name)) { _main.ShowToast("That name contains characters Windows does not allow"); return; }
        await RelocateAsync(b, x => BookMover.Rename(x, name));
    }

    [RelayCommand]
    private async Task MoveToFolder(Book b)
    {
        var root = RootOf(b.Id) ?? Path.GetDirectoryName(b.Id)!;
        var dlg = new OpenFolderDialog { Title = "Move into which folder? You can create one here.", InitialDirectory = root };
        if (dlg.ShowDialog() != true) return;
        var folder = dlg.FolderName;
        if (RootOf(folder + Path.DirectorySeparatorChar) is null && !_main.Store.Settings.RootFolders.Contains(folder, StringComparer.OrdinalIgnoreCase))
        { _main.ShowToast("Choose a folder inside one of your library folders"); return; }
        if (folder.StartsWith(b.Id + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) { _main.ShowToast("Cannot move a book into itself"); return; }
        await RelocateAsync(b, x => BookMover.MoveInto(x, folder));
    }

    [RelayCommand]
    private void NewFolder()
    {
        var root = _main.Store.Settings.RootFolders.FirstOrDefault();
        if (root is null) { _main.ShowToast("Add a library folder first"); return; }
        var name = InputDialog.Ask("New folder", "Folder name, for example a genre or an author");
        if (name is null) return;
        if (!ValidName(name)) { _main.ShowToast("That name contains characters Windows does not allow"); return; }
        var path = Path.Combine(root, name);
        if (Directory.Exists(path)) { _main.ShowToast("That folder already exists"); return; }
        Directory.CreateDirectory(path);
        _main.ShowToast("Created " + name + ". Right-click a book and choose Move to folder to fill it.");
    }

    [RelayCommand]
    private async Task FindCover(Book b)
    {
        if (!_main.Store.Settings.AllowCoverLookup) { _main.ShowToast("Online cover lookup is switched off in Settings"); return; }
        var dlg = new CoverSearchDialog(b.Title, b.Author);
        if (dlg.ShowDialog() != true || dlg.Chosen is not { } pick) return;
        try
        {
            var bytes = await CoverSearch.DownloadAsync(pick.ImageUrl);
            _main.Covers.Replace(b, bytes);
            b.CoverPath = _main.Covers.PathFor(b);
            if (dlg.ApplyDescription && !string.IsNullOrWhiteSpace(pick.Description)) b.Description = pick.Description.Trim();
            _main.Store.SaveSoon();
            _main.ShowToast("Cover updated from " + pick.Source);
            Refresh();
        }
        catch (Exception ex) { _main.ShowToast("Could not fetch that cover: " + ex.Message); }
    }

    [RelayCommand]
    private void SetCover(Book b)
    {
        var dlg = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.webp" };
        if (dlg.ShowDialog() != true) return;
        _main.Covers.Replace(b, dlg.FileName);
        b.CoverPath = _main.Covers.PathFor(b);
        _main.Store.SaveSoon();
        Refresh();
    }
}
