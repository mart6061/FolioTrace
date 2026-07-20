using Services;

namespace Test;

public sealed class BoundedLruCacheTests
{
    [Fact]
    public void RespectsItsConfiguredCapacity()
    {
        var cache = new BoundedLruCache<int, string>(2);

        cache[1] = "one";
        cache[2] = "two";
        cache[3] = "three";

        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void EvictsTheLeastRecentlyUsedEntryFirst()
    {
        var cache = new BoundedLruCache<int, string>(2);

        cache[1] = "one";
        cache[2] = "two";
        cache[3] = "three";

        Assert.False(cache.ContainsKey(1));
        Assert.True(cache.ContainsKey(2));
        Assert.True(cache.ContainsKey(3));
    }

    [Fact]
    public void ReadingAnEntryCountsAsRecentUse()
    {
        var cache = new BoundedLruCache<int, string>(2);

        cache[1] = "one";
        cache[2] = "two";

        // Reading key 1 should make key 2 the least-recently-used entry instead.
        Assert.True(cache.TryGetValue(1, out _));

        cache[3] = "three";

        Assert.True(cache.ContainsKey(1));
        Assert.False(cache.ContainsKey(2));
        Assert.True(cache.ContainsKey(3));
    }

    [Fact]
    public void OverwritingAnExistingKeyDoesNotConsumeCapacity()
    {
        var cache = new BoundedLruCache<int, string>(2);

        cache[1] = "one";
        cache[2] = "two";
        cache[1] = "one-updated";

        Assert.Equal(2, cache.Count);
        Assert.True(cache.TryGetValue(1, out var value));
        Assert.Equal("one-updated", value);
    }

    [Fact]
    public void RemoveDeletesAnEntry()
    {
        var cache = new BoundedLruCache<int, string>(2);

        cache[1] = "one";

        Assert.True(cache.Remove(1));
        Assert.False(cache.ContainsKey(1));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void ClearRemovesEverything()
    {
        var cache = new BoundedLruCache<int, string>(2);

        cache[1] = "one";
        cache[2] = "two";
        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.Empty(cache.Keys);
    }
}
