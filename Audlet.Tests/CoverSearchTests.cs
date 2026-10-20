using Audlet.Library;

namespace Audlet.Tests;

public class CoverSearchTests
{
    [Fact]
    public void ParsesOpenLibraryHitsWithCoversOnly()
    {
        var json = """{"docs":[{"key":"/works/OL1W","title":"Dune","author_name":["Frank Herbert"],"cover_i":12345},{"key":"/works/OL2W","title":"Dune (no cover)"}]}""";
        var hits = CoverSearch.ParseOpenLibrary(json);
        var h = Assert.Single(hits);
        Assert.Equal("Dune", h.Title);
        Assert.Equal("Frank Herbert", h.Author);
        Assert.Equal("https://covers.openlibrary.org/b/id/12345-L.jpg", h.ImageUrl);
        Assert.Equal("https://openlibrary.org/works/OL1W.json", h.DetailsUrl);
        Assert.Equal("Open Library", h.Source);
    }

    [Fact]
    public void ParsesOpenLibraryDescriptionInBothShapes()
    {
        Assert.Equal("A desert planet.", CoverSearch.ParseOpenLibraryDescription("""{"description":"A desert planet."}"""));
        Assert.Equal("Spice.", CoverSearch.ParseOpenLibraryDescription("""{"description":{"type":"/type/text","value":"Spice."}}"""));
        Assert.Null(CoverSearch.ParseOpenLibraryDescription("""{"title":"x"}"""));
    }

    [Fact]
    public void ParsesGoogleBooksAndUpgradesThumbnail()
    {
        var json = """{"items":[{"volumeInfo":{"title":"Dune","authors":["Frank Herbert"],"description":"Blurb","imageLinks":{"thumbnail":"http://books.google.com/books/content?id=1&zoom=1&edge=curl&source=gbs_api"}}},{"volumeInfo":{"title":"No image"}}]}""";
        var hits = CoverSearch.ParseGoogle(json);
        var h = Assert.Single(hits);
        Assert.Equal("Google Books", h.Source);
        Assert.Equal("Blurb", h.Description);
        Assert.StartsWith("https://", h.ImageUrl);
        Assert.Contains("zoom=2", h.ImageUrl);
        Assert.DoesNotContain("edge=curl", h.ImageUrl);
    }

    [Fact]
    public void EmptyResponsesGiveNoCandidates()
    {
        Assert.Empty(CoverSearch.ParseOpenLibrary("""{"numFound":0}"""));
        Assert.Empty(CoverSearch.ParseGoogle("""{"totalItems":0}"""));
    }

    [Theory]
    [InlineData("https://m.media-amazon.com/images/I/abc.jpg", true)]
    [InlineData("https://is1-ssl.mzstatic.com/image/thumb/x/500x500bb.jpg", true)]
    [InlineData("https://covers.openlibrary.org/b/id/123-L.jpg", true)]
    [InlineData("https://books.google.com/books/content?id=x", true)]
    [InlineData("http://m.media-amazon.com/images/I/abc.jpg", false)]
    [InlineData("https://evil.example/media-amazon.com/x.jpg", false)]
    [InlineData("https://media-amazon.com.evil.example/x.jpg", false)]
    public void OnlyKnownImageHostsAreFetched(string url, bool allowed) => Assert.Equal(allowed, CoverSearch.IsAllowedImageUrl(url));
}
