using Audlet.Library;

namespace Audlet.Tests;

public class StoreCoverTests
{
    [Fact]
    public void ParsesAudibleProducts()
    {
        var json = """{"products":[{"title":"Dune","authors":[{"name":"Frank Herbert"}],"publisher_summary":"<p>NOW A <b>MAJOR</b> MOTION PICTURE</p>","product_images":{"500":"https://m.media-amazon.com/images/I/x._SL500_.jpg"}},{"title":"No art","authors":[]}]}""";
        var hits = CoverSearch.ParseAudible(json);
        var h = Assert.Single(hits);
        Assert.Equal("Audible", h.Source);
        Assert.Equal("Frank Herbert", h.Author);
        Assert.Equal("NOW A MAJOR MOTION PICTURE", h.Description);
        Assert.EndsWith("_SL500_.jpg", h.ImageUrl);
    }

    [Fact]
    public void ParsesAppleAudiobooksAndUpsizesArt()
    {
        var json = """{"results":[{"collectionName":"Dune","artistName":"Frank Herbert","artworkUrl100":"https://is1-ssl.mzstatic.com/image/thumb/a/b.jpg/100x100bb.jpg","description":"Set on the desert planet Arrakis, <i>Dune</i> is..."},{"collectionName":"No art"}]}""";
        var hits = CoverSearch.ParseApple(json);
        var h = Assert.Single(hits);
        Assert.Equal("Apple Books", h.Source);
        Assert.EndsWith("/600x600bb.jpg", h.ImageUrl);
        Assert.Equal("Set on the desert planet Arrakis, Dune is...", h.Description);
    }

    [Fact]
    public void StripHtmlDecodesEntities() => Assert.Equal("Tom & Jerry", CoverSearch.StripHtml("<b>Tom</b> &amp; Jerry"));
}
