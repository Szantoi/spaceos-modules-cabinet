namespace SpaceOS.Cabinet.Abstractions;

using Ardalis.Result;

/// <summary>
/// Admin-only flag moderation operations for <c>CatalogEntry</c> (§3.7).
/// Consumers implement this port for admin tooling.
/// </summary>
public interface IFlagModerationProvider
{
    /// <summary>
    /// Clears all active flags on the specified catalog entry and optionally sets a
    /// time-bounded admin acknowledgment window.
    /// </summary>
    /// <param name="catalogEntryId">The catalog entry to clear flags on.</param>
    /// <param name="adminUserId">The admin user performing the action.</param>
    /// <param name="ackDuration">Optional acknowledgment window; defaults to 90 days if <c>null</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> ClearFlagsAsync(Guid catalogEntryId, Guid adminUserId, TimeSpan? ackDuration, CancellationToken ct = default);

    /// <summary>
    /// Permanently removes a catalog entry from the system.
    /// </summary>
    /// <param name="catalogEntryId">The catalog entry to remove.</param>
    /// <param name="adminUserId">The admin user performing the action.</param>
    /// <param name="reason">Non-empty reason for the removal (audit trail).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<Result> RemoveEntryAsync(Guid catalogEntryId, Guid adminUserId, string reason, CancellationToken ct = default);
}
