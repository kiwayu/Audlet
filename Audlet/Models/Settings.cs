namespace Audlet.Models;

public enum EndOfBookAction { Stop, NextBook, NextBookPlay }
public enum PositionMode { File, Book }
public enum SortMode { Title, Author, Series, RecentlyPlayed, Length, DateAdded }

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
    public List<string> LooseFiles { get; set; } = [];   // audio files dropped onto the window, each its own book
    public int SmallRewindSec { get; set; } = 10;
    public int BigRewindSec { get; set; } = 60;
    public bool ShowPrevNextFile { get; set; } = true;   // previous and next chapter buttons
    public Dictionary<string, string> Keys { get; set; } = Keybindings.Defaults();   // action name to gesture, see Keybindings
    public Dictionary<string, string> GlobalKeys { get; set; } = Keybindings.GlobalDefaults();
    public bool AutoRewind { get; set; } = true;
    public bool SkipSilence { get; set; }
    public double DefaultSpeed { get; set; } = 1.0;
    public double[] DefaultEqBands { get; set; } = [0, 0, 0, 0, 0];
    public double BoostDb { get; set; }
    public bool Mono { get; set; }
    public double Balance { get; set; }
    public bool CloseToTray { get; set; } = true;
    public bool CheckForUpdates { get; set; } = true;
    public bool AllowCoverLookup { get; set; } = true;   // the only other thing that goes online: title and author, on request
    public string? BackupFolder { get; set; }            // a second place for the daily backup zip, such as a synced folder
    public bool GlobalHotkeys { get; set; } = true;
    public bool PauseOnDeviceLoss { get; set; } = true;
    public static readonly double[] SpeedPresets = [1.0, 1.25, 1.5, 1.75, 2.0];
    public int SleepDefaultMin { get; set; } = 30;
    public bool SleepFadeout { get; set; } = true;
    public SleepSchedule SleepSchedule { get; set; } = new();
    public EndOfBookAction EndOfBookAction { get; set; } = EndOfBookAction.Stop;
    public PositionMode PositionMode { get; set; } = PositionMode.File;
    public SortMode SortMode { get; set; } = SortMode.RecentlyPlayed;
    public string Theme { get; set; } = "System";   // a ThemeCatalog name, or System to follow Windows
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
