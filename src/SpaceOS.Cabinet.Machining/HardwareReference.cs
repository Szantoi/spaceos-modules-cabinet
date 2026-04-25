namespace SpaceOS.Cabinet.Machining;

/// <summary>
/// Identifies a hardware item (hinge, dowel, connector, etc.) from a tenant hardware catalog.
/// Immutable value object — equality is by catalog identity.
/// </summary>
/// <param name="CatalogId">Unique item identifier within the catalog (e.g. SKU or part number).</param>
/// <param name="CatalogType">Logical catalog namespace (e.g. "Blum", "Häfele", "Tenant").</param>
public sealed record HardwareReference(string CatalogId, string CatalogType)
{
    /// <summary>
    /// Returns <c>true</c> when both <see cref="CatalogId"/> and <see cref="CatalogType"/>
    /// are non-empty, non-whitespace strings.
    /// </summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(CatalogId) &&
        !string.IsNullOrWhiteSpace(CatalogType);
}
