using Audlet.Models;
using Audlet.Player;

namespace Audlet.Tests;

public class HistoryTests
{
    [Fact]
    public void RecordsNewestFirstAndCapsAt100()
    {
        var book = new Book { Id = "x" };
        for (var i = 0; i < 150; i++)
            History.Record(book, HistoryKind.Seek, 0, i, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(i));
        Assert.Equal(100, book.History.Count);
        Assert.Equal(149, book.History[0].PositionSec);
        Assert.Equal(50, book.History[^1].PositionSec);
    }
}
