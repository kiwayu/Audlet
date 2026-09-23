namespace Audlet.Tests;

public class SmokeTests
{
    [Fact]
    public void AppAssemblyLoads() => Assert.NotNull(typeof(Audlet.App).Assembly);
}
