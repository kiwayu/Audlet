using Audlet.Player;

namespace Audlet.Audio;

// Polls the audio device list and reports when an enabled output device disappears, such as headphones being unplugged.
public sealed class DeviceWatcher
{
    private readonly IAudioPlayer _audio;
    private readonly ITicker _timer;
    private HashSet<string> _known = [];

    public event Action? DeviceLost;

    public DeviceWatcher(IAudioPlayer audio, Func<TimeSpan, ITicker> timers)
    {
        _audio = audio;
        _timer = timers(TimeSpan.FromSeconds(2));
        _known = audio.OutputDevices();
        _timer.Tick += (o, a) => Check();
        _timer.Start();
    }

    public static bool Lost(IReadOnlySet<string> before, IReadOnlySet<string> after) => before.Any(d => !after.Contains(d));

    private void Check()
    {
        var now = _audio.OutputDevices();
        if (Lost(_known, now)) DeviceLost?.Invoke();
        _known = now;
    }
}
