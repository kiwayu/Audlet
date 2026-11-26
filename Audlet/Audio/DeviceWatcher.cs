using ManagedBass;
using Audlet.Player;

namespace Audlet.Audio;

// Polls the audio device list and reports when an enabled output device disappears, such as headphones being unplugged.
public sealed class DeviceWatcher
{
    private readonly ITicker _timer;
    private HashSet<string> _known = [];

    public event Action? DeviceLost;

    public DeviceWatcher(Func<TimeSpan, ITicker> timers)
    {
        _timer = timers(TimeSpan.FromSeconds(2));
        _known = Snapshot();
        _timer.Tick += (o, a) => Check();
        _timer.Start();
    }

    public static bool Lost(IReadOnlySet<string> before, IReadOnlySet<string> after) => before.Any(d => !after.Contains(d));

    private void Check()
    {
        var now = Snapshot();
        if (Lost(_known, now)) DeviceLost?.Invoke();
        _known = now;
    }

    private static HashSet<string> Snapshot()
    {
        var set = new HashSet<string>();
        for (var i = 1; Bass.GetDeviceInfo(i, out var info); i++)
            if (info.IsEnabled) set.Add(info.Name);
        return set;
    }
}
