using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Audlet.Data;

public sealed record UpdateInfo(Version Version, string PageUrl, string? InstallerUrl, long InstallerSize = 0, string? ShaUrl = null);

// Asks GitHub for the latest release and reports it when it is newer than the running build.
public static class UpdateChecker
{
    private const string LatestUrl = "https://api.github.com/repos/kiwayu/Audlet/releases/latest";
    public const long MaxInstallerBytes = 300L * 1024 * 1024;

    public static Version Current { get; } = Assembly.GetExecutingAssembly().GetName().Version is { } v ? new Version(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build) : new Version(0, 0, 0);

    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await Net.Http.GetStringAsync(LatestUrl, ct);
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
        string? installer = null, sha = null;
        long size = 0;
        if (root.TryGetProperty("assets", out var assets))
            foreach (var a in assets.EnumerateArray())
            {
                var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var link = a.TryGetProperty("browser_download_url", out var u) && u.GetString() is { } l && IsReleaseLink(l) ? l : null;
                if (link is null) continue;
                if (name.StartsWith("Audlet-Setup", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    installer = link;
                    size = a.TryGetProperty("size", out var sz) && sz.TryGetInt64(out var bytes) ? bytes : 0;
                }
                else if (name.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)) sha = link;
            }
        return new UpdateInfo(version, page, installer, size, sha);
    }

    // Fetches the installer to the temp folder and checks it against the size GitHub reported and,
    // when the release carries one, the published SHA-256. Anything that does not match is deleted.
    public static async Task<string> DownloadInstallerAsync(UpdateInfo info, CancellationToken ct = default)
    {
        if (info.InstallerUrl is null) throw new InvalidOperationException("This release has no installer.");
        var target = Path.Combine(Path.GetTempPath(), Path.GetFileName(new Uri(info.InstallerUrl).AbsolutePath));
        using var response = await Net.Http.GetAsync(info.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        long total = 0;
        await using (var input = await response.Content.ReadAsStreamAsync(ct))
        await using (var output = File.Create(target))
        {
            var buffer = new byte[1 << 16];
            int read;
            while ((read = await input.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (total > MaxInstallerBytes) throw new InvalidDataException("The download is larger than any Audlet installer.");
                hash.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), ct);
            }
        }
        try
        {
            if (info.InstallerSize > 0 && total != info.InstallerSize) throw new InvalidDataException("The download does not match the size GitHub reported.");
            if (info.ShaUrl is not null)
            {
                var expected = Regex.Match(await Net.Http.GetStringAsync(info.ShaUrl, ct), "[0-9a-fA-F]{64}").Value;
                var actual = Convert.ToHexString(hash.GetHashAndReset());
                if (expected.Length == 0 || !actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The download does not match its published checksum.");
            }
        }
        catch (Exception) { File.Delete(target); throw; }
        return target;
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
