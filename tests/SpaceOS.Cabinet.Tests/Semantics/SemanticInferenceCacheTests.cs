using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Semantics;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Semantics;

public class SemanticInferenceCacheTests
{
    // ── TryGet ───────────────────────────────────────────────────────────────

    [Fact]
    public void TryGet_EmptyCache_ReturnsNull()
    {
        var cache = new SemanticInferenceCache();
        var version = Guid.NewGuid();
        var partId = Guid.NewGuid();

        var result = cache.TryGet(version, partId);

        Assert.Null(result);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedValue()
    {
        var cache = new SemanticInferenceCache();
        var version = Guid.NewGuid();
        var partId = Guid.NewGuid();

        cache.Set(version, partId, PartRole.Shelf);
        var result = cache.TryGet(version, partId);

        Assert.Equal(PartRole.Shelf, result);
    }

    [Fact]
    public void TryGet_DifferentVersion_ReturnsNull()
    {
        var cache = new SemanticInferenceCache();
        var version1 = Guid.NewGuid();
        var version2 = Guid.NewGuid();
        var partId = Guid.NewGuid();

        cache.Set(version1, partId, PartRole.Shelf);
        var result = cache.TryGet(version2, partId);

        Assert.Null(result);
    }

    [Fact]
    public void TryGet_DifferentPartId_ReturnsNull()
    {
        var cache = new SemanticInferenceCache();
        var version = Guid.NewGuid();

        cache.Set(version, Guid.NewGuid(), PartRole.Shelf);
        var result = cache.TryGet(version, Guid.NewGuid());

        Assert.Null(result);
    }

    // ── Set / Count ───────────────────────────────────────────────────────────

    [Fact]
    public void Count_ReflectsNumberOfEntries()
    {
        var cache = new SemanticInferenceCache();
        var version = Guid.NewGuid();

        Assert.Equal(0, cache.Count);

        cache.Set(version, Guid.NewGuid(), PartRole.Shelf);
        Assert.Equal(1, cache.Count);

        cache.Set(version, Guid.NewGuid(), PartRole.Bottom);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Set_OverMaxSize_EvictsAllEntries()
    {
        const int maxSize = 5;
        var cache = new SemanticInferenceCache(maxSize);
        var version = Guid.NewGuid();

        // Fill to capacity
        for (int i = 0; i < maxSize; i++)
            cache.Set(version, Guid.NewGuid(), PartRole.Shelf);

        Assert.Equal(maxSize, cache.Count);

        // One more entry should trigger eviction then re-add
        cache.Set(version, Guid.NewGuid(), PartRole.Bottom);

        // After eviction + re-add, count should be 1
        Assert.Equal(1, cache.Count);
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var cache = new SemanticInferenceCache();
        var version = Guid.NewGuid();
        cache.Set(version, Guid.NewGuid(), PartRole.Shelf);
        cache.Set(version, Guid.NewGuid(), PartRole.Bottom);

        cache.Clear();

        Assert.Equal(0, cache.Count);
    }

    // ── Thread safety ─────────────────────────────────────────────────────────

    [Fact]
    public void ConcurrentAccess_DoesNotThrow()
    {
        var cache = new SemanticInferenceCache();
        var version = Guid.NewGuid();

        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var partId = Guid.NewGuid();
            cache.Set(version, partId, PartRole.Shelf);
            cache.TryGet(version, partId);
        })).ToArray();

        // Should not throw — ConcurrentDictionary provides thread safety
        var ex = Record.Exception(() => Task.WaitAll(tasks));
        Assert.Null(ex);
    }
}
