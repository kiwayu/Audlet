using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Audlet.Models;

namespace Audlet.Views;

// Lays out equal-width cards as WrapPanel does, but creates only the cards in and near the visible part of the
// ScrollViewer around the list, so a large library costs about as much as a screenful of books.
public sealed class CardPanel : VirtualizingPanel
{
    private readonly CardRows _rows = new();
    private readonly double[] _kind = new double[2];   // a new card's height: [0] a book not started, [1] a started one
    private ScrollViewer? _scroller;
    private double _slot;

    // Raised before cards are created, with the items of their rows and of one more row on each side, so slow work
    // for them (decoding covers) can start early.
    public event Action<IReadOnlyList<object>>? Upcoming;

    public CardPanel() => Unloaded += (_, _) => { if (_scroller is not null) _scroller.ScrollChanged -= OnScroll; _scroller = null; };

    private void OnScroll(object sender, ScrollChangedEventArgs e)
    {
        if (e.OriginalSource == _scroller && (e.VerticalChange != 0 || e.ViewportHeightChange != 0)) InvalidateMeasure();
    }

    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        if (args.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move)
            RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size available)
    {
        var children = InternalChildren;   // creates the item container generator on first use
        var items = ItemsControl.GetItemsOwner(this)?.Items;
        var count = items?.Count ?? 0;
        if (_scroller is null && FindScroller() is { } scroller) { _scroller = scroller; scroller.ScrollChanged += OnScroll; }
        if (count == 0) { Clear(0, 0); _rows.Reset([], 1); return default; }
        var constraint = new Size(available.Width, double.PositiveInfinity);
        // A started book's card also shows its progress bar (see the card template), which makes it the tall kind.
        var started = new bool[count];
        for (var i = 0; i < count; i++) started[i] = items![i] is Book { BookPositionSec: > 0 };
        var heights = Heights(started, constraint);
        if (_slot == 0) _slot = children[0].DesiredSize.Width;
        var columns = Math.Min(count, CardRows.ColumnsFor(available.Width, _slot));
        _rows.Reset(heights, columns);

        // Half a screen above and below the visible part is created too, so scrolling reveals cards that already exist.
        var viewport = _scroller is { ViewportHeight: > 0 } s ? s.ViewportHeight : Window.GetWindow(this) is { Height: > 0 } w ? w.Height : 1080;
        var offset = _scroller?.VerticalOffset ?? 0;
        var (first, last) = _rows.Overlapping(offset - viewport / 2, offset + viewport * 1.5);
        var start = Math.Min(first * columns, count);
        var end = Math.Min((last + 1) * columns, count);
        if (Upcoming is { } upcoming && start < end)
        {
            var from = Math.Max(0, start - columns);
            upcoming(Enumerable.Range(from, Math.Min(count, end + columns) - from).Select(i => items![i]).ToList());
        }
        Create(start, end, constraint);
        Clear(start, end, started);
        _rows.Reset(Heights(started, constraint), columns);   // with the cards just made, and without those just removed
        return new Size(columns * _slot, _rows.Total);
    }

    // Each item's card height. A card that exists is as tall as it measured: it shows its book as the book was when the
    // card was made, which may since have changed. Any other card will be made from its book as it is now, so it is as
    // tall as a new card of its kind, measured once on a card made for that.
    private double[] Heights(bool[] started, Size constraint)
    {
        var heights = new double[started.Length];
        var generator = ItemContainerGenerator;
        for (var c = 0; c < InternalChildren.Count; c++)
            if (generator.IndexFromGeneratorPosition(new GeneratorPosition(c, 0)) is >= 0 and var item) heights[item] = InternalChildren[c].DesiredSize.Height;
        for (var i = 0; i < started.Length; i++)
        {
            if (heights[i] > 0) continue;
            var kind = started[i] ? 1 : 0;
            if (_kind[kind] == 0) { Create(i, i + 1, constraint); _kind[kind] = Container(i).DesiredSize.Height; }
            heights[i] = _kind[kind];
        }
        return heights;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var generator = ItemContainerGenerator;
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var item = generator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
            var row = item / _rows.Columns;
            var child = InternalChildren[i];
            child.Arrange(new Rect(item % _rows.Columns * _slot, _rows.Top(row), child.DesiredSize.Width, _rows.Height(row)));
        }
        return finalSize;
    }

    private ScrollViewer? FindScroller()
    {
        DependencyObject? d = this;
        while (d is not null and not ScrollViewer) d = VisualTreeHelper.GetParent(d);
        return d as ScrollViewer;
    }

    private UIElement Container(int item)
    {
        var position = ItemContainerGenerator.GeneratorPositionFromIndex(item);
        return InternalChildren[position.Index];
    }

    // Creates and measures the containers for items [start, end), keeping InternalChildren in item order.
    private void Create(int start, int end, Size constraint)
    {
        if (start >= end) return;
        var generator = ItemContainerGenerator;
        var position = generator.GeneratorPositionFromIndex(start);
        var index = position.Offset == 0 ? position.Index : position.Index + 1;
        using (generator.StartAt(position, GeneratorDirection.Forward, true))
        {
            for (var i = start; i < end; i++, index++)
            {
                var child = (UIElement)generator.GenerateNext(out var isNew);
                if (isNew)
                {
                    if (index >= InternalChildren.Count) AddInternalChild(child); else InsertInternalChild(index, child);
                    generator.PrepareItemContainer(child);
                }
                child.Measure(constraint);
            }
        }
    }

    // Removes the containers of items outside [start, end), except a card of the other kind than its book now is: the
    // book started or finished after the card was made, a new card would differ, and WrapPanel keeps the card until the
    // list refreshes.
    private void Clear(int start, int end, bool[]? started = null)
    {
        var generator = ItemContainerGenerator;
        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var position = new GeneratorPosition(i, 0);
            var item = generator.IndexFromGeneratorPosition(position);
            if (item >= start && item < end) continue;
            if (item >= 0 && started is not null && _kind[0] != _kind[1] && InternalChildren[i].DesiredSize.Height == _kind[started[item] ? 0 : 1]) continue;
            generator.Remove(position, 1);
            RemoveInternalChildRange(i, 1);
        }
    }
}
