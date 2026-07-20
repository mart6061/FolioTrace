namespace Services;

/// <summary>
/// A fixed-capacity cache that evicts the least-recently-used entry when a new key would exceed capacity.
/// Not thread-safe on its own - callers are expected to guard access with their own lock, matching the
/// existing Lock-based concurrency pattern already used by every service that owns one of these.
/// </summary>
public sealed class BoundedLruCache<TKey, TValue> where TKey : notnull
{
    private readonly int capacity;
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> map = [];
    private readonly LinkedList<KeyValuePair<TKey, TValue>> recencyOrder = new();

    public BoundedLruCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

        this.capacity = capacity;
    }

    public int Count => map.Count;

    public IEnumerable<TKey> Keys => map.Keys;

    public IEnumerable<TValue> Values => recencyOrder.Select(entry => entry.Value);

    public bool ContainsKey(TKey key) => map.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (map.TryGetValue(key, out var node))
        {
            Touch(node);
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public TValue this[TKey key]
    {
        set
        {
            if (map.TryGetValue(key, out var existing))
            {
                existing.Value = new KeyValuePair<TKey, TValue>(key, value);
                Touch(existing);
                return;
            }

            if (map.Count >= capacity)
                EvictLeastRecentlyUsed();

            var node = recencyOrder.AddLast(new KeyValuePair<TKey, TValue>(key, value));
            map[key] = node;
        }
    }

    public bool Remove(TKey key)
    {
        if (!map.TryGetValue(key, out var node))
            return false;

        recencyOrder.Remove(node);
        map.Remove(key);
        return true;
    }

    public void Clear()
    {
        map.Clear();
        recencyOrder.Clear();
    }

    private void Touch(LinkedListNode<KeyValuePair<TKey, TValue>> node)
    {
        recencyOrder.Remove(node);
        recencyOrder.AddLast(node);
    }

    private void EvictLeastRecentlyUsed()
    {
        var lru = recencyOrder.First;
        if (lru is null)
            return;

        recencyOrder.RemoveFirst();
        map.Remove(lru.Value.Key);
    }
}
