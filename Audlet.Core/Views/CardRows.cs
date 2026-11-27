namespace Audlet.Views;

// Row arithmetic for the library's card grid, which creates only the cards near the visible part of the list.
// A row is as tall as its tallest card, as in WrapPanel, so with every card's height known each row lies exactly
// where WrapPanel would put it.
public sealed class CardRows
{
    private double[] _height = [];   // per row

    public int Columns { get; private set; }
    public int Count => _height.Length;

    // As many slots as fit, as WrapPanel wraps: n slots fit when n * slot <= width. Never fewer than one.
    public static int ColumnsFor(double width, double slot) =>
        double.IsPositiveInfinity(width) ? int.MaxValue : Math.Max(1, (int)Math.Floor(width / slot + 1e-9));

    // heights[i]: the height of the i-th card.
    public void Reset(IReadOnlyList<double> heights, int columns)
    {
        Columns = columns;
        _height = new double[(heights.Count + columns - 1) / columns];
        for (var i = 0; i < heights.Count; i++) _height[i / columns] = Math.Max(_height[i / columns], heights[i]);
    }

    public double Height(int row) => _height[row];

    public double Top(int row)
    {
        var top = 0.0;
        for (var r = 0; r < row; r++) top += Height(r);
        return top;
    }

    public double Total => Top(Count);

    // The rows that overlap [from, to); Last is below First when none do.
    public (int First, int Last) Overlapping(double from, double to)
    {
        int first = Count, last = -1;
        var top = 0.0;
        for (var r = 0; r < Count && top < to; r++)
        {
            var bottom = top + Height(r);
            if (bottom > from) { first = Math.Min(first, r); last = r; }
            top = bottom;
        }
        return (first, last);
    }
}
