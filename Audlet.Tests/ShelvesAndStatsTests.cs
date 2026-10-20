using Audlet.Library;
using Audlet.Models;
using Audlet.Player;

namespace Audlet.Tests;

public class ShelvesAndStatsTests
{
    private static readonly DateTime Now = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private static Book Make(BookState state, double position = 0, double total = 100, DateTime? lastPlayed = null, DateTime? added = null) => new()
    {
        Id = "x", State = state, AddedUtc = added ?? Now.AddDays(-90), LastPlayedUtc = lastPlayed,
        Files = [new AudioFile("a", total, "a")], FileIndex = 0, PositionSec = position,
    };

    [Fact]
    public void ShelvesFollowTheirRules()
    {
        Assert.True(Shelves.RecentlyAdded(Make(BookState.New, added: Now.AddDays(-3)), Now));
        Assert.False(Shelves.RecentlyAdded(Make(BookState.New, added: Now.AddDays(-40)), Now));
        Assert.True(Shelves.NearlyDone(Make(BookState.Started, position: 80)));
        Assert.False(Shelves.NearlyDone(Make(BookState.Started, position: 50)));
        Assert.False(Shelves.NearlyDone(Make(BookState.Finished, position: 99)));
        Assert.True(Shelves.Paused(Make(BookState.Started, lastPlayed: Now.AddDays(-45)), Now));
        Assert.False(Shelves.Paused(Make(BookState.Started, lastPlayed: Now.AddDays(-2)), Now));
    }

    [Fact]
    public void SummaryCountsStreakAverageAndTopAuthor()
    {
        var today = new DateTime(2026, 10, 20);
        var stats = new StatsFile
        {
            Days =
            [
                new DayStats { Day = "2026-10-20", ListenedSec = 600 },
                new DayStats { Day = "2026-10-19", ListenedSec = 1200 },
                new DayStats { Day = "2026-10-18", ListenedSec = 1800 },
                new DayStats { Day = "2026-10-16", ListenedSec = 3600 },
            ],
        };
        var books = new List<Book>
        {
            new() { Id = "a", Author = "Bram Stoker", ListenedSec = 5000, State = BookState.Finished },
            new() { Id = "b", Author = "Jane Austen", ListenedSec = 2000, State = BookState.Started },
            new() { Id = "c", Author = "Jane Austen", ListenedSec = 4000, State = BookState.Finished },
        };

        var s = StatsSummary.Compute(stats, books, today);

        Assert.Equal(2, s.BooksFinished);
        Assert.Equal(3, s.StreakDays);
        Assert.Equal(1800, s.AveragePerListeningDaySec);
        Assert.Equal("Jane Austen", s.TopAuthor);
        Assert.Equal(14, s.LastDays.Count);
        Assert.Equal(1.0, s.LastDays[^5].Fraction);
        Assert.Equal(0, s.LastDays[^4].ListenedSec);
    }

    [Fact]
    public void StreakSkipsAnUnstartedToday()
    {
        var stats = new StatsFile { Days = [new DayStats { Day = "2026-10-19", ListenedSec = 60 }, new DayStats { Day = "2026-10-18", ListenedSec = 60 }] };
        Assert.Equal(2, StatsSummary.Compute(stats, [], new DateTime(2026, 10, 20)).StreakDays);
    }

    [Fact]
    public void SrtParsesTimingsAndStripsTags()
    {
        var text = "1\r\n00:00:01,000 --> 00:00:03,500\r\nHello <i>there</i>\r\nsecond line\r\n\r\n2\r\n01:02:03.250 --> 01:02:04.000\r\nLater\r\n";
        var cues = Srt.Parse(text);
        Assert.Equal(2, cues.Count);
        Assert.Equal(1.0, cues[0].StartSec);
        Assert.Equal(3.5, cues[0].EndSec);
        Assert.Equal("Hello there second line", cues[0].Text);
        Assert.Equal(3723.25, cues[1].StartSec);
        Assert.Equal("Hello there second line", Srt.At(cues, 2));
        Assert.Null(Srt.At(cues, 10));
    }
}
