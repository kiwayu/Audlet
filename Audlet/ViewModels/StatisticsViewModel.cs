using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Audlet.Player;

namespace Audlet.ViewModels;

public sealed record MonthRow(string Month, string Listened, double Fraction);
public sealed record BookRow(string Title, string Listened);

public sealed class StatisticsViewModel : ObservableObject
{
    public string TotalListened { get; }
    public ObservableCollection<MonthRow> Months { get; } = [];
    public ObservableCollection<BookRow> Books { get; } = [];
    public ObservableCollection<MonthRow> Days { get; } = [];
    public int BooksFinished { get; }
    public int Streak { get; }
    public string AveragePerDay { get; }
    public string TopAuthor { get; }

    public StatisticsViewModel(MainViewModel main)
    {
        var months = main.Store.Stats.Months.OrderByDescending(m => m.Month).ToList();
        var max = months.Count == 0 ? 1 : Math.Max(1, months.Max(m => m.ListenedSec));
        foreach (var m in months) Months.Add(new MonthRow(m.Month, Hours(m.ListenedSec), (double)m.ListenedSec / max));
        TotalListened = Hours(months.Sum(m => m.ListenedSec));
        foreach (var b in main.Store.Library.Books.Where(b => b.ListenedSec > 0).OrderByDescending(b => b.ListenedSec))
            Books.Add(new BookRow(b.Title, Hours(b.ListenedSec)));
        var summary = StatsSummary.Compute(main.Store.Stats, main.Store.Library.Books, DateTime.Today);
        BooksFinished = summary.BooksFinished;
        Streak = summary.StreakDays;
        AveragePerDay = Hours(summary.AveragePerListeningDaySec);
        TopAuthor = summary.TopAuthor.Length > 0 ? summary.TopAuthor : "nobody yet";
        foreach (var d in summary.LastDays)
            Days.Add(new MonthRow(DateTime.ParseExact(d.Day, StatsSummary.DayFormat, CultureInfo.InvariantCulture).ToString("ddd d MMM"), Hours(d.ListenedSec), d.Fraction));
    }

    public static string Hours(long sec) => (sec / 3600) + "h " + (sec % 3600 / 60).ToString("00") + "m";
}
