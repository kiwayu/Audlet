namespace WindowsAudiobookPlayer.Player;

// ponytail: raises tempo during silence instead of cutting it; replace with a DSP that drops silent frames if the speed-up is audible
public sealed class SilenceSkipper
{
    public int Threshold { get; init; } = 800;
    public int SilenceMs { get; init; } = 400;
    public int IntervalMs { get; init; } = 50;
    public double FastSpeed { get; init; } = 2.0;

    private int _silentMs;
    public bool Fast { get; private set; }

    public bool Sample(int level)
    {
        if (level < Threshold)
        {
            _silentMs += IntervalMs;
            Fast = _silentMs >= SilenceMs;
        }
        else
        {
            _silentMs = 0;
            Fast = false;
        }
        return Fast;
    }

    public void Reset() { _silentMs = 0; Fast = false; }
}
