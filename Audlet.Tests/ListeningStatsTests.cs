using Audlet.Models;
using Audlet.Player;

namespace Audlet.Tests;

public class ListeningStatsTests
{
    // Mid-month noon UTC, so the local date is the same in every time zone from UTC-11 to UTC+11.
    private static readonly DateTime T0 = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void WholeSecondsGoToTheBookTheMonthAndTheDay()
    {
        var book = new Book { Id = "b", ListenedSec = 10 };
        var stats = new StatsFile { Months = [new MonthStats { Month = "2026-03", ListenedSec = 100 }] };
        var clock = new ListeningStats();
        clock.Restart(T0);

        clock.Add(book, stats, T0.AddSeconds(2.5));

        Assert.Equal(12, book.ListenedSec);
        Assert.Equal(102, Assert.Single(stats.Months).ListenedSec);
        var day = Assert.Single(stats.Days);
        Assert.Equal(("2026-03-15", 2L), (day.Day, day.ListenedSec));
    }

    [Fact]
    public void LessThanASecondOrAClockGoingBackAddsNothing()
    {
        var book = new Book { Id = "b" };
        var stats = new StatsFile();
        var clock = new ListeningStats();
        clock.Restart(T0);

        clock.Add(book, stats, T0.AddSeconds(0.9));
        clock.Add(book, stats, T0.AddSeconds(-5));
        Assert.Equal(0, book.ListenedSec);
        Assert.Empty(stats.Days);

        clock.Add(book, stats, T0.AddSeconds(3));
        clock.Add(book, stats, T0.AddSeconds(4.2));
        Assert.Equal(4, book.ListenedSec);
        Assert.Equal(4, Assert.Single(stats.Days).ListenedSec);
    }

    [Fact]
    public void PastFourHundredDaysOnlyTheNewestYearIsKept()
    {
        var stats = new StatsFile { Days = Enumerable.Range(1, 400).Select(i => new DayStats { Day = DateTime.Parse("2025-03-14").AddDays(-i).ToString("yyyy-MM-dd"), ListenedSec = 60 }).ToList() };
        var clock = new ListeningStats();
        clock.Restart(T0);

        clock.Add(new Book { Id = "b" }, stats, T0.AddSeconds(5));

        Assert.Equal(366, stats.Days.Count);
        Assert.Equal("2026-03-15", stats.Days[0].Day);
        Assert.Equal("2025-03-13", stats.Days[1].Day);
        Assert.Equal(DateTime.Parse("2025-03-14").AddDays(-365).ToString("yyyy-MM-dd"), stats.Days[^1].Day);
    }
}
