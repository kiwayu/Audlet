namespace WindowsAudiobookPlayer.Tests;

public class SmokeTests
{
    [Fact]
    public void AppAssemblyLoads() => Assert.NotNull(typeof(WindowsAudiobookPlayer.App).Assembly);
}
