using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Audlet.Converters;
using Audlet.Models;
using Audlet.Player;

namespace Audlet.ViewModels;

public sealed record ChapterRow(Chapter Chapter, int Index, string Length, bool IsCurrent, bool IsPast);
public sealed record TickMark(double Fraction, string Title, double StartSec);

public sealed partial class NowPlayingViewModel : ObservableObject
{
    public MainViewModel Main { get; }
    public PlaybackController Controller => Main.Controller;
    public Settings Settings => Main.Store.Settings;
    public ObservableCollection<ChapterRow> Chapters { get; } = [];
    public ObservableCollection<TickMark> Ticks { get; } = [];
    public ObservableCollection<Bookmark> Bookmarks { get; } = [];
    public ObservableCollection<HistoryEntry> History { get; } = [];
    public int[] SleepChoices => Models.Settings.SleepChoices;
    public double[] SpeedChoices { get; } = [0.75, 1.0, 1.25, 1.5, 1.75, 2.0, 2.5];

    [ObservableProperty] private string _newNote = "";
    [ObservableProperty] private double _eq0;
    [ObservableProperty] private double _eq1;
    [ObservableProperty] private double _eq2;
    [ObservableProperty] private double _eq3;
    [ObservableProperty] private double _eq4;
    [ObservableProperty] private int _pitch;
    [ObservableProperty] private double _boost;
    [ObservableProperty] private int _skipStart;
    [ObservableProperty] private int _skipEnd;

    public bool IsBookMode => Settings.PositionMode == PositionMode.Book;
    public double ShownPosition => IsBookMode ? Controller.BookPositionSec : Controller.PositionSec;
    public double ShownTotal => IsBookMode ? Controller.BookTotalSec : Controller.FileDurationSec;
    public string ChapterLabel => Controller.CurrentChapter is { } c && Controller.CurrentBook is { } b
        ? "Chapter " + (b.Chapters.IndexOf(c) + 1) + " of " + b.Chapters.Count : "";
    public string SleepLabel => Controller.SleepActive
        ? (Controller.SleepRemaining > TimeSpan.Zero ? Controller.SleepRemaining.ToString(@"mm\:ss") : "chapter")
        : "Sleep";

    public NowPlayingViewModel(MainViewModel main)
    {
        Main = main;
        var b = Controller.CurrentBook;
        var eq = b?.EqBands ?? Settings.DefaultEqBands;
        _eq0 = eq[0]; _eq1 = eq[1]; _eq2 = eq[2]; _eq3 = eq[3]; _eq4 = eq[4];
        _pitch = b?.PitchSemitones ?? 0; _boost = Settings.BoostDb; _skipStart = b?.SkipStartSec ?? 0; _skipEnd = b?.SkipEndSec ?? 0;
        Controller.PropertyChanged += (o, e) =>
        {
            if (e.PropertyName is nameof(PlaybackController.PositionSec) or nameof(PlaybackController.BookPositionSec))
            { OnPropertyChanged(nameof(ShownPosition)); OnPropertyChanged(nameof(ShownTotal)); }
            if (e.PropertyName is nameof(PlaybackController.CurrentChapter) or nameof(PlaybackController.CurrentBook))
            { RebuildChapters(); RebuildLists(); RebuildTicks(); OnPropertyChanged(nameof(ChapterLabel)); }
            if (e.PropertyName is nameof(PlaybackController.SleepRemaining) or nameof(PlaybackController.SleepActive))
                OnPropertyChanged(nameof(SleepLabel));
        };
        RebuildChapters(); RebuildLists(); RebuildTicks();
    }

    private void RebuildChapters()
    {
        Chapters.Clear();
        if (Controller.CurrentBook is not { } b) return;
        var cur = Controller.CurrentChapter is { } c ? b.Chapters.IndexOf(c) : -1;
        for (var i = 0; i < b.Chapters.Count; i++)
            Chapters.Add(new ChapterRow(b.Chapters[i], i, TimeConverter.Format(b.Chapters[i].EndSec - b.Chapters[i].StartSec), i == cur, i < cur));
    }

    // Chapter starts as fractions of the seek bar: the current file in file mode, the whole book in book mode.
    public void RebuildTicks()
    {
        Ticks.Clear();
        if (Controller.CurrentBook is not { } b) return;
        if (IsBookMode)
        {
            var total = b.TotalDurationSec;
            if (total <= 0) return;
            var offsets = new double[b.Files.Count]; double acc = 0;
            for (var i = 0; i < b.Files.Count; i++) { offsets[i] = acc; acc += b.Files[i].DurationSec; }
            foreach (var c in b.Chapters)
            {
                var start = (c.FileIndex < offsets.Length ? offsets[c.FileIndex] : 0) + c.StartSec;
                if (start > 0) Ticks.Add(new TickMark(start / total, c.Title, start));
            }
        }
        else
        {
            var total = Controller.FileDurationSec;
            if (total <= 0) return;
            foreach (var c in b.Chapters.Where(c => c.FileIndex == Controller.FileIndex && c.StartSec > 0))
                Ticks.Add(new TickMark(c.StartSec / total, c.Title, c.StartSec));
        }
    }

    public string ChapterAtFraction(double fraction)
    {
        if (Controller.CurrentBook is not { } b) return "";
        var total = IsBookMode ? b.TotalDurationSec : Controller.FileDurationSec;
        var sec = fraction * total;
        var tick = Ticks.LastOrDefault(t => t.StartSec <= sec);
        var name = tick?.Title ?? (IsBookMode ? b.Chapters.FirstOrDefault()?.Title : b.Chapters.FirstOrDefault(c => c.FileIndex == Controller.FileIndex)?.Title) ?? "";
        return name + "  " + TimeConverter.Format(sec);
    }

    private void RebuildLists()
    {
        Bookmarks.Clear(); History.Clear();
        if (Controller.CurrentBook is not { } b) return;
        foreach (var x in b.Bookmarks) Bookmarks.Add(x);
        foreach (var x in b.History) History.Add(x);
    }

    [RelayCommand] private void JumpToChapter(ChapterRow r) => Controller.SeekFile(r.Chapter.FileIndex, r.Chapter.StartSec, HistoryKind.Jump);
    [RelayCommand] private void JumpToBookmark(Bookmark b) => Controller.JumpToBookmark(b);
    [RelayCommand] private void DeleteBookmark(Bookmark b) { Controller.CurrentBook?.Bookmarks.Remove(b); Main.Store.SaveSoon(); RebuildLists(); }
    [RelayCommand] private void UndoTo(HistoryEntry e) => Controller.UndoTo(e);
    [RelayCommand] private void AddBookmark() { Controller.AddBookmark(NewNote.Trim()); NewNote = ""; RebuildLists(); }
    [RelayCommand] private void TogglePositionMode()
    {
        Settings.PositionMode = IsBookMode ? PositionMode.File : PositionMode.Book; Main.Store.SaveSoon();
        OnPropertyChanged(nameof(IsBookMode)); OnPropertyChanged(nameof(ShownPosition)); OnPropertyChanged(nameof(ShownTotal));
        RebuildTicks();
    }
    [RelayCommand] private void ToggleLock() => Controller.Locked = !Controller.Locked;
    [RelayCommand] private void SpeedPreset(double s) => Controller.SetSpeed(s);
    [RelayCommand] private void SleepPreset(int min) => Controller.StartSleep(min);
    [RelayCommand] private void SleepChapterEnd() => Controller.StartSleepUntilChapterEnd();
    [RelayCommand] private void StopSleep() => Controller.StopSleep();
    [RelayCommand] private void SeekTo(double sec) { if (IsBookMode) Controller.SeekBook(sec); else Controller.SeekFile(Controller.FileIndex, sec); }
    [RelayCommand] private void ResetEq() { Eq0 = Eq1 = Eq2 = Eq3 = Eq4 = 0; }
    [RelayCommand] private void SetDefaultSpeed() { Settings.DefaultSpeed = Controller.Speed; Main.Store.SaveSoon(); Main.ShowToast("Default speed set"); }
    [RelayCommand] private void SetDefaultEq() { Settings.DefaultEqBands = [Eq0, Eq1, Eq2, Eq3, Eq4]; Main.Store.SaveSoon(); Main.ShowToast("Default equalizer set"); }

    partial void OnEq0Changed(double v) => PushEq();
    partial void OnEq1Changed(double v) => PushEq();
    partial void OnEq2Changed(double v) => PushEq();
    partial void OnEq3Changed(double v) => PushEq();
    partial void OnEq4Changed(double v) => PushEq();
    private void PushEq() => Controller.SetEq([Eq0, Eq1, Eq2, Eq3, Eq4]);
    partial void OnPitchChanged(int v) => Controller.SetPitch(v);
    partial void OnBoostChanged(double v) => Controller.SetBoost(v);
    partial void OnSkipStartChanged(int v) { if (Controller.CurrentBook is { } b) { b.SkipStartSec = v; Main.Store.SaveSoon(); } }
    partial void OnSkipEndChanged(int v) { if (Controller.CurrentBook is { } b) { b.SkipEndSec = v; Main.Store.SaveSoon(); } }
}
