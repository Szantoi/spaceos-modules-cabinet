namespace SpaceOS.Cabinet.Application;

using SpaceOS.Cabinet.Domain;

/// <summary>
/// Write-side port for <see cref="TenantStandard"/> persistence (consumer implements).
/// Placed in the Application layer because <see cref="TenantStandard"/> lives in Domain,
/// and Abstractions must not depend on Domain.
/// </summary>
public interface ITenantStandardWriteRepository
{
    /// <summary>Persists a new <see cref="TenantStandard"/> aggregate.</summary>
    /// <param name="standard">The new aggregate to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(TenantStandard standard, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a <see cref="TenantStandard"/> by its unique identifier,
    /// or <c>null</c> if not found.
    /// </summary>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<TenantStandard?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persists mutations to an existing <see cref="TenantStandard"/> aggregate.</summary>
    /// <param name="standard">The aggregate with applied changes.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(TenantStandard standard, CancellationToken ct = default);
}
