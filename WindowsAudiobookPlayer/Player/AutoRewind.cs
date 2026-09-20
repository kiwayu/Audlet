namespace WindowsAudiobookPlayer.Player;

public static class AutoRewind
{
    public static int SecondsFor(TimeSpan pause) => pause.TotalSeconds switch
    {
        < 10 => 0,
        < 120 => 5,
        < 600 => 15,
        _ => 30,
    };
}
