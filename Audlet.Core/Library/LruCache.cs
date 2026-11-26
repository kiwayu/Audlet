namespace Audlet.Library;

// Keeps the most recently used values, up to a fixed count; adding past the count drops the least recently used one.
public sealed class LruCache<TKey, TValue>(int capacity) where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _nodes = [];
    private readonly LinkedList<(TKey Key, TValue Value)> _order = new();

    public int Count => _nodes.Count;
    public int Capacity { get; set; } = capacity;

    public bool TryGet(TKey key, out TValue value)
    {
        if (!_nodes.TryGetValue(key, out var node)) { value = default!; return false; }
        _order.Remove(node);
        _order.AddFirst(node);
        value = node.Value.Value;
        return true;
    }

    public void Add(TKey key, TValue value)
    {
        if (_nodes.Remove(key, out var old)) _order.Remove(old);
        _nodes[key] = _order.AddFirst((key, value));
        if (_nodes.Count <= Capacity) return;
        _nodes.Remove(_order.Last!.Value.Key);
        _order.RemoveLast();
    }
}
