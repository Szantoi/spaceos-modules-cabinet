namespace SpaceOS.Cabinet.Catalog.Infrastructure;

/// <summary>
/// Placeholder interceptor that will enforce cabinet-specific role constraints
/// on catalog mutations (e.g. restricting community submissions to tenants with the
/// <c>cabinet:community_contributor</c> role).
/// Full implementation is deferred to a future sprint when the Kernel RBAC integration is wired.
/// </summary>
public sealed class CabinetRoleInterceptor
{
    /// <summary>
    /// Returns <c>true</c> if the given user is permitted to submit community catalog entries.
    /// Placeholder: always returns <c>true</c> until RBAC is integrated.
    /// </summary>
    /// <param name="userId">The user performing the action.</param>
    /// <param name="tenantId">The tenant context.</param>
    public bool CanSubmitCommunityEntry(Guid userId, Guid tenantId) => true;

    /// <summary>
    /// Returns <c>true</c> if the given user is permitted to perform moderation actions
    /// (approve, reject, clear flags) on catalog entries.
    /// Placeholder: always returns <c>true</c> until RBAC is integrated.
    /// </summary>
    /// <param name="userId">The user performing the action.</param>
    public bool CanModerate(Guid userId) => true;
}
