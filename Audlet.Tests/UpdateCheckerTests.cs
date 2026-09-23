using Audlet.Data;

namespace Audlet.Tests;

public class UpdateCheckerTests
{
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2", "1.2.0")]
    [InlineData("V2.0.0", "2.0.0")]
    public void ParsesTags(string tag, string expected)
    {
        Assert.True(UpdateChecker.TryParseVersion(tag, out var v));
        Assert.Equal(new Version(expected), v);
    }

    [Fact]
    public void RejectsJunkTags() => Assert.False(UpdateChecker.TryParseVersion("latest", out var v));

    [Fact]
    public void ParsesReleaseAndFindsInstaller()
    {
        var json = """{"tag_name":"v1.1.0","html_url":"https://github.com/kiwayu/Audlet/releases/tag/v1.1.0","assets":[{"name":"Audlet-Setup-1.1.0.exe","browser_download_url":"https://example.test/Audlet-Setup-1.1.0.exe"}]}""";
        var info = UpdateChecker.Parse(json)!;
        Assert.Equal(new Version(1, 1, 0), info.Version);
        Assert.EndsWith("/v1.1.0", info.PageUrl);
        Assert.Equal("https://example.test/Audlet-Setup-1.1.0.exe", info.InstallerUrl);
    }

    [Fact]
    public void CurrentVersionIsThreePart() => Assert.Equal(3, UpdateChecker.Current.ToString().Split('.').Length);
}
