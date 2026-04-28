namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Read-side port for <c>TenantStandard</c> persistence (consumer implements).
/// Decouples the domain from the infrastructure layer.
/// </summary>
public interface ITenantStandardRepository
{
    /// <summary>
    /// Returns the flat <see cref="TenantStandardSnapshot"/> for the given tenant,
    /// or <c>null</c> if no standard has been configured for that tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<TenantStandardSnapshot?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Returns all <see cref="TenantStandardSnapshot"/> records for the given tenant.
    /// Typically returns zero or one item (one standard per tenant per module).
    /// </summary>
    /// <param name="tenantId">The tenant identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<TenantStandardSnapshot>> ListByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}

/// <summary>
/// Flat DTO returned by <see cref="ITenantStandardRepository"/>.
/// Carries all persisted tenant standard fields for read-side consumers.
/// </summary>
public sealed record TenantStandardSnapshot(
    Guid Id,
    Guid TenantId,
    string CarcassMaterial,
    double CarcassThicknessMm,
    string BackPanelMaterial,
    double BackPanelThicknessMm,
    bool LineBoreEnabled,
    double FirstHoleOffsetMm,
    double SpacingMm,
    double DiameterMm,
    double TallCabinetHeightMm,
    double LongShelfMm,
    IReadOnlyDictionary<string, string> RuleSeverityOverrides);
