namespace WindowsAudiobookPlayer.Models;

public enum EndOfBookAction { Stop, NextBook, NextBookPlay }
public enum PositionMode { File, Book }
public enum SortMode { Title, Author, RecentlyPlayed, Length, DateAdded }
public enum ThemeOverride { System, Light, Dark }

public sealed class SleepSchedule
{
    public bool Enabled { get; set; }
    public TimeOnly FromTime { get; set; } = new(22, 0);
    public TimeOnly ToTime { get; set; } = new(6, 0);
}

public sealed class Settings
{
    public static readonly int[] RewindChoices = [5, 10, 15, 20, 30, 45, 60, 90, 120, 300];
    public static readonly int[] SleepChoices = [5, 10, 15, 20, 30, 45, 60, 75, 90];

    public List<string> RootFolders { get; set; } = [];
    public int SmallRewindSec { get; set; } = 10;
    public int BigRewindSec { get; set; } = 60;
    public bool ShowPrevNextFile { get; set; } = true;
    public bool AutoRewind { get; set; } = true;
    public bool SkipSilence { get; set; }
    public double DefaultSpeed { get; set; } = 1.0;
    public double[] DefaultEqBands { get; set; } = [0, 0, 0, 0, 0];
    public double BoostDb { get; set; }
    public bool Mono { get; set; }
    public double Balance { get; set; }
    public int SleepDefaultMin { get; set; } = 30;
    public bool SleepFadeout { get; set; } = true;
    public SleepSchedule SleepSchedule { get; set; } = new();
    public EndOfBookAction EndOfBookAction { get; set; } = EndOfBookAction.Stop;
    public PositionMode PositionMode { get; set; } = PositionMode.File;
    public SortMode SortMode { get; set; } = SortMode.RecentlyPlayed;
    public ThemeOverride Theme { get; set; } = ThemeOverride.System;
    public List<string> Queue { get; set; } = [];
    public string? LastBookId { get; set; }
    public double SidebarWidth { get; set; } = 220;
    public double ChaptersPanelFraction { get; set; } = 1.0 / 3.0;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 760;
    public bool WindowMaximized { get; set; }
}
