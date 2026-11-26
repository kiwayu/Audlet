namespace Audlet.Audio;

// The audio engine: one open file at a time, plus the list of output devices. The app's is BassPlayer.
public interface IAudioPlayer : IDisposable
{
    event Action? Ended;
    bool Mono { get; set; }
    bool IsOpen { get; }
    double PositionSec { get; set; }
    double Speed { get; set; }
    int PitchSemitones { get; set; }
    double BoostDb { get; set; }
    double Balance { get; set; }
    double Volume { get; set; }
    bool Open(string path);
    void Play();
    void Pause();
    void SetEq(double[] gainsDb);
    int Level();
    HashSet<string> OutputDevices();
}
