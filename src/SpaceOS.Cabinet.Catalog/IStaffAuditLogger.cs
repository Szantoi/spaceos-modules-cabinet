namespace SpaceOS.Cabinet.Catalog;

/// <summary>
/// Logs staff mutations to curated catalog entries (SEC-CAB02-1, BE-CAB02-6).
/// </summary>
/// <remarks>
/// DI lifetime: Singleton — implementations use <c>IServiceScopeFactory</c> internally
/// to avoid a captive dependency on a scoped DbContext.
/// </remarks>
public interface IStaffAuditLogger
{
    /// <summary>Logs a staff action on a catalog entry.</summary>
    /// <param name="staffUserId">Staff user who performed the action.</param>
    /// <param name="action">Action name (e.g. "Approve", "Publish", "Deprecate", "Reject").</param>
    /// <param name="catalogEntryId">The affected catalog entry.</param>
    /// <param name="details">Optional additional context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync(
        Guid staffUserId,
        string action,
        Guid catalogEntryId,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>Logs system actor activation (BE-CAB02-6: <c>app.is_system_actor</c> audit).</summary>
    /// <param name="catalogEntryId">The affected catalog entry.</param>
    /// <param name="reason">Reason for system actor activation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogSystemActorActivationAsync(
        Guid catalogEntryId,
        string reason,
        CancellationToken cancellationToken = default);
}
