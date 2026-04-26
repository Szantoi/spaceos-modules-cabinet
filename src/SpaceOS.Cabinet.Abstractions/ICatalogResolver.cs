namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Lightweight resolver interface for Domain-layer use.
/// Allows Skeleton to check pinned catalog entries without depending on the full Catalog package.
/// </summary>
public interface ICatalogResolver
{
    /// <summary>
    /// Returns whether a catalog entry has been pinned for the given skeleton + part + type combination.
    /// </summary>
    /// <param name="skeletonId">The skeleton whose pins are checked.</param>
    /// <param name="partId">The specific part within the skeleton.</param>
    /// <param name="type">The catalog type slot.</param>
    /// <param name="catalogEntryId">The pinned catalog entry ID, or <see cref="Guid.Empty"/> if not pinned.</param>
    /// <returns><c>true</c> if a pin exists; <c>false</c> otherwise.</returns>
    bool TryGetPinnedEntry(Guid skeletonId, Guid partId, CatalogType type, out Guid catalogEntryId);
}
