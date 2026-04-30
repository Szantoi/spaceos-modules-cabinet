namespace SpaceOS.Cabinet.Application;

using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;

/// <summary>Repository contract for <see cref="CatalogEntry"/> persistence (implemented by the consumer).</summary>
public interface ICatalogEntryRepository
{
    /// <summary>Retrieves a <see cref="CatalogEntry"/> by its unique identifier, or <c>null</c> if not found.</summary>
    Task<CatalogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Lists catalog entries filtered by tenant, type and/or state.</summary>
    Task<IReadOnlyList<CatalogEntry>> ListAsync(
        Guid tenantId,
        CatalogType? type = null,
        CatalogLifecycleState? state = null,
        CancellationToken ct = default);

    /// <summary>Persists a new <see cref="CatalogEntry"/>.</summary>
    Task AddAsync(CatalogEntry entry, CancellationToken ct = default);

    /// <summary>Persists mutations to an existing <see cref="CatalogEntry"/>.</summary>
    Task UpdateAsync(CatalogEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Finds the first catalog entry for the given tenant whose <c>SimilarityFingerprint</c>
    /// matches <paramref name="fingerprint"/>, or <c>null</c> if none exists.
    /// Used by the community UPSERT path (SEC-02).
    /// </summary>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="fingerprint">The server-computed fingerprint to match.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CatalogEntry?> GetByFingerprintAsync(Guid tenantId, string fingerprint, CancellationToken ct = default);
}
