namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Checks whether the current tenant may read a Shared <c>CatalogEntry</c> owned by another tenant (§3.6).
/// Consumers implement this port to enforce share-lineage access control.
/// </summary>
public interface IShareLineageResolver
{
    /// <summary>
    /// Returns <c>true</c> if the current tenant has been granted read access to entries
    /// owned by <paramref name="entryOwnerTenantId"/>.
    /// </summary>
    /// <param name="entryOwnerTenantId">Tenant that owns the catalog entry.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> CanCurrentTenantReadEntryAsync(Guid entryOwnerTenantId, CancellationToken ct = default);
}
