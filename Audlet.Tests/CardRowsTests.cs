using Audlet.Views;

namespace Audlet.Tests;

public class CardRowsTests
{
    [Fact]
    public void ColumnsAreTheSlotsThatFitTheWidthAsWrapPanelWraps()
    {
        Assert.Equal(4, CardRows.ColumnsFor(784, 196));
        Assert.Equal(3, CardRows.ColumnsFor(783.9, 196));
        Assert.Equal(1, CardRows.ColumnsFor(100, 196));
    }

    [Fact]
    public void ARowIsTallWhenAnyOfItsBooksIsStarted()
    {
        var rows = new CardRows { Short = 300, Tall = 316 };
        bool[] started = [false, false, false, false, false, true, false, false, false, false];
        rows.Reset(started, columns: 4);
        Assert.Equal(3, rows.Count);
        Assert.Equal([300, 316, 300], [rows.Height(0), rows.Height(1), rows.Height(2)]);
        Assert.Equal(616, rows.Top(2));
        Assert.Equal(916, rows.Total);
        // The same books in a narrower window: the started one now shares a row with others, and the total follows.
        rows.Reset(started, columns: 2);
        Assert.Equal(1516, rows.Total);
    }

    [Fact]
    public void OverlappingFindsTheRowsInABand()
    {
        var rows = new CardRows { Short = 100, Tall = 120 };
        rows.Reset(new bool[40], columns: 4);
        Assert.Equal((0, 2), rows.Overlapping(-50, 250));
        Assert.Equal((3, 4), rows.Overlapping(300, 500));
        Assert.Equal((9, 9), rows.Overlapping(950, 2000));
        var (first, last) = rows.Overlapping(1000, 1100);
        Assert.True(last < first);
    }
}
