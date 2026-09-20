using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.ViewModels;

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
