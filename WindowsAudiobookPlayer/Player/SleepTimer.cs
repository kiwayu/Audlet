using WindowsAudiobookPlayer.Models;

namespace WindowsAudiobookPlayer.Player;

public sealed class SleepTimer
{
    public static readonly TimeSpan FadeLength = TimeSpan.FromSeconds(60);

    private TimeSpan _duration;
    private DateTime _endUtc;

    public bool Fadeout { get; set; } = true;
    public bool IsRunning { get; private set; }
    public bool UntilChapterEnd { get; private set; }

    public void Start(TimeSpan duration, DateTime nowUtc)
    {
        _duration = duration; _endUtc = nowUtc + duration; IsRunning = true; UntilChapterEnd = false;
    }

    public void StartUntilChapterEnd() { IsRunning = true; UntilChapterEnd = true; }

    public void Reset(DateTime nowUtc) { if (IsRunning && !UntilChapterEnd) _endUtc = nowUtc + _duration; }

    public void Stop() { IsRunning = false; UntilChapterEnd = false; }

    public TimeSpan Remaining(DateTime nowUtc) =>
        !IsRunning || UntilChapterEnd ? TimeSpan.Zero : (_endUtc > nowUtc ? _endUtc - nowUtc : TimeSpan.Zero);

    public bool Expired(DateTime nowUtc) => IsRunning && !UntilChapterEnd && nowUtc >= _endUtc;

    public double VolumeFor(DateTime nowUtc)
    {
        if (!IsRunning || !Fadeout || UntilChapterEnd) return 1.0;
        var left = Remaining(nowUtc);
        return left >= FadeLength ? 1.0 : Math.Max(0, left / FadeLength);
    }
}

public static class SleepScheduleExtensions
{
    public static bool Contains(this SleepSchedule s, TimeOnly t) =>
        s.FromTime <= s.ToTime ? t >= s.FromTime && t < s.ToTime
                               : t >= s.FromTime || t < s.ToTime;
}
