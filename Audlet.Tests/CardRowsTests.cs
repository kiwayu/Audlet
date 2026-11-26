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
    public void UnmeasuredRowsTakeTheShortestMeasuredHeight()
    {
        var rows = new CardRows { Estimate = 320 };
        rows.Reset(items: 10, columns: 4);
        Assert.Equal(3, rows.Count);
        rows.Measured(0, 320);
        rows.Measured(1, 300);
        Assert.Equal(300, rows.Height(2));
        Assert.Equal(620, rows.Top(2));
        Assert.Equal(920, rows.Total);
    }

    [Fact]
    public void OverlappingFindsTheRowsInABand()
    {
        var rows = new CardRows { Estimate = 100 };
        rows.Reset(items: 40, columns: 4);
        Assert.Equal((0, 2), rows.Overlapping(-50, 250));
        Assert.Equal((3, 4), rows.Overlapping(300, 500));
        Assert.Equal((9, 9), rows.Overlapping(950, 2000));
        var (first, last) = rows.Overlapping(1000, 1100);
        Assert.True(last < first);
    }
}
