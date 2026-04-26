namespace SpaceOS.Cabinet.Application;

using SpaceOS.Cabinet.Abstractions;

/// <summary>No-op <see cref="ICatalogResolver"/> used when no pinned entries exist.</summary>
public sealed class NullCatalogResolver : ICatalogResolver
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullCatalogResolver Instance = new();

    private NullCatalogResolver() { }

    /// <inheritdoc/>
    public bool TryGetPinnedEntry(Guid skeletonId, Guid partId, CatalogType type, out Guid catalogEntryId)
    {
        catalogEntryId = Guid.Empty;
        return false;
    }
}
