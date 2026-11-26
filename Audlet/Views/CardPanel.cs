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
        // One card of each kind is measured once, so every row's height is known before its cards are created.
        if (_rows.Short == 0 && Array.IndexOf(started, false) is >= 0 and var plain) { Create(plain, plain + 1, constraint); _rows.Short = Container(plain).DesiredSize.Height; }
        if (_rows.Tall == 0 && Array.IndexOf(started, true) is >= 0 and var tall) { Create(tall, tall + 1, constraint); _rows.Tall = Container(tall).DesiredSize.Height; }
        if (_slot == 0) _slot = children[0].DesiredSize.Width;
        var columns = Math.Min(count, CardRows.ColumnsFor(available.Width, _slot));
        _rows.Reset(started, columns);

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
        Clear(start, end);
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
