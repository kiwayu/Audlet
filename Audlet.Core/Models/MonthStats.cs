namespace Audlet.Models;

public sealed class MonthStats
{
    public required string Month { get; set; }   // "2026-09"
    public long ListenedSec { get; set; }
}

public sealed class DayStats
{
    public required string Day { get; set; }   // "2026-09-20"
    public long ListenedSec { get; set; }
}

public sealed class StatsFile
{
    public List<MonthStats> Months { get; set; } = [];
    public List<DayStats> Days { get; set; } = [];

    // A copy with lists of its own, to be written out while the UI thread goes on adding days and months.
    public StatsFile Copy() => new() { Months = [.. Months], Days = [.. Days] };
}

public sealed class LibraryFile
{
    public List<Book> Books { get; set; } = [];
}
