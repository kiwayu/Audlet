using Audlet.Audio;
using Audlet.Data;
using Audlet.Library;
using Audlet.Models;
using Audlet.Player;
using Audlet.Views;
using Audlet.ViewModels;

namespace Audlet.Tests;

// What each sidebar section lists, and in which order.
public class LibrarySectionsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-sections-" + Guid.NewGuid().ToString("N"));
    private readonly BassPlayer _player = new();
    private readonly AppStore _store;
    private readonly MainViewModel _main;

    public LibrarySectionsTests()
    {
        // Debounced saves never fire, so nothing is written after the folder is deleted.
        _store = new AppStore(_dir) { DebounceMs = Timeout.Infinite };
        _store.Load();
        var now = DateTime.UtcNow;
        _store.Library.Books.AddRange(
        [
            Make("Alpha", "Zed", BookState.New, sec: 150, added: now.AddDays(-5)),
            Make("beta", "Young", BookState.Started, sec: 100, added: now.AddDays(-100), played: now.AddDays(-1), position: 80, series: ("Saga", 2)),
            Make("Gamma", "Xavier", BookState.Started, sec: 300, added: now.AddDays(-60), played: now.AddDays(-40), position: 10, series: ("Saga", 1)),
            Make("Delta", "Walker", BookState.Finished, sec: 200, added: now.AddDays(-2), played: now.AddDays(-3)),
        ]);
        _store.Settings.Queue = [@"C:\BOOKS\GAMMA", @"C:\Books\Gone", @"C:\Books\Alpha"];
        _main = new MainViewModel(_store, new PlaybackController(_player, _store, UiTimer.Every), new CoverCache(Path.Combine(_dir, "covers"), new WpfCoverCodec()), new WpfShell());
    }

    public void Dispose()
    {
        _player.Dispose();
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    private static Book Make(string title, string author, BookState state, double sec, DateTime added,
                             DateTime? played = null, double position = 0, (string Name, double Index)? series = null) =>
        new()
        {
            Id = @"C:\Books\" + title, Title = title, Author = author, State = state, AddedUtc = added, LastPlayedUtc = played,
            Files = [new AudioFile(@"C:\Books\" + title + @"\01.mp3", sec, "01")], PositionSec = position,
            Series = series?.Name, SeriesIndex = series?.Index,
        };

    private static string[] Titles(LibraryViewModel vm) => vm.Books.Select(b => b.Title).ToArray();

    [Theory]
    [InlineData(Section.Library, new[] { "beta", "Delta", "Gamma", "Alpha" })]
    [InlineData(Section.Started, new[] { "beta", "Gamma" })]
    [InlineData(Section.New, new[] { "Alpha" })]
    [InlineData(Section.Finished, new[] { "Delta" })]
    [InlineData(Section.RecentlyAdded, new[] { "Delta", "Alpha" })]
    [InlineData(Section.NearlyDone, new[] { "beta" })]
    [InlineData(Section.Paused, new[] { "Gamma" })]
    [InlineData(Section.Queue, new[] { "Gamma", "Alpha" })]
    public void EachSectionListsItsBooks(Section section, string[] expected) =>
        Assert.Equal(expected, Titles(new LibraryViewModel(_main, section)));

    [Theory]
    [InlineData(SortMode.Title, new[] { "Alpha", "beta", "Delta", "Gamma" })]
    [InlineData(SortMode.Author, new[] { "Delta", "Gamma", "beta", "Alpha" })]
    [InlineData(SortMode.Series, new[] { "Alpha", "Delta", "Gamma", "beta" })]
    [InlineData(SortMode.Length, new[] { "Gamma", "Delta", "Alpha", "beta" })]
    [InlineData(SortMode.DateAdded, new[] { "Delta", "Alpha", "Gamma", "beta" })]
    [InlineData(SortMode.RecentlyPlayed, new[] { "beta", "Delta", "Gamma", "Alpha" })]
    public void SortOrdersTheLibraryAndIsRemembered(SortMode sort, string[] expected)
    {
        var vm = new LibraryViewModel(_main, Section.Library) { Sort = sort };
        Assert.Equal(expected, Titles(vm));
        Assert.Equal(sort, _store.Settings.SortMode);
        Assert.Equal(expected, Titles(new LibraryViewModel(_main, Section.Library)));
    }

    [Fact]
    public void TheQueueKeepsItsOwnOrderWhateverTheSort()
    {
        var vm = new LibraryViewModel(_main, Section.Queue) { Sort = SortMode.Title };
        Assert.Equal(["Gamma", "Alpha"], Titles(vm));
    }

    [Fact]
    public void SearchMatchesTitleOrAuthorIgnoringCaseWithinTheSection()
    {
        var vm = new LibraryViewModel(_main, Section.Library) { Search = "YOUNG" };
        Assert.Equal(["beta"], Titles(vm));
        vm.Search = "alp";
        Assert.Equal(["Alpha"], Titles(vm));
        var started = new LibraryViewModel(_main, Section.Started) { Search = "xav" };
        Assert.Equal(["Gamma"], Titles(started));
        vm.Search = " ";
        Assert.Equal(4, vm.Books.Count);
    }
}
