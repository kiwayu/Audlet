using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Audlet.Audio;
using Audlet.Data;
using Audlet.Models;

namespace Audlet.Player;

public sealed partial class PlaybackController : ObservableObject
{
    private readonly BassPlayer _player;
    private readonly AppStore _store;
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer _level = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly SilenceSkipper _silence = new();
    private readonly SleepTimer _sleep = new();
    private BookTimeline? _timeline;
    private DateTime? _pausedAtUtc;
    private DateTime _lastStatUtc;
    private DateTime _lastSaveUtc;
    private const double SilenceHopSec = 0.2;

    [ObservableProperty] private Book? _currentBook;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private int _fileIndex;
    [ObservableProperty] private double _positionSec;
    [ObservableProperty] private double _bookPositionSec;
    [ObservableProperty] private Chapter? _currentChapter;
    [ObservableProperty] private double _speed = 1.0;
    [ObservableProperty] private TimeSpan _sleepRemaining;
    [ObservableProperty] private bool _sleepActive;
    [ObservableProperty] private bool _locked;
    [ObservableProperty] private string? _toast;
    [ObservableProperty] private bool _dozePromptVisible;
    private bool _dozeMarked;

    public Settings Settings => _store.Settings;
    public double BookTotalSec => _timeline?.Total ?? 0;
    public double FileDurationSec => CurrentBook is { } b && FileIndex < b.Files.Count ? b.Files[FileIndex].DurationSec : 0;
    public IReadOnlyList<Book> Books => _store.Library.Books;

    public PlaybackController(BassPlayer player, AppStore store)
    {
        _player = player;
        _store = store;
        _player.Ended += OnFileEnded;
        _tick.Tick += (_, _) => Tick();
        _level.Tick += (_, _) => LevelTick();
        _tick.Start();
    }

    public void LoadBook(Book book, bool play)
    {
        if (CurrentBook is { } prev && prev != book)
        {
            SavePosition();
            History.Record(book, HistoryKind.BookSwitch, book.FileIndex, book.PositionSec, DateTime.UtcNow);
        }
        _player.Pause();
        IsPlaying = false;
        CurrentBook = book;
        _timeline = new BookTimeline(book);
        _store.Settings.LastBookId = book.Id;
        Speed = book.Speed ?? _store.Settings.DefaultSpeed;
        _player.Mono = _store.Settings.Mono;
        _player.Balance = _store.Settings.Balance;
        _player.BoostDb = _store.Settings.BoostDb;
        _player.PitchSemitones = book.PitchSemitones;
        _player.SetEq(book.EqBands ?? _store.Settings.DefaultEqBands);
        if (book.Files.Count == 0 || book.Missing) { ShowToast("The files for this book are missing."); return; }
        if (!OpenFile(book.FileIndex, book.PositionSec)) return;
        if (play) Play();
    }

    private bool OpenFile(int fileIndex, double posSec)
    {
        var book = CurrentBook!;
        fileIndex = Math.Clamp(fileIndex, 0, book.Files.Count - 1);
        if (!_player.Open(book.Files[fileIndex].Path))
        {
            IsPlaying = false;
            ShowToast("Cannot open " + Path.GetFileName(book.Files[fileIndex].Path));
            return false;
        }
        _player.Speed = Speed;
        var start = Math.Max(posSec, book.SkipStartSec);
        _player.PositionSec = start;
        FileIndex = fileIndex;
        book.FileIndex = fileIndex;
        book.PositionSec = start;
        _silence.Reset();
        Refresh();
        return true;
    }

    public void TogglePlay() { if (IsPlaying) Pause(); else Play(); }

    public void Play()
    {
        if (CurrentBook is null || !_player.IsOpen) return;
        if (_pausedAtUtc is { } pausedAt && _store.Settings.AutoRewind)
        {
            var back = AutoRewind.SecondsFor(DateTime.UtcNow - pausedAt);
            if (back > 0) MoveBy(-back, record: false);
        }
        _pausedAtUtc = null;
        if (CurrentBook.DozePositionSec is not null) DozePromptVisible = true;
        if (_store.Settings.SleepSchedule.Enabled && !_sleep.IsRunning &&
            _store.Settings.SleepSchedule.Contains(TimeOnly.FromDateTime(DateTime.Now)))
            StartSleep(_store.Settings.SleepDefaultMin);
        _player.Play();
        IsPlaying = true;
        _lastStatUtc = DateTime.UtcNow;
        if (CurrentBook.State == BookState.New) CurrentBook.State = BookState.Started;
        CurrentBook.LastPlayedUtc = DateTime.UtcNow;
        if (_store.Settings.SkipSilence) _level.Start();
        _store.SaveSoon();
    }

    public void Pause()
    {
        if (!IsPlaying) return;
        AccumulateStats();
        _player.Pause();
        _level.Stop();
        IsPlaying = false;
        _pausedAtUtc = DateTime.UtcNow;
        SavePosition();
        _store.SaveNow();
    }

    public void SeekFile(int fileIndex, double posSec, HistoryKind kind = HistoryKind.Seek)
    {
        if (CurrentBook is null) return;
        History.Record(CurrentBook, kind, FileIndex, PositionSec, DateTime.UtcNow);
        var wasPlaying = IsPlaying;
        if (fileIndex != FileIndex) { if (!OpenFile(fileIndex, posSec)) return; if (wasPlaying) _player.Play(); }
        else _player.PositionSec = posSec;
        Refresh();
    }

    public void SeekBook(double bookSec)
    {
        if (_timeline is null) return;
        var (f, p) = _timeline.FromBookSec(bookSec);
        SeekFile(f, p);
    }

    public void Jump(int seconds) => MoveBy(seconds, record: Math.Abs(seconds) > _store.Settings.SmallRewindSec);

    private void MoveBy(int seconds, bool record)
    {
        if (_timeline is null || CurrentBook is null) return;
        var (f, p) = _timeline.Offset(FileIndex, _player.PositionSec, seconds);
        if (record) History.Record(CurrentBook, HistoryKind.Jump, FileIndex, PositionSec, DateTime.UtcNow);
        var wasPlaying = IsPlaying;
        if (f != FileIndex) { if (!OpenFile(f, p)) return; if (wasPlaying) _player.Play(); }
        else _player.PositionSec = p;
        Refresh();
    }

    public void PrevFile()
    {
        if (CurrentBook is null) return;
        var target = _player.PositionSec > 3 ? FileIndex : Math.Max(0, FileIndex - 1);
        SeekFile(target, 0, HistoryKind.Jump);
    }

    public void NextFile()
    {
        if (CurrentBook is null) return;
        if (FileIndex + 1 >= CurrentBook.Files.Count) { OnBookEnded(); return; }
        SeekFile(FileIndex + 1, 0, HistoryKind.Jump);
    }

    public void SetSpeed(double speed)
    {
        Speed = Math.Round(Math.Clamp(speed, 0.5, 3.0), 2);
        if (CurrentBook is { } b) b.Speed = Speed;
        _player.Speed = Speed;
        _store.SaveSoon();
    }
    public void AdjustSpeed(double delta) => SetSpeed(Speed + delta);
    public void SetPitch(int semitones) { if (CurrentBook is { } b) b.PitchSemitones = semitones; _player.PitchSemitones = semitones; _store.SaveSoon(); }
    public void SetEq(double[] bands) { if (CurrentBook is { } b) b.EqBands = (double[])bands.Clone(); _player.SetEq(bands); _store.SaveSoon(); }
    public void SetBoost(double db) { _store.Settings.BoostDb = db; _player.BoostDb = db; _store.SaveSoon(); }

    public void ApplySoundSettings()
    {
        _player.Balance = _store.Settings.Balance; _player.BoostDb = _store.Settings.BoostDb;
        if (_store.Settings.SkipSilence && IsPlaying) _level.Start(); else { _level.Stop(); }
        if (_player.Mono != _store.Settings.Mono && CurrentBook is not null && _player.IsOpen)
        {
            _player.Mono = _store.Settings.Mono;
            var wasPlaying = IsPlaying; var pos = _player.PositionSec;
            if (OpenFile(FileIndex, pos) && wasPlaying) _player.Play();
        }
    }

    public void StartSleep(int minutes) { _sleep.Fadeout = _store.Settings.SleepFadeout; _sleep.Start(TimeSpan.FromMinutes(minutes), DateTime.UtcNow); SleepActive = true; _dozeMarked = false; }
    public void StartSleepUntilChapterEnd() { _sleep.StartUntilChapterEnd(); SleepActive = true; }
    public void StopSleep() { _sleep.Stop(); SleepActive = false; _player.Volume = 1.0; SleepRemaining = TimeSpan.Zero; }
    public void ResetSleep() { _sleep.Reset(DateTime.UtcNow); _player.Volume = 1.0; _dozeMarked = false; }

    // Answer to the wake prompt: go back to where the fade began, or keep going.
    public void RewindToDoze()
    {
        if (CurrentBook is { DozeFileIndex: { } f, DozePositionSec: { } p }) SeekFile(f, p, HistoryKind.Jump);
        ClearDoze();
    }
    public void ClearDoze() { if (CurrentBook is { } b) { b.DozeFileIndex = null; b.DozePositionSec = null; } DozePromptVisible = false; _store.SaveSoon(); }

    // Speed presets cycle from the player bar: 1x, 1.25x, 1.5x, 1.75x, 2x, then round again.
    public void CycleSpeed()
    {
        var presets = Settings.SpeedPresets;
        var next = presets.FirstOrDefault(s => s > Speed + 0.001);
        SetSpeed(next > 0 ? next : presets[0]);
    }

    public void PauseForDeviceLoss()
    {
        if (!IsPlaying) return;
        Pause();
        ShowToast("Paused: the audio device went away");
    }

    public void AddBookmark(string note)
    {
        if (CurrentBook is null) return;
        CurrentBook.Bookmarks.Insert(0, new Bookmark(FileIndex, PositionSec, note, DateTime.UtcNow));
        ShowToast("Bookmark added");
        _store.SaveSoon();
    }
    public void JumpToBookmark(Bookmark b) => SeekFile(b.FileIndex, b.PositionSec, HistoryKind.Jump);
    public void UndoTo(HistoryEntry e) => SeekFile(e.FileIndex, e.PositionSec, HistoryKind.Jump);

    public void MarkState(Book book, BookState state)
    {
        book.State = state;
        if (state == BookState.New) { book.FileIndex = 0; book.PositionSec = 0; if (book == CurrentBook && _player.IsOpen) SeekFile(0, 0, HistoryKind.Jump); }
        _store.SaveSoon();
    }

    // Reset first so the same text twice still raises a change notification.
    private void ShowToast(string text) { Toast = null; Toast = text; }

    private void Tick()
    {
        if (CurrentBook is null) return;
        if (IsPlaying)
        {
            AccumulateStats();
            var pos = _player.PositionSec;
            if (CurrentBook.SkipEndSec > 0 && pos >= FileDurationSec - CurrentBook.SkipEndSec) { OnFileEnded(); return; }
            if (DateTime.UtcNow - _lastSaveUtc > TimeSpan.FromSeconds(5)) { SavePosition(); _store.SaveSoon(); _lastSaveUtc = DateTime.UtcNow; }
            if (_sleep.IsRunning)
            {
                var now = DateTime.UtcNow;
                SleepRemaining = _sleep.Remaining(now);
                var volume = _sleep.VolumeFor(now);
                if (volume < 1.0 && !_dozeMarked) { CurrentBook.DozeFileIndex = FileIndex; CurrentBook.DozePositionSec = pos; _dozeMarked = true; }
                _player.Volume = volume;
                if (_sleep.Expired(now)) { StopSleep(); Pause(); }
            }
        }
        Refresh();
    }

    private void LevelTick()
    {
        if (!IsPlaying) return;
        // Once a pause has lasted long enough, hop forward in small steps until sound returns.
        if (_silence.Sample(_player.Level()) && _player.PositionSec + SilenceHopSec < FileDurationSec)
            _player.PositionSec += SilenceHopSec;
    }

    private void Refresh()
    {
        PositionSec = _player.PositionSec;
        BookPositionSec = _timeline?.ToBookSec(FileIndex, PositionSec) ?? 0;
        var chapter = _timeline?.ChapterAt(FileIndex, PositionSec);
        if (chapter != CurrentChapter)
        {
            if (_sleep.IsRunning && _sleep.UntilChapterEnd && CurrentChapter is not null && IsPlaying) { StopSleep(); Pause(); }
            CurrentChapter = chapter;
        }
        OnPropertyChanged(nameof(BookTotalSec));
        OnPropertyChanged(nameof(FileDurationSec));
    }

    private void SavePosition()
    {
        if (CurrentBook is null || !_player.IsOpen) return;
        CurrentBook.FileIndex = FileIndex;
        CurrentBook.PositionSec = _player.PositionSec;
    }

    private void AccumulateStats()
    {
        if (CurrentBook is null) return;
        var now = DateTime.UtcNow;
        var sec = (long)(now - _lastStatUtc).TotalSeconds;
        if (sec <= 0) return;
        _lastStatUtc = now;
        CurrentBook.ListenedSec += sec;
        var key = now.ToLocalTime().ToString("yyyy-MM");
        var month = _store.Stats.Months.FirstOrDefault(m => m.Month == key);
        if (month is null) _store.Stats.Months.Add(month = new MonthStats { Month = key });
        month.ListenedSec += sec;
    }

    private void OnFileEnded()
    {
        if (CurrentBook is null) return;
        if (FileIndex + 1 < CurrentBook.Files.Count)
        {
            var wasPlaying = IsPlaying;
            if (OpenFile(FileIndex + 1, 0) && wasPlaying) _player.Play();
            return;
        }
        OnBookEnded();
    }

    private void OnBookEnded()
    {
        if (CurrentBook is null) return;
        var finished = CurrentBook;
        AccumulateStats();
        _player.Pause(); IsPlaying = false; _level.Stop();
        finished.State = BookState.Finished;
        finished.FileIndex = 0; finished.PositionSec = 0;
        _store.Settings.Queue.RemoveAll(id => id.Equals(finished.Id, StringComparison.OrdinalIgnoreCase));
        var action = _store.Settings.EndOfBookAction;
        var nextId = _store.Settings.Queue.FirstOrDefault();
        var next = nextId is null ? null : Books.FirstOrDefault(b => b.Id.Equals(nextId, StringComparison.OrdinalIgnoreCase));
        if (action != EndOfBookAction.Stop && next is not null)
        {
            _store.Settings.Queue.RemoveAt(0);
            LoadBook(next, play: action == EndOfBookAction.NextBookPlay);
        }
        else ShowToast("The book is finished.");
        _store.SaveNow();
    }

    public void Shutdown()
    {
        if (IsPlaying) AccumulateStats();
        SavePosition();
        _tick.Stop(); _level.Stop();
        _store.SaveNow();
        _player.Dispose();
    }
}
