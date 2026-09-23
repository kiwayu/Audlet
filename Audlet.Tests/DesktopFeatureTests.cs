using Audlet.Audio;
using Audlet.Data;
using Audlet.Player;

namespace Audlet.Tests;

public class DesktopFeatureTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "wap-desk-" + Guid.NewGuid().ToString("N"));
    public DesktopFeatureTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void SpeedCyclesThroughPresetsAndWraps()
    {
        var store = new AppStore(_dir); store.Load();
        using var player = new BassPlayer();
        var c = new PlaybackController(player, store);
        var seen = new List<double>();
        for (var i = 0; i < 6; i++) { c.CycleSpeed(); seen.Add(c.Speed); }
        Assert.Equal([1.25, 1.5, 1.75, 2.0, 1.0, 1.25], seen);
        c.SetSpeed(1.3);
        c.CycleSpeed();
        Assert.Equal(1.5, c.Speed);
    }

    [Fact]
    public void DeviceLossMeansAKnownDeviceVanished()
    {
        var before = new HashSet<string> { "Speakers", "Headphones" };
        Assert.True(DeviceWatcher.Lost(before, new HashSet<string> { "Speakers" }));
        Assert.False(DeviceWatcher.Lost(before, new HashSet<string> { "Speakers", "Headphones", "Monitor" }));
        Assert.False(DeviceWatcher.Lost(before, before));
    }
}
