namespace SpaceOS.Cabinet.Catalog;

/// <summary>
/// No-op <see cref="IStaffAuditLogger"/> for use in test and local environments
/// where no audit persistence is required.
/// </summary>
public sealed class NullStaffAuditLogger : IStaffAuditLogger
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullStaffAuditLogger Instance = new();

    private NullStaffAuditLogger() { }

    /// <inheritdoc/>
    public Task LogAsync(
        Guid staffUserId,
        string action,
        Guid catalogEntryId,
        string? details = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task LogSystemActorActivationAsync(
        Guid catalogEntryId,
        string reason,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
