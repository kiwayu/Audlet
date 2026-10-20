using System.Globalization;
using Audlet.Models;

namespace Audlet.Player;

public sealed record DayRow(string Day, long ListenedSec, double Fraction);

// The figures on the Statistics page that are derived rather than stored.
public sealed record StatsSummary(int BooksFinished, int StreakDays, long AveragePerListeningDaySec, string TopAuthor, IReadOnlyList<DayRow> LastDays)
{
    public const string DayFormat = "yyyy-MM-dd";

    public static StatsSummary Compute(StatsFile stats, IReadOnlyList<Book> books, DateTime today, int days = 14)
    {
        var byDay = stats.Days.GroupBy(d => d.Day).ToDictionary(g => g.Key, g => g.Sum(d => d.ListenedSec));
        long Listened(DateTime day) => byDay.GetValueOrDefault(day.ToString(DayFormat));

        var rows = Enumerable.Range(0, days).Select(i => today.AddDays(i - days + 1)).Select(d => new DayRow(d.ToString(DayFormat), Listened(d), 0)).ToList();
        var max = Math.Max(1, rows.Max(r => r.ListenedSec));
        rows = rows.Select(r => r with { Fraction = (double)r.ListenedSec / max }).ToList();

        // A streak counts back from today, or from yesterday when today has not started yet.
        var streak = 0;
        var cursor = Listened(today) > 0 ? today : today.AddDays(-1);
        while (Listened(cursor) > 0) { streak++; cursor = cursor.AddDays(-1); }

        var recent = stats.Days
            .Where(d => d.ListenedSec > 0 && DateTime.TryParseExact(d.Day, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) && dt > today.AddDays(-30))
            .ToList();
        var average = recent.Count == 0 ? 0 : recent.Sum(d => d.ListenedSec) / recent.Count;

        var top = books.Where(b => b.ListenedSec > 0 && b.Author.Length > 0)
            .GroupBy(b => b.Author).OrderByDescending(g => g.Sum(b => b.ListenedSec)).Select(g => g.Key).FirstOrDefault() ?? "";

        return new StatsSummary(books.Count(b => b.State == BookState.Finished), streak, average, top, rows);
    }
}
