using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Audlet.Library;

public sealed record CoverCandidate(string Source, string Title, string Author, string? Description, string ImageUrl, string? DetailsUrl);
public sealed record CoverSearchResult(IReadOnlyList<CoverCandidate> Hits, IReadOnlyList<string> Warnings);

// Looks up covers and blurbs on Open Library and Google Books. Parsing is separate from fetching so it can be tested offline.
public static class CoverSearch
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };

    static CoverSearch() => Http.DefaultRequestHeaders.UserAgent.ParseAdd("Audlet/1.0 (audiobook player)");

    public static async Task<CoverSearchResult> SearchAsync(string title, string author, CancellationToken ct = default)
    {
        var results = new List<CoverCandidate>();
        var warnings = new List<string>();
        // Audiobook stores first: their art is the square cover a listener expects, and their blurbs are written for the audio edition.
        var tasks = new (string Name, Task<IReadOnlyList<CoverCandidate>> Task)[]
        {
            ("Audible", SearchAudibleAsync(title, author, ct)),
            ("Apple Books", SearchAppleAsync(title, author, ct)),
            ("Open Library", SearchOpenLibraryAsync(title, author, ct)),
            ("Google Books", SearchGoogleAsync(title, author, ct)),
        };
        foreach (var (name, task) in tasks)
        {
            try { results.AddRange(await task); }
            catch (HttpRequestException ex) when ((int?)ex.StatusCode == 429) { warnings.Add(name + " is rate limiting this connection; try again later"); }
            catch (Exception ex) { warnings.Add(name + " failed: " + ex.Message); }
        }
        return new CoverSearchResult(results, warnings);
    }

    public static async Task<byte[]> DownloadAsync(string url, CancellationToken ct = default) => await Http.GetByteArrayAsync(url, ct);

    private static async Task<IReadOnlyList<CoverCandidate>> SearchAudibleAsync(string title, string author, CancellationToken ct)
    {
        var url = "https://api.audible.com/1.0/catalog/products?num_results=8&response_groups=media,contributors,product_desc&image_sizes=500&title=" + Uri.EscapeDataString(title)
                  + (string.IsNullOrWhiteSpace(author) ? "" : "&author=" + Uri.EscapeDataString(author));
        return ParseAudible(await Http.GetStringAsync(url, ct));
    }

    private static async Task<IReadOnlyList<CoverCandidate>> SearchAppleAsync(string title, string author, CancellationToken ct)
    {
        var url = "https://itunes.apple.com/search?media=audiobook&entity=audiobook&limit=8&term=" + Uri.EscapeDataString((title + " " + author).Trim());
        return ParseApple(await Http.GetStringAsync(url, ct));
    }

    public static IReadOnlyList<CoverCandidate> ParseAudible(string json)
    {
        var list = new List<CoverCandidate>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("products", out var products)) return list;
        foreach (var p in products.EnumerateArray())
        {
            if (!p.TryGetProperty("product_images", out var images) || images.ValueKind != JsonValueKind.Object) continue;
            string? img = null;
            foreach (var prop in images.EnumerateObject()) img = prop.Value.GetString();   // the largest requested size
            if (string.IsNullOrEmpty(img)) continue;
            var title = p.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var author = p.TryGetProperty("authors", out var a) && a.ValueKind == JsonValueKind.Array
                ? string.Join(", ", a.EnumerateArray().Select(x => x.TryGetProperty("name", out var n) ? n.GetString() : null).Where(n => n is not null)) : "";
            var desc = p.TryGetProperty("publisher_summary", out var ps) ? ps.GetString() : p.TryGetProperty("merchandising_summary", out var ms) ? ms.GetString() : null;
            list.Add(new CoverCandidate("Audible", title, author, StripHtml(desc), img, null));
        }
        return list;
    }

    public static IReadOnlyList<CoverCandidate> ParseApple(string json)
    {
        var list = new List<CoverCandidate>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results)) return list;
        foreach (var r in results.EnumerateArray())
        {
            if (!r.TryGetProperty("artworkUrl100", out var art) || art.GetString() is not { Length: > 0 } small) continue;
            var title = r.TryGetProperty("collectionName", out var t) ? t.GetString() ?? "" : "";
            var author = r.TryGetProperty("artistName", out var a) ? a.GetString() ?? "" : "";
            var desc = r.TryGetProperty("description", out var d) ? d.GetString() : null;
            list.Add(new CoverCandidate("Apple Books", title, author, StripHtml(desc), small.Replace("100x100bb", "600x600bb"), null));
        }
        return list;
    }

    private static readonly Regex Tags = new("<[^>]+>");
    public static string? StripHtml(string? html) =>
        html is null ? null : System.Net.WebUtility.HtmlDecode(Tags.Replace(html, " ")).Replace("  ", " ").Trim();

    private static async Task<IReadOnlyList<CoverCandidate>> SearchOpenLibraryAsync(string title, string author, CancellationToken ct)
    {
        var url = "https://openlibrary.org/search.json?limit=8&fields=key,title,author_name,cover_i&q=" + Uri.EscapeDataString((title + " " + author).Trim());
        var list = ParseOpenLibrary(await Http.GetStringAsync(url, ct));
        // Descriptions live on the work record; fetch them for the first few hits only.
        var withText = new List<CoverCandidate>();
        foreach (var c in list.Take(4))
        {
            string? text = null;
            if (c.DetailsUrl is not null)
                try { text = ParseOpenLibraryDescription(await Http.GetStringAsync(c.DetailsUrl, ct)); } catch (Exception) { }
            withText.Add(c with { Description = text });
        }
        withText.AddRange(list.Skip(4));
        return withText;
    }

    private static async Task<IReadOnlyList<CoverCandidate>> SearchGoogleAsync(string title, string author, CancellationToken ct)
    {
        var q = "intitle:" + title + (string.IsNullOrWhiteSpace(author) ? "" : " inauthor:" + author);
        var url = "https://www.googleapis.com/books/v1/volumes?maxResults=8&printType=books&q=" + Uri.EscapeDataString(q);
        return ParseGoogle(await Http.GetStringAsync(url, ct));
    }

    public static IReadOnlyList<CoverCandidate> ParseOpenLibrary(string json)
    {
        var list = new List<CoverCandidate>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("docs", out var docs)) return list;
        foreach (var d in docs.EnumerateArray())
        {
            if (!d.TryGetProperty("cover_i", out var cover) || cover.ValueKind != JsonValueKind.Number) continue;
            var title = d.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var author = d.TryGetProperty("author_name", out var a) && a.ValueKind == JsonValueKind.Array
                ? string.Join(", ", a.EnumerateArray().Select(x => x.GetString())) : "";
            var key = d.TryGetProperty("key", out var k) ? k.GetString() : null;
            list.Add(new CoverCandidate("Open Library", title, author, null,
                "https://covers.openlibrary.org/b/id/" + cover.GetInt64() + "-L.jpg",
                key is null ? null : "https://openlibrary.org" + key + ".json"));
        }
        return list;
    }

    public static string? ParseOpenLibraryDescription(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("description", out var d)) return null;
        return d.ValueKind == JsonValueKind.String ? d.GetString()
             : d.ValueKind == JsonValueKind.Object && d.TryGetProperty("value", out var v) ? v.GetString() : null;
    }

    public static IReadOnlyList<CoverCandidate> ParseGoogle(string json)
    {
        var list = new List<CoverCandidate>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("items", out var items)) return list;
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("volumeInfo", out var v)) continue;
            if (!v.TryGetProperty("imageLinks", out var links) || !links.TryGetProperty("thumbnail", out var thumb)) continue;
            var img = (thumb.GetString() ?? "").Replace("http://", "https://").Replace("zoom=1", "zoom=2").Replace("&edge=curl", "");
            var title = v.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
            var author = v.TryGetProperty("authors", out var a) && a.ValueKind == JsonValueKind.Array
                ? string.Join(", ", a.EnumerateArray().Select(x => x.GetString())) : "";
            var desc = v.TryGetProperty("description", out var d) ? d.GetString() : null;
            list.Add(new CoverCandidate("Google Books", title, author, desc, img, null));
        }
        return list;
    }
}
