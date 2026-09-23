using Audlet.Player;

namespace Audlet.Tests;

public class AutoRewindTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(9, 0)]
    [InlineData(10, 5)]
    [InlineData(119, 5)]
    [InlineData(120, 15)]
    [InlineData(599, 15)]
    [InlineData(600, 30)]
    [InlineData(86400, 30)]
    public void Tiers(int pauseSeconds, int expected) =>
        Assert.Equal(expected, AutoRewind.SecondsFor(TimeSpan.FromSeconds(pauseSeconds)));
}
