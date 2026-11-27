using Audlet.Models;

namespace Audlet.Player;

// Counts listening time in whole seconds onto the book and onto this month's and today's totals. The part of a second
// not counted yet is carried to the next count, across a pause too, so it adds up instead of being dropped.
public sealed class ListeningStats
{
    private DateTime _lastUtc;
    private TimeSpan _uncounted;

    public void Restart(DateTime nowUtc) => _lastUtc = nowUtc;

    public void Add(Book book, StatsFile stats, DateTime nowUtc)
    {
        if (nowUtc < _lastUtc) return;   // the clock went back: count again once it has passed the last count
        _uncounted += nowUtc - _lastUtc;
        _lastUtc = nowUtc;
        var sec = (long)_uncounted.TotalSeconds;
        if (sec == 0) return;
        _uncounted -= TimeSpan.FromSeconds(sec);
        book.ListenedSec += sec;
        var key = nowUtc.ToLocalTime().ToString("yyyy-MM");
        var month = stats.Months.FirstOrDefault(m => m.Month == key);
        if (month is null) stats.Months.Add(month = new MonthStats { Month = key });
        month.ListenedSec += sec;
        var dayKey = nowUtc.ToLocalTime().ToString("yyyy-MM-dd");
        var day = stats.Days.FirstOrDefault(d => d.Day == dayKey);
        if (day is null) stats.Days.Add(day = new DayStats { Day = dayKey });
        day.ListenedSec += sec;
        if (stats.Days.Count > 400) stats.Days = stats.Days.OrderByDescending(d => d.Day).Take(366).ToList();
    }
}
