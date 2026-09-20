using WindowsAudiobookPlayer.Library;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Tests;

public class LibraryMergerTests
{
    private static Book B(string id, params string[] files) => new()
    {
        Id = id, Title = Path.GetFileName(id),
        Files = files.Select(f => new AudioFile(Path.Combine(id, f), 60, f)).ToList(),
        Chapters = files.Select((f, i) => new Chapter(f, i, 0, 60)).ToList(),
        AddedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    };

    [Fact]
    public void ExistingBookKeepsStateAndGetsFreshFiles()
    {
        var old = B(@"C:\b\one", "1.mp3");
        old.State = BookState.Started; old.PositionSec = 33;
        old.Bookmarks.Add(new Bookmark(0, 5, "n", DateTime.UtcNow));
        old.Characters.Add(new Character("A", "B"));
        old.History.Add(new HistoryEntry(HistoryKind.Seek, 0, 1, DateTime.UtcNow));
        old.ListenedSec = 99;
        var existing = new List<Book> { old };

        LibraryMerger.Merge(existing, [B(@"C:\b\one", "1.mp3", "2.mp3")]);

        var merged = Assert.Single(existing);
        Assert.Same(old, merged);
        Assert.Equal(2, merged.Files.Count);
        Assert.Equal(2, merged.Chapters.Count);
        Assert.Equal(BookState.Started, merged.State);
        Assert.Equal(33, merged.PositionSec);
        Assert.Single(merged.Bookmarks);
        Assert.Single(merged.Characters);
        Assert.Single(merged.History);
        Assert.Equal(99, merged.ListenedSec);
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), merged.AddedUtc);
        Assert.False(merged.Missing);
    }

    [Fact]
    public void NewBookIsAddedAndVanishedBookIsMarkedMissing()
    {
        var existing = new List<Book> { B(@"C:\b\gone", "1.mp3") };
        LibraryMerger.Merge(existing, [B(@"C:\b\new", "1.mp3")]);
        Assert.Equal(2, existing.Count);
        Assert.True(existing.Single(b => b.Id == @"C:\b\gone").Missing);
        Assert.False(existing.Single(b => b.Id == @"C:\b\new").Missing);
    }

    [Fact]
    public void MissingBookThatReturnsIsUnmarked()
    {
        var old = B(@"C:\b\back", "1.mp3"); old.Missing = true;
        LibraryMerger.Merge([old], [B(@"C:\b\back", "1.mp3")]);
        Assert.False(old.Missing);
    }

    [Fact]
    public void FileIndexIsClampedWhenFilesShrink()
    {
        var old = B(@"C:\b\x", "1.mp3", "2.mp3", "3.mp3"); old.FileIndex = 2; old.PositionSec = 10;
        LibraryMerger.Merge([old], [B(@"C:\b\x", "1.mp3")]);
        Assert.Equal(0, old.FileIndex);
        Assert.Equal(0, old.PositionSec);
    }

    [Fact]
    public void IdComparisonIgnoresCase()
    {
        var existing = new List<Book> { B(@"C:\b\Case", "1.mp3") };
        LibraryMerger.Merge(existing, [B(@"c:\B\case", "1.mp3")]);
        Assert.Single(existing);
    }
}
