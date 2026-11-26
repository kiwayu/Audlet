namespace Audlet.Views;

// Row arithmetic for the library's card grid, which creates only the cards near the visible part of the list.
// A card has one of two heights, known from its book before the card exists: a started book's card also shows a
// progress bar. So every row's height is known, and each row lies exactly where WrapPanel would put it.
public sealed class CardRows
{
    private bool[] _tall = [];   // per row: it holds a started book

    public int Columns { get; private set; }
    public int Count => _tall.Length;
    public double Short { get; set; }   // the height of a card, and of a started book's card
    public double Tall { get; set; }

    // As many slots as fit, as WrapPanel wraps: n slots fit when n * slot <= width. Never fewer than one.
    public static int ColumnsFor(double width, double slot) =>
        double.IsPositiveInfinity(width) ? int.MaxValue : Math.Max(1, (int)Math.Floor(width / slot + 1e-9));

    // started[i]: the i-th book's card is the tall kind.
    public void Reset(IReadOnlyList<bool> started, int columns)
    {
        Columns = columns;
        _tall = new bool[(started.Count + columns - 1) / columns];
        for (var i = 0; i < started.Count; i++) _tall[i / columns] |= started[i];
    }

    public double Height(int row) => _tall[row] ? Tall : Short;

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
