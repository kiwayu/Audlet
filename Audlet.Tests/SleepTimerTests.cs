using Audlet.Models;
using Audlet.Player;

namespace Audlet.Tests;

public class SleepTimerTests
{
    private static readonly DateTime T0 = new(2026, 9, 20, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CountsDownAndExpires()
    {
        var t = new SleepTimer { Fadeout = true };
        t.Start(TimeSpan.FromMinutes(5), T0);
        Assert.True(t.IsRunning);
        Assert.Equal(TimeSpan.FromMinutes(3), t.Remaining(T0.AddMinutes(2)));
        Assert.Equal(1.0, t.VolumeFor(T0.AddMinutes(3)));
        Assert.Equal(0.5, t.VolumeFor(T0.AddMinutes(4).AddSeconds(30)), 3);
        Assert.False(t.Expired(T0.AddMinutes(4).AddSeconds(59)));
        Assert.True(t.Expired(T0.AddMinutes(5)));
    }

    [Fact]
    public void ResetRestartsTheSameDuration()
    {
        var t = new SleepTimer();
        t.Start(TimeSpan.FromMinutes(10), T0);
        t.Reset(T0.AddMinutes(8));
        Assert.Equal(TimeSpan.FromMinutes(10), t.Remaining(T0.AddMinutes(8)));
    }

    [Fact]
    public void NoFadeoutKeepsFullVolume()
    {
        var t = new SleepTimer { Fadeout = false };
        t.Start(TimeSpan.FromMinutes(1), T0);
        Assert.Equal(1.0, t.VolumeFor(T0.AddSeconds(59)));
    }

    [Theory]
    [InlineData(22, 0, 6, 0, 23, 30, true)]
    [InlineData(22, 0, 6, 0, 3, 0, true)]
    [InlineData(22, 0, 6, 0, 12, 0, false)]
    [InlineData(9, 0, 17, 0, 12, 0, true)]
    [InlineData(9, 0, 17, 0, 18, 0, false)]
    public void ScheduleWindowHandlesMidnight(int fh, int fm, int th, int tm, int h, int m, bool expected)
    {
        var s = new SleepSchedule { Enabled = true, FromTime = new TimeOnly(fh, fm), ToTime = new TimeOnly(th, tm) };
        Assert.Equal(expected, s.Contains(new TimeOnly(h, m)));
    }
}
