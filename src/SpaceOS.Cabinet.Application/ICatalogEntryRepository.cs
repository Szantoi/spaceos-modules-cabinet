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
}
