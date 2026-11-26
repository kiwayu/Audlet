namespace Audlet.Player;

// Detects sustained silence from periodic level samples; the controller hops forward while Fast is true.
public sealed class SilenceSkipper
{
    public int Threshold { get; init; } = 800;
    public int SilenceMs { get; init; } = 400;
    public int IntervalMs { get; init; } = 50;

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
