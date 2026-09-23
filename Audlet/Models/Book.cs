namespace Audlet.Models;

public enum BookState { New, Started, Finished }
public enum HistoryKind { Seek, Jump, BookSwitch }

public sealed record AudioFile(string Path, double DurationSec, string Title);
public sealed record Chapter(string Title, int FileIndex, double StartSec, double EndSec);
public sealed record Bookmark(int FileIndex, double PositionSec, string Note, DateTime CreatedUtc);
public sealed record Character(string Name, string Description);
public sealed record HistoryEntry(HistoryKind Kind, int FileIndex, double PositionSec, DateTime AtUtc);

public sealed class Book
{
    public const int HistoryCap = 100;

    public required string Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Breadcrumb { get; set; } = "";
    public List<AudioFile> Files { get; set; } = [];
    public List<Chapter> Chapters { get; set; } = [];
    public string? CoverPath { get; set; }
    public BookState State { get; set; } = BookState.New;
    public int FileIndex { get; set; }
    public double PositionSec { get; set; }
    public double? Speed { get; set; }
    public double[]? EqBands { get; set; }
    public int PitchSemitones { get; set; }
    public int SkipStartSec { get; set; }
    public int SkipEndSec { get; set; }
    public List<Bookmark> Bookmarks { get; set; } = [];
    public List<Character> Characters { get; set; } = [];
    public List<HistoryEntry> History { get; set; } = [];
    public long ListenedSec { get; set; }
    public DateTime AddedUtc { get; set; }
    public DateTime? LastPlayedUtc { get; set; }
    public bool Missing { get; set; }
    public int? DozeFileIndex { get; set; }        // where the sleep fade began, offered as a rewind target on the next play
    public double? DozePositionSec { get; set; }

    public double TotalDurationSec => Files.Sum(f => f.DurationSec);
    public double BookPositionSec => Files.Take(FileIndex).Sum(f => f.DurationSec) + PositionSec;
}
