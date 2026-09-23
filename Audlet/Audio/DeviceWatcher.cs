using System.Windows.Threading;
using ManagedBass;

namespace Audlet.Audio;

// Polls the audio device list and reports when an enabled output device disappears, such as headphones being unplugged.
public sealed class DeviceWatcher
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private HashSet<string> _known = [];

    public event Action? DeviceLost;

    public DeviceWatcher()
    {
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
