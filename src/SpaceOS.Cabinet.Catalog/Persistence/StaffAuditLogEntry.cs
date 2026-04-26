namespace SpaceOS.Cabinet.Catalog.Persistence;

/// <summary>Audit log record for staff mutations to curated catalog entries (SEC-CAB02-4).</summary>
public sealed class StaffAuditLogEntry
{
    /// <summary>Unique identifier of this audit log entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Staff user who performed the action.</summary>
    public Guid StaffUserId { get; private set; }

    /// <summary>Action name (e.g. "Approve", "Publish", "Deprecate", "Reject").</summary>
    public string Action { get; private set; }

    /// <summary>The affected catalog entry ID.</summary>
    public Guid CatalogEntryId { get; private set; }

    /// <summary>UTC timestamp when the action occurred.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Optional additional context for the action.</summary>
    public string? Details { get; private set; }

    // EF Core parameterless constructor
    private StaffAuditLogEntry() { Action = string.Empty; }

    /// <summary>
    /// Creates a new <see cref="StaffAuditLogEntry"/> with the specified staff action details.
    /// </summary>
    /// <param name="staffUserId">Staff user who performed the action. Must not be empty.</param>
    /// <param name="action">Action name. Must not be null or whitespace.</param>
    /// <param name="catalogEntryId">Affected catalog entry ID. Must not be empty.</param>
    /// <param name="details">Optional additional context.</param>
    /// <returns>A new <see cref="StaffAuditLogEntry"/> with a generated ID and current UTC timestamp.</returns>
    public static StaffAuditLogEntry Create(
        Guid staffUserId,
        string action,
        Guid catalogEntryId,
        string? details = null)
    {
        if (staffUserId == Guid.Empty)
            throw new ArgumentException("StaffUserId required.", nameof(staffUserId));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action required.", nameof(action));
        if (catalogEntryId == Guid.Empty)
            throw new ArgumentException("CatalogEntryId required.", nameof(catalogEntryId));

        return new StaffAuditLogEntry
        {
            Id = Guid.NewGuid(),
            StaffUserId = staffUserId,
            Action = action,
            CatalogEntryId = catalogEntryId,
            Timestamp = DateTimeOffset.UtcNow,
            Details = details
        };
    }
}
