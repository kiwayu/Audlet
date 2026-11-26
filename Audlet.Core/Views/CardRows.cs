namespace Audlet.Views;

// Row arithmetic for the library's card grid, which creates only the cards near the visible part of the list.
// Rows not laid out yet count with the height of the shortest row measured so far: rows without a started book.
public sealed class CardRows
{
    private readonly List<double> _heights = [];   // NaN until the row has been measured

    public int Columns { get; private set; }
    public int Count => _heights.Count;
    public double Estimate { get; set; }

    // As many slots as fit, as WrapPanel wraps: n slots fit when n * slot <= width. Never fewer than one.
    public static int ColumnsFor(double width, double slot) =>
        double.IsPositiveInfinity(width) ? int.MaxValue : Math.Max(1, (int)Math.Floor(width / slot + 1e-9));

    // A new item or column count invalidates every measured row.
    public void Reset(int items, int columns)
    {
        Columns = columns;
        _heights.Clear();
        for (var i = (items + columns - 1) / columns; i > 0; i--) _heights.Add(double.NaN);
    }

    public double Height(int row) => double.IsNaN(_heights[row]) ? Estimate : _heights[row];

    public void Measured(int row, double height)
    {
        _heights[row] = height;
        Estimate = Estimate == 0 ? height : Math.Min(Estimate, height);
    }

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
