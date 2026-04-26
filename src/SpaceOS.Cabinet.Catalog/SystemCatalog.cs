namespace SpaceOS.Cabinet.Catalog;

/// <summary>
/// Well-known identifiers for the system (curated) catalog tenant.
/// Entries owned by this tenant are the lowest-priority fallback in the 6-level resolution chain.
/// </summary>
public static class SystemCatalog
{
    /// <summary>The fixed TenantId used for all curated catalog entries.</summary>
    public static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>The fixed UserId used as the actor when seeding curated entries.</summary>
    public static readonly Guid ActorUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
}
