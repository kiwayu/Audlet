namespace WindowsAudiobookPlayer.Models;

public sealed class MonthStats
{
    public required string Month { get; set; }   // "2026-09"
    public long ListenedSec { get; set; }
}

public sealed class StatsFile
{
    public List<MonthStats> Months { get; set; } = [];
}

public sealed class LibraryFile
{
    public List<Book> Books { get; set; } = [];
}
