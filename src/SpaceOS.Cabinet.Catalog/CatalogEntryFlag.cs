namespace SpaceOS.Cabinet.Catalog;

using System.Text.RegularExpressions;
using Ardalis.Result;

/// <summary>
/// Records a content-moderation flag raised by a tenant against a <see cref="CatalogEntry"/>
/// owned by another tenant. PII is stripped from the free-text note on creation.
/// Self-flagging is forbidden.
/// </summary>
public sealed class CatalogEntryFlag
{
    private static readonly Regex EmailRegex =
        new(@"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}",
            RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    private static readonly Regex PhoneRegex =
        new(@"(\+?36|06)[\s-]?\d{1,2}[\s-]?\d{3}[\s-]?\d{4}",
            RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    /// <summary>Unique identifier of this flag.</summary>
    public Guid Id { get; private set; }

    /// <summary>The flagged catalog entry.</summary>
    public Guid CatalogEntryId { get; private set; }

    /// <summary>Tenant that submitted the flag.</summary>
    public Guid ReporterTenantId { get; private set; }

    /// <summary>User within <see cref="ReporterTenantId"/> who submitted the flag.</summary>
    public Guid ReporterUserId { get; private set; }

    /// <summary>Reason selected by the reporter.</summary>
    public FlagReason Reason { get; private set; }

    /// <summary>Optional free-text note with PII stripped (emails → [email], phones → [phone]).</summary>
    public string? Note { get; private set; }

    /// <summary>Current resolution state of this flag.</summary>
    public FlagState State { get; private set; }

    /// <summary>UTC timestamp when the flag was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>UTC timestamp when the flag was resolved; null if still active.</summary>
    public DateTimeOffset? ResolvedAt { get; private set; }

    /// <summary>Admin user who resolved the flag; null if not yet resolved.</summary>
    public Guid? ResolvedByUserId { get; private set; }

    private CatalogEntryFlag() { }

    private static string? StripPii(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        input = EmailRegex.Replace(input, "[email]");
        input = PhoneRegex.Replace(input, "[phone]");
        return input.Length > 1000 ? input[..1000] : input;
    }

    /// <summary>
    /// Creates a new <see cref="CatalogEntryFlag"/>. PII in <paramref name="note"/> is automatically
    /// stripped and the note is truncated to 1000 characters.
    /// </summary>
    /// <param name="catalogEntryId">The catalog entry being flagged.</param>
    /// <param name="reporterTenantId">Tenant submitting the flag. Must not equal <paramref name="entryOwnerTenantId"/>.</param>
    /// <param name="reporterUserId">User submitting the flag.</param>
    /// <param name="reason">The selected reason for the flag.</param>
    /// <param name="note">Optional free-text note (PII is stripped, max 1000 chars after stripping).</param>
    /// <param name="entryOwnerTenantId">Owning tenant of the entry (used for self-flag guard).</param>
    /// <returns>Success with the new flag, or a validation result.</returns>
    public static Result<CatalogEntryFlag> Create(
        Guid catalogEntryId,
        Guid reporterTenantId,
        Guid reporterUserId,
        FlagReason reason,
        string? note,
        Guid entryOwnerTenantId)
    {
        if (reporterTenantId == entryOwnerTenantId)
            return Result<CatalogEntryFlag>.Invalid(new ValidationError("Cannot flag own entry."));

        return Result<CatalogEntryFlag>.Success(new CatalogEntryFlag
        {
            Id = Guid.NewGuid(),
            CatalogEntryId = catalogEntryId,
            ReporterTenantId = reporterTenantId,
            ReporterUserId = reporterUserId,
            Reason = reason,
            Note = StripPii(note),
            State = FlagState.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Resolves the flag with the given state and records the resolving admin.
    /// </summary>
    /// <param name="newState">New resolution state (must not be <see cref="FlagState.Active"/>).</param>
    /// <param name="adminUserId">Admin user performing the resolution.</param>
    public void Resolve(FlagState newState, Guid adminUserId)
    {
        State = newState;
        ResolvedAt = DateTimeOffset.UtcNow;
        ResolvedByUserId = adminUserId;
    }
}
