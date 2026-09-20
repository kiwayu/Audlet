using System.Text.Json;
using WindowsAudiobookPlayer.Data;
using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Tests;

public class ModelsJsonTests
{
    [Fact]
    public void BookRoundTrips()
    {
        var at = new DateTime(2026, 9, 20, 14, 3, 0, DateTimeKind.Utc);
        var book = new Book
        {
            Id = @"C:\Books\Weir\Project Hail Mary",
            Title = "Project Hail Mary",
            Author = "Andy Weir",
            Files = [new AudioFile(@"C:\Books\Weir\Project Hail Mary\01.mp3", 1800.5, "Part 1")],
            Chapters = [new Chapter("Part 1", 0, 0, 1800.5)],
            State = BookState.Started,
            PositionSec = 12.25,
            Speed = 1.25,
            EqBands = [0, 1, 2, 3, 4],
            Bookmarks = [new Bookmark(0, 10, "note", at)],
            Characters = [new Character("Rocky", "Eridian engineer")],
            History = [new HistoryEntry(HistoryKind.Jump, 0, 5, at)],
            ListenedSec = 42,
            AddedUtc = at,
        };

        var json = JsonSerializer.Serialize(book, Json.Options);
        var back = JsonSerializer.Deserialize<Book>(json, Json.Options)!;

        Assert.Equal(book.Id, back.Id);
        Assert.Equal("Started", JsonDocument.Parse(json).RootElement.GetProperty("state").GetString());
        Assert.Contains("2026-09-20T14:03:00Z", json);
        Assert.Equal(book.Files[0].Path, back.Files[0].Path);
        Assert.Equal("note", back.Bookmarks[0].Note);
        Assert.Equal(1.25, back.Speed);
        Assert.Equal(book.EqBands, back.EqBands);
        Assert.Equal(1800.5, back.TotalDurationSec);
    }

    [Fact]
    public void SettingsHaveSaneDefaults()
    {
        var s = new Settings();
        Assert.Equal(10, s.SmallRewindSec);
        Assert.Equal(60, s.BigRewindSec);
        Assert.Equal(1.0, s.DefaultSpeed);
        Assert.Equal(5, s.DefaultEqBands.Length);
        Assert.Equal(EndOfBookAction.Stop, s.EndOfBookAction);
        Assert.Empty(s.RootFolders);
    }
}
