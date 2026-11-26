using Audlet.Library;

namespace Audlet.Tests;

public class LruCacheTests
{
    [Fact]
    public void DropsTheLeastRecentlyUsedValuePastCapacity()
    {
        var cache = new LruCache<string, int>(2);
        cache.Add("a", 1);
        cache.Add("b", 2);
        Assert.True(cache.TryGet("a", out _));   // a is now newer than b
        cache.Add("c", 3);
        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("a", out var a));
        Assert.Equal(1, a);
        Assert.True(cache.TryGet("c", out var c));
        Assert.Equal(3, c);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void AddingAnExistingKeyReplacesItsValue()
    {
        var cache = new LruCache<string, int>(2);
        cache.Add("a", 1);
        cache.Add("a", 5);
        Assert.Equal(1, cache.Count);
        Assert.True(cache.TryGet("a", out var a));
        Assert.Equal(5, a);
    }
}
