using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.ViewModels;

public sealed record BookmarkRow(Book Book, Bookmark Bookmark);

public sealed partial class BookmarksViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    public ObservableCollection<BookmarkRow> Rows { get; } = [];

    public BookmarksViewModel(MainViewModel main) { _main = main; Refresh(); }

    private void Refresh()
    {
        Rows.Clear();
        foreach (var b in _main.Store.Library.Books)
            foreach (var m in b.Bookmarks.OrderByDescending(m => m.CreatedUtc)) Rows.Add(new BookmarkRow(b, m));
    }

    [RelayCommand]
    private void Open(BookmarkRow r)
    {
        if (_main.Controller.CurrentBook != r.Book) _main.Controller.LoadBook(r.Book, play: false);
        _main.Controller.JumpToBookmark(r.Bookmark);
        _main.Section = Section.NowPlaying;
    }

    [RelayCommand] private void Delete(BookmarkRow r) { r.Book.Bookmarks.Remove(r.Bookmark); _main.Store.SaveSoon(); Refresh(); }
}
