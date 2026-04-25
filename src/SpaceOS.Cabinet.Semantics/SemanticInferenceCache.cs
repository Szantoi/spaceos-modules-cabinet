using System.Collections.Concurrent;
using SpaceOS.Cabinet.Abstractions;

namespace SpaceOS.Cabinet.Semantics;

/// <summary>
/// Lockless version-keyed cache for semantic inference results (DB-CAB-6).
/// Cache key: (SkeletonVersion, PartId).
/// Old entries are evicted wholesale when <see cref="DefaultMaxCacheSize"/> is reached.
/// </summary>
public sealed class SemanticInferenceCache
{
    /// <summary>Default maximum number of cached entries before eviction.</summary>
    public const int DefaultMaxCacheSize = 10_000;

    private readonly int _maxCacheSize;
    private readonly ConcurrentDictionary<(Guid SkeletonVersion, Guid PartId), PartRole> _cache = new();

    /// <summary>
    /// Initialises the cache with the given capacity cap.
    /// </summary>
    /// <param name="maxCacheSize">Maximum entries before a full eviction sweep.</param>
    public SemanticInferenceCache(int maxCacheSize = DefaultMaxCacheSize)
    {
        if (maxCacheSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCacheSize), "maxCacheSize must be > 0.");
        _maxCacheSize = maxCacheSize;
    }

    /// <summary>
    /// Returns the cached role for the given skeleton version and part, or <c>null</c> if not cached.
    /// </summary>
    /// <param name="skeletonVersion">The skeleton's optimistic concurrency version.</param>
    /// <param name="partId">The part identifier.</param>
    public PartRole? TryGet(Guid skeletonVersion, Guid partId)
    {
        return _cache.TryGetValue((skeletonVersion, partId), out var role) ? role : null;
    }

    /// <summary>
    /// Stores a role inference result. Evicts all entries if the cache is at capacity.
    /// </summary>
    /// <param name="skeletonVersion">The skeleton's optimistic concurrency version.</param>
    /// <param name="partId">The part identifier.</param>
    /// <param name="role">The inferred role.</param>
    public void Set(Guid skeletonVersion, Guid partId, PartRole role)
    {
        // Simple eviction: when at capacity, clear all (LRU approximation).
        if (_cache.Count >= _maxCacheSize)
            _cache.Clear();

        _cache[(skeletonVersion, partId)] = role;
    }

    /// <summary>Number of entries currently in the cache.</summary>
    public int Count => _cache.Count;

    /// <summary>Clears all cached entries.</summary>
    public void Clear() => _cache.Clear();
}
