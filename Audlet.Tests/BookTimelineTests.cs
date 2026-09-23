using Audlet.Models;
using Audlet.Player;

namespace Audlet.Tests;

public class BookTimelineTests
{
    private static Book Book3() => new()
    {
        Id = "x",
        Files = [new AudioFile("1", 100, "a"), new AudioFile("2", 200, "b"), new AudioFile("3", 50, "c")],
        Chapters = [new Chapter("a1", 0, 0, 40), new Chapter("a2", 0, 40, 100), new Chapter("b", 1, 0, 200), new Chapter("c", 2, 0, 50)],
    };

    [Fact] public void TotalSumsFiles() => Assert.Equal(350, new BookTimeline(Book3()).Total);

    [Fact]
    public void BookSecondsMapBothWays()
    {
        var t = new BookTimeline(Book3());
        Assert.Equal(130, t.ToBookSec(1, 30));
        Assert.Equal((1, 30), t.FromBookSec(130));
        Assert.Equal((2, 50), t.FromBookSec(999));
        Assert.Equal((0, 0), t.FromBookSec(-5));
    }

    [Fact]
    public void ChapterLookup()
    {
        var t = new BookTimeline(Book3());
        Assert.Equal(1, t.ChapterIndexAt(0, 40));
        Assert.Equal(0, t.ChapterIndexAt(0, 39.9));
        Assert.Equal(2, t.ChapterIndexAt(1, 150));
        Assert.Equal("c", t.ChapterAt(2, 10)!.Title);
    }

    [Fact]
    public void OffsetCrossesFiles()
    {
        var t = new BookTimeline(Book3());
        Assert.Equal((1, 10), t.Offset(0, 90, 20));
        Assert.Equal((0, 90), t.Offset(1, 10, -20));
        Assert.Equal((0, 0), t.Offset(0, 5, -60));
        Assert.Equal((2, 50), t.Offset(2, 40, 60));
    }
}
