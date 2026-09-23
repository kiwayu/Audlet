using Audlet.Player;

namespace Audlet.Tests;

public class SilenceSkipperTests
{
    [Fact]
    public void EntersFastModeAfter400msOfSilenceAndLeavesOnSound()
    {
        var s = new SilenceSkipper();
        for (var i = 0; i < 7; i++) Assert.False(s.Sample(0));   // 350 ms
        Assert.True(s.Sample(0));                                  // 400 ms
        Assert.True(s.Sample(0));
        Assert.False(s.Sample(5000));
        Assert.False(s.Sample(0));
    }

    [Fact]
    public void ResetClearsCounter()
    {
        var s = new SilenceSkipper();
        for (var i = 0; i < 7; i++) s.Sample(0);
        s.Reset();
        Assert.False(s.Sample(0));
    }
}
