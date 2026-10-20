using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Audlet.Data;

public sealed record UpdateInfo(Version Version, string PageUrl, string? InstallerUrl);

// Asks GitHub for the latest release and reports it when it is newer than the running build.
public static class UpdateChecker
{
    private const string LatestUrl = "https://api.github.com/repos/kiwayu/Audlet/releases/latest";
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    static UpdateChecker() => Http.DefaultRequestHeaders.UserAgent.ParseAdd("Audlet/" + Current + " (update check)");

    public static Version Current { get; } = Assembly.GetExecutingAssembly().GetName().Version is { } v ? new Version(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build) : new Version(0, 0, 0);

    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await Http.GetStringAsync(LatestUrl, ct);
            var latest = Parse(json);
            return latest is not null && latest.Version > Current ? latest : null;
        }
        catch (Exception) { return null; }   // no network, rate limit, or no releases yet: stay quiet
    }

    public static UpdateInfo? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (!root.TryGetProperty("tag_name", out var tag) || !TryParseVersion(tag.GetString(), out var version)) return null;
        // Both links are opened with the shell, so only release links on GitHub itself are accepted.
        var page = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? "" : "";
        if (!IsReleaseLink(page)) return null;
        string? installer = null;
        if (root.TryGetProperty("assets", out var assets))
            foreach (var a in assets.EnumerateArray())
                if (a.TryGetProperty("name", out var n) && (n.GetString() ?? "").EndsWith("Setup" + "-" + version + ".exe", StringComparison.OrdinalIgnoreCase)
                    || a.TryGetProperty("name", out n) && (n.GetString() ?? "").StartsWith("Audlet-Setup", StringComparison.OrdinalIgnoreCase))
                    installer = a.TryGetProperty("browser_download_url", out var u) && u.GetString() is { } link && IsReleaseLink(link) ? link : null;
        return new UpdateInfo(version, page, installer);
    }

    private static bool IsReleaseLink(string url) => url.StartsWith("https://github.com/kiwayu/Audlet/", StringComparison.OrdinalIgnoreCase);

    public static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag)) return false;
        var text = tag.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(text, out var parsed)) return false;
        version = new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build));
        return true;
    }
}
