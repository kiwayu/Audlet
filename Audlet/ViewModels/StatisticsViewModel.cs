using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Audlet.ViewModels;

public sealed record MonthRow(string Month, string Listened, double Fraction);
public sealed record BookRow(string Title, string Listened);

public sealed class StatisticsViewModel : ObservableObject
{
    public string TotalListened { get; }
    public ObservableCollection<MonthRow> Months { get; } = [];
    public ObservableCollection<BookRow> Books { get; } = [];

    public StatisticsViewModel(MainViewModel main)
    {
        var months = main.Store.Stats.Months.OrderByDescending(m => m.Month).ToList();
        var max = months.Count == 0 ? 1 : Math.Max(1, months.Max(m => m.ListenedSec));
        foreach (var m in months) Months.Add(new MonthRow(m.Month, Hours(m.ListenedSec), (double)m.ListenedSec / max));
        TotalListened = Hours(months.Sum(m => m.ListenedSec));
        foreach (var b in main.Store.Library.Books.Where(b => b.ListenedSec > 0).OrderByDescending(b => b.ListenedSec))
            Books.Add(new BookRow(b.Title, Hours(b.ListenedSec)));
    }

    public static string Hours(long sec) => (sec / 3600) + "h " + (sec % 3600 / 60).ToString("00") + "m";
}
