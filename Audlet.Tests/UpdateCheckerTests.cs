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
    public void ReadsInstallerSizeAndChecksumAsset()
    {
        var json = """{"tag_name":"v2.0.0","html_url":"https://github.com/kiwayu/Audlet/releases/tag/v2.0.0","assets":[{"name":"Audlet-Setup-2.0.0.exe","size":55000000,"browser_download_url":"https://github.com/kiwayu/Audlet/releases/download/v2.0.0/Audlet-Setup-2.0.0.exe"},{"name":"Audlet-Setup-2.0.0.exe.sha256","size":80,"browser_download_url":"https://github.com/kiwayu/Audlet/releases/download/v2.0.0/Audlet-Setup-2.0.0.exe.sha256"}]}""";
        var info = UpdateChecker.Parse(json);
        Assert.NotNull(info);
        Assert.Equal(55000000, info.InstallerSize);
        Assert.EndsWith(".sha256", info.ShaUrl);
    }

    [Fact]
    public void OnlyAcceptsReleaseLinksOnGitHub()
    {
        var json = """{"tag_name":"v9.0.0","html_url":"https://github.com/kiwayu/Audlet/releases/tag/v9.0.0","assets":[{"name":"Audlet-Setup-9.0.0.exe","browser_download_url":"file:///C:/evil.exe"}]}""";
        var info = UpdateChecker.Parse(json);
        Assert.NotNull(info);
        Assert.Null(info.InstallerUrl);
        Assert.Null(UpdateChecker.Parse("""{"tag_name":"v9.0.0","html_url":"https://example.com/x","assets":[]}"""));
    }

    [Fact]
    public void RejectsJunkTags() => Assert.False(UpdateChecker.TryParseVersion("latest", out var v));

    [Fact]
    public void ParsesReleaseAndFindsInstaller()
    {
        var json = """{"tag_name":"v1.1.0","html_url":"https://github.com/kiwayu/Audlet/releases/tag/v1.1.0","assets":[{"name":"Audlet-Setup-1.1.0.exe","browser_download_url":"https://github.com/kiwayu/Audlet/releases/download/v1.1.0/Audlet-Setup-1.1.0.exe"}]}""";
        var info = UpdateChecker.Parse(json)!;
        Assert.Equal(new Version(1, 1, 0), info.Version);
        Assert.EndsWith("/v1.1.0", info.PageUrl);
        Assert.Equal("https://github.com/kiwayu/Audlet/releases/download/v1.1.0/Audlet-Setup-1.1.0.exe", info.InstallerUrl);
    }

    [Fact]
    public void CurrentVersionIsThreePart() => Assert.Equal(3, UpdateChecker.Current.ToString().Split('.').Length);
}
