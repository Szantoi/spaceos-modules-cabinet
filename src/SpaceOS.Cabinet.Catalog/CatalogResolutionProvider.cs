namespace SpaceOS.Cabinet.Catalog;

using Ardalis.Result;

/// <summary>
/// 6-level catalog resolution with per-request cache (BE-CAB02-2).
/// Implements levels 3 (tenant-private) and 6 (curated fallback) for Cabinet 0.1.
/// Levels 1 (skeleton-pin), 2 (template-default), 4 (tenant-shared), 5 (community) are planned for 0.3+.
/// </summary>
/// <remarks>
/// DI lifetime: <b>Scoped</b> — one instance per HTTP request to ensure the cache is request-scoped.
/// </remarks>
public sealed class CatalogResolutionProvider : ICatalogResolutionProvider
{
    private readonly IReadOnlyList<CatalogEntry> _publishedEntries;
    private readonly Dictionary<(Guid, CatalogType), CatalogEntry?> _cache = new();

    /// <summary>
    /// Initializes a new <see cref="CatalogResolutionProvider"/> with all known published entries.
    /// Non-published entries are filtered out immediately.
    /// </summary>
    /// <param name="publishedEntries">All catalog entries available to the resolution engine.</param>
    public CatalogResolutionProvider(IEnumerable<CatalogEntry> publishedEntries)
    {
        ArgumentNullException.ThrowIfNull(publishedEntries);
        _publishedEntries = publishedEntries
            .Where(e => e.State == CatalogLifecycleState.Published)
            .ToList();
    }

    /// <inheritdoc/>
    public Result<CatalogEntry> Resolve(Guid tenantId, CatalogType type, CatalogResolutionContext context)
    {
        var cacheKey = (tenantId, type);
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return cached is not null
                ? Result<CatalogEntry>.Success(cached)
                : Result<CatalogEntry>.Error($"No catalog entry found for type '{type}' and tenant '{tenantId}'.");
        }

        // Level 3: Tenant-private
        var tenantPrivate = _publishedEntries.FirstOrDefault(e =>
            e.TenantId == tenantId &&
            e.Visibility == CatalogVisibility.Private &&
            e.Type == type);

        if (tenantPrivate is not null)
        {
            _cache[cacheKey] = tenantPrivate;
            return Result<CatalogEntry>.Success(tenantPrivate);
        }

        // Level 6: Curated fallback
        var curated = _publishedEntries.FirstOrDefault(e =>
            e.TenantId == SystemCatalog.TenantId &&
            e.Visibility == CatalogVisibility.Curated &&
            e.Type == type);

        if (curated is not null)
        {
            _cache[cacheKey] = curated;
            return Result<CatalogEntry>.Success(curated);
        }

        _cache[cacheKey] = null;
        return Result<CatalogEntry>.Error($"No catalog entry found for type '{type}' and tenant '{tenantId}'.");
    }
}
