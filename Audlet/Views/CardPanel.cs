using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Audlet.Views;

// Lays out equal-width cards as WrapPanel does, but creates only the cards in and near the visible part of the
// ScrollViewer around the list, so a large library costs about as much as a screenful of books.
public sealed class CardPanel : VirtualizingPanel
{
    private readonly CardRows _rows = new();
    private ScrollViewer? _scroller;
    private double _slot;
    private bool _stale = true;

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
        _stale = true;
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size available)
    {
        var children = InternalChildren;   // creates the item container generator on first use
        var count = ItemsControl.GetItemsOwner(this)?.Items.Count ?? 0;
        if (_scroller is null && FindScroller() is { } scroller) { _scroller = scroller; scroller.ScrollChanged += OnScroll; }
        if (count == 0) { Clear(0, 0); _rows.Reset(0, 1); return default; }
        var constraint = new Size(available.Width, double.PositiveInfinity);
        if (_slot == 0)
        {
            Create(0, 1, constraint);
            _slot = children[0].DesiredSize.Width;
            _rows.Estimate = children[0].DesiredSize.Height;
        }
        var columns = Math.Min(count, CardRows.ColumnsFor(available.Width, _slot));
        if (_stale || columns != _rows.Columns) { _rows.Reset(count, columns); _stale = false; }

        // Half a screen above and below the visible part is created too, so scrolling reveals cards that already exist.
        var viewport = _scroller is { ViewportHeight: > 0 } s ? s.ViewportHeight : Window.GetWindow(this) is { Height: > 0 } w ? w.Height : 1080;
        var offset = _scroller?.VerticalOffset ?? 0;
        var (first, last) = _rows.Overlapping(offset - viewport / 2, offset + viewport * 1.5);
        var start = Math.Min(first * columns, count);
        var end = Math.Min((last + 1) * columns, count);
        if (Upcoming is { } upcoming && start < end)
        {
            var items = ItemsControl.GetItemsOwner(this)!.Items;
            var from = Math.Max(0, start - columns);
            upcoming(Enumerable.Range(from, Math.Min(count, end + columns) - from).Select(i => items[i]).ToList());
        }
        Create(start, end, constraint);
        Clear(start, end);

        // Rows created above the visible top may turn out taller than estimated; scroll by the difference so nothing on screen moves.
        var visible = _rows.Overlapping(offset, offset + 1).First;
        var shift = 0.0;
        for (var r = first; r <= last; r++)
        {
            var height = 0.0;
            for (var i = r * columns; i < Math.Min(count, (r + 1) * columns); i++) height = Math.Max(height, Container(i).DesiredSize.Height);
            if (r < visible) shift += height - _rows.Height(r);
            _rows.Measured(r, height);
        }
        if (shift != 0) _scroller?.ScrollToVerticalOffset(offset + shift);
        return new Size(columns * _slot, _rows.Total);
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

    // Removes the containers of items outside [start, end).
    private void Clear(int start, int end)
    {
        var generator = ItemContainerGenerator;
        for (var i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var position = new GeneratorPosition(i, 0);
            var item = generator.IndexFromGeneratorPosition(position);
            if (item >= start && item < end) continue;
            generator.Remove(position, 1);
            RemoveInternalChildRange(i, 1);
        }
    }
}
