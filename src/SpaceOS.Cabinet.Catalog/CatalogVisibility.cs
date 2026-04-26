namespace SpaceOS.Cabinet.Catalog;

/// <summary>Controls which tenants can discover and resolve a <see cref="CatalogEntry"/>.</summary>
public enum CatalogVisibility
{
    /// <summary>Only the owning tenant can use this entry.</summary>
    Private,

    /// <summary>Shared within a tenant group (Cabinet 0.3+).</summary>
    Shared,

    /// <summary>Visible to the entire community (Cabinet 0.3+).</summary>
    Community,

    /// <summary>System-curated entry, owned by <see cref="SystemCatalog.TenantId"/>. Lowest-priority fallback.</summary>
    Curated
}
