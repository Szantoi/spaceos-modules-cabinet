namespace SpaceOS.Cabinet.Catalog;

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog.Events;

/// <summary>
/// Aggregate root representing a versioned catalog standard entry.
/// Implements the 5-state lifecycle FSM: Draft → Submitted → Approved → Published → Deprecated.
/// Rejected is a terminal dead-end from Submitted (entry must be re-created as a new Draft).
/// </summary>
public sealed class CatalogEntry
{
    /// <summary>Maximum payload size in bytes (SEC-CAB02-5).</summary>
    public const int MaxPayloadSizeBytes = 64 * 1024;

    /// <summary>Maximum length of the entry name.</summary>
    public const int MaxNameLength = 100;

    /// <summary>Maximum length of the entry description.</summary>
    public const int MaxDescriptionLength = 500;

    private static readonly Regex PayloadSchemaVersionRegex =
        new(@"^[a-z][a-z0-9_]*/v\d+$", RegexOptions.Compiled);

    /// <summary>Unique identifier of this catalog entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant identifier.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The kind of standard this entry represents.</summary>
    public CatalogType Type { get; private set; }

    /// <summary>Human-readable name.</summary>
    public string Name { get; private set; }

    /// <summary>Optional description.</summary>
    public string Description { get; private set; }

    /// <summary>Visibility scope that controls who can resolve this entry.</summary>
    public CatalogVisibility Visibility { get; private set; }

    /// <summary>Current FSM state.</summary>
    public CatalogLifecycleState State { get; private set; }

    /// <summary>Raw JSON payload conforming to <see cref="PayloadSchemaVersion"/>.</summary>
    public string PayloadJson { get; private set; }

    /// <summary>Schema version identifier (e.g. "horizontalRole/v1").</summary>
    public string PayloadSchemaVersion { get; private set; }

    /// <summary>SHA-256 hex digest of <see cref="PayloadJson"/> (lower-case, no dashes).</summary>
    public string ContentHash { get; private set; }

    /// <summary>Monotonically increasing version counter; incremented on every state transition.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Actor who created this entry.</summary>
    public Guid CreatedBy { get; private set; }

    /// <summary>UTC timestamp of the last mutation.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Actor who performed the last mutation.</summary>
    public Guid UpdatedBy { get; private set; }

    /// <summary>UTC timestamp when the entry was first published; null until then.</summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>UTC timestamp when the entry was deprecated; null until then.</summary>
    public DateTimeOffset? DeprecatedAt { get; private set; }

    // ── Cabinet 0.3 federation fields ───────────────────────────────────────────

    /// <summary>Server-computed similarity fingerprint (SEC-02). Never client-supplied.</summary>
    public string? SimilarityFingerprint { get; private set; }

    /// <summary>FK to owning <see cref="CatalogEntryCluster"/> (null if not clusterable).</summary>
    public Guid? ClusterId { get; private set; }

    /// <summary>SEC-03: time-bounded admin acknowledgment for flag threshold bypass.</summary>
    public DateTimeOffset? AdminAcknowledgedUntil { get; private set; }

    /// <summary>
    /// True when ≥3 active flags exist and the entry is not within an active admin acknowledgment window.
    /// Computed in-memory; generated column in DB.
    /// </summary>
    public bool IsAutoHidden => ActiveFlagCount >= 3
        && (AdminAcknowledgedUntil is null || AdminAcknowledgedUntil < DateTimeOffset.UtcNow);

    /// <summary>Denormalized rating rollup (aggregate-method primary, DB trigger defense-in-depth).</summary>
    public RatingAggregate Ratings { get; private set; } = RatingAggregate.Empty;

    /// <summary>Count of active (non-resolved) flags against this entry.</summary>
    public int ActiveFlagCount { get; private set; }

    private readonly List<ICatalogDomainEvent> _domainEvents = new();

    /// <summary>Uncommitted domain events raised since the last <see cref="PopDomainEvents"/> call.</summary>
    public IReadOnlyList<ICatalogDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private CatalogEntry()
    {
        Name = string.Empty;
        Description = string.Empty;
        PayloadJson = string.Empty;
        PayloadSchemaVersion = string.Empty;
        ContentHash = string.Empty;
    }

    /// <summary>
    /// Creates a new <see cref="CatalogEntry"/> in <see cref="CatalogLifecycleState.Draft"/> state.
    /// </summary>
    /// <param name="tenantId">Owning tenant. Must equal <see cref="SystemCatalog.TenantId"/> when <paramref name="visibility"/> is <see cref="CatalogVisibility.Curated"/>.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="type">Catalog entry type.</param>
    /// <param name="name">Display name (1–100 chars).</param>
    /// <param name="description">Optional description (0–500 chars).</param>
    /// <param name="visibility">Visibility scope.</param>
    /// <param name="payloadJson">JSON payload (must pass <paramref name="validator"/> and be ≤ 64 KB).</param>
    /// <param name="payloadSchemaVersion">Schema version string matching <c>^[a-z][a-z0-9_]*/v\d+$</c>.</param>
    /// <param name="validator">Payload validator.</param>
    /// <returns>A successful result with the new entry, or a validation/error result.</returns>
    public static Result<CatalogEntry> CreateDraft(
        Guid tenantId,
        Guid actorUserId,
        CatalogType type,
        string name,
        string? description,
        CatalogVisibility visibility,
        string payloadJson,
        string payloadSchemaVersion,
        ICatalogPayloadValidator validator)
    {
        if (visibility == CatalogVisibility.Curated && tenantId != SystemCatalog.TenantId)
            return Result<CatalogEntry>.Error("Curated visibility requires SystemCatalog.TenantId.");

        if (string.IsNullOrWhiteSpace(name))
            return Result<CatalogEntry>.Invalid(new ValidationError("Name is required."));

        if (name.Length > MaxNameLength)
            return Result<CatalogEntry>.Invalid(new ValidationError($"Name must be <= {MaxNameLength} characters."));

        var desc = description ?? string.Empty;
        if (desc.Length > MaxDescriptionLength)
            return Result<CatalogEntry>.Invalid(new ValidationError($"Description must be <= {MaxDescriptionLength} characters."));

        if (!PayloadSchemaVersionRegex.IsMatch(payloadSchemaVersion))
            return Result<CatalogEntry>.Invalid(new ValidationError($"Invalid PayloadSchemaVersion format: '{payloadSchemaVersion}'."));

        if (Encoding.UTF8.GetByteCount(payloadJson) > MaxPayloadSizeBytes)
            return Result<CatalogEntry>.Invalid(new ValidationError($"PayloadJson exceeds {MaxPayloadSizeBytes} byte limit."));

        var validationResult = validator.Validate(type, payloadSchemaVersion, payloadJson);
        if (!validationResult.IsSuccess)
            return Result<CatalogEntry>.Error(string.Join("; ", validationResult.Errors));

        var now = DateTimeOffset.UtcNow;
        var entry = new CatalogEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Name = name,
            Description = desc,
            Visibility = visibility,
            State = CatalogLifecycleState.Draft,
            PayloadJson = payloadJson,
            PayloadSchemaVersion = payloadSchemaVersion,
            ContentHash = ComputeHash(payloadJson),
            Version = 1,
            CreatedAt = now,
            CreatedBy = actorUserId,
            UpdatedAt = now,
            UpdatedBy = actorUserId
        };

        entry._domainEvents.Add(new CatalogEntryCreated(entry.Id, tenantId, type, actorUserId, now));
        return Result<CatalogEntry>.Success(entry);
    }

    /// <summary>
    /// Transitions from <see cref="CatalogLifecycleState.Draft"/> to <see cref="CatalogLifecycleState.Submitted"/>.
    /// Re-validates and re-hashes the payload.
    /// </summary>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="validator">Payload validator (re-run on submit).</param>
    public Result Submit(Guid actorUserId, ICatalogPayloadValidator validator)
    {
        if (State != CatalogLifecycleState.Draft)
            return Result.Error($"Cannot submit from state '{State}'. Only Draft entries can be submitted.");

        var validationResult = validator.Validate(Type, PayloadSchemaVersion, PayloadJson);
        if (!validationResult.IsSuccess)
            return Result.Error(string.Join("; ", validationResult.Errors));

        if (Encoding.UTF8.GetByteCount(PayloadJson) > MaxPayloadSizeBytes)
            return Result.Invalid(new ValidationError("PayloadJson exceeds size limit."));

        ContentHash = ComputeHash(PayloadJson);
        State = CatalogLifecycleState.Submitted;
        UpdatedBy = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        _domainEvents.Add(new CatalogEntrySubmitted(Id, ContentHash, actorUserId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Transitions from <see cref="CatalogLifecycleState.Submitted"/> to <see cref="CatalogLifecycleState.Approved"/>.
    /// </summary>
    /// <param name="actorUserId">Approving user.</param>
    public Result Approve(Guid actorUserId)
    {
        if (State != CatalogLifecycleState.Submitted)
            return Result.Error($"Cannot approve from state '{State}'. Only Submitted entries can be approved.");

        State = CatalogLifecycleState.Approved;
        UpdatedBy = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        _domainEvents.Add(new CatalogEntryApproved(Id, actorUserId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Transitions from <see cref="CatalogLifecycleState.Submitted"/> to <see cref="CatalogLifecycleState.Rejected"/>.
    /// </summary>
    /// <param name="actorUserId">Rejecting user.</param>
    /// <param name="reason">Non-empty rejection reason visible to the entry owner.</param>
    public Result Reject(Guid actorUserId, string reason)
    {
        if (State != CatalogLifecycleState.Submitted)
            return Result.Error($"Cannot reject from state '{State}'. Only Submitted entries can be rejected.");

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Invalid(new ValidationError("Rejection reason is required."));

        State = CatalogLifecycleState.Rejected;
        UpdatedBy = actorUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        _domainEvents.Add(new CatalogEntryRejected(Id, reason, actorUserId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Transitions from <see cref="CatalogLifecycleState.Approved"/> to <see cref="CatalogLifecycleState.Published"/>.
    /// </summary>
    /// <param name="actorUserId">Publishing user.</param>
    public Result Publish(Guid actorUserId)
    {
        if (State != CatalogLifecycleState.Approved)
            return Result.Error($"Cannot publish from state '{State}'. Only Approved entries can be published.");

        var now = DateTimeOffset.UtcNow;
        State = CatalogLifecycleState.Published;
        PublishedAt = now;
        UpdatedBy = actorUserId;
        UpdatedAt = now;
        Version++;
        _domainEvents.Add(new CatalogEntryPublished(Id, ContentHash, actorUserId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// Transitions from <see cref="CatalogLifecycleState.Published"/> to <see cref="CatalogLifecycleState.Deprecated"/>.
    /// </summary>
    /// <param name="actorUserId">Deprecating user.</param>
    public Result Deprecate(Guid actorUserId)
    {
        if (State != CatalogLifecycleState.Published)
            return Result.Error($"Cannot deprecate from state '{State}'. Only Published entries can be deprecated.");

        var now = DateTimeOffset.UtcNow;
        State = CatalogLifecycleState.Deprecated;
        DeprecatedAt = now;
        UpdatedBy = actorUserId;
        UpdatedAt = now;
        Version++;
        _domainEvents.Add(new CatalogEntryDeprecated(Id, actorUserId, UpdatedAt));
        return Result.Success();
    }

    /// <summary>
    /// SEC-02: server-side assignment only — assigns the server-computed similarity fingerprint
    /// and the owning cluster reference. Never call this from a client-supplied DTO.
    /// </summary>
    /// <param name="fingerprint">Normalized fingerprint (null if not clusterable).</param>
    /// <param name="clusterId">FK to the owning cluster (null if not clusterable).</param>
    public Result AssignFingerprintAndCluster(string? fingerprint, Guid? clusterId)
    {
        SimilarityFingerprint = fingerprint;
        ClusterId = clusterId;
        return Result.Success();
    }

    /// <summary>
    /// Ingests a <see cref="CatalogEntryRating"/> into the rolling rating aggregate (BE-06, aggregate-method primary).
    /// Pass <paramref name="oldStars"/> when the caller is re-rating (replaces a previous vote).
    /// </summary>
    /// <param name="rating">The rating to ingest. Must belong to this entry.</param>
    /// <param name="oldStars">Previous star value when re-rating; <c>null</c> for a first-time rating.</param>
    public Result IngestRating(CatalogEntryRating rating, int? oldStars = null)
    {
        if (rating.CatalogEntryId != Id)
            return Result.Invalid(new ValidationError("Rating does not belong to this entry."));

        if (oldStars.HasValue && Ratings.Count > 0)
        {
            var newAvg = ((Ratings.AverageStars * Ratings.Count) - oldStars.Value + rating.Stars)
                         / Ratings.Count;
            Ratings = Ratings with { AverageStars = Math.Round(newAvg, 2), LastRatedAt = DateTimeOffset.UtcNow };
        }
        else
        {
            var newCount = Ratings.Count + 1;
            var newAvg = ((Ratings.AverageStars * Ratings.Count) + rating.Stars) / newCount;
            Ratings = new RatingAggregate(newCount, Math.Round(newAvg, 2), DateTimeOffset.UtcNow);
        }

        return Result.Success();
    }

    /// <summary>
    /// Ingests a <see cref="CatalogEntryFlag"/> and increments the active flag count (BE-06, aggregate-method primary).
    /// </summary>
    /// <param name="flag">The flag to ingest. Must belong to this entry.</param>
    public Result IngestFlag(CatalogEntryFlag flag)
    {
        if (flag.CatalogEntryId != Id)
            return Result.Invalid(new ValidationError("Flag does not belong to this entry."));
        ActiveFlagCount++;
        return Result.Success();
    }

    /// <summary>
    /// SEC-03: time-bounded admin flag acknowledgment. Suppresses <see cref="IsAutoHidden"/> until
    /// <paramref name="ackDuration"/> elapses (default 90 days, maximum 365 days).
    /// </summary>
    /// <param name="adminUserId">Admin user performing the acknowledgment.</param>
    /// <param name="ackDuration">Acknowledgment window; defaults to 90 days if <c>null</c>.</param>
    public Result ClearFlagsByAdmin(Guid adminUserId, TimeSpan? ackDuration = null)
    {
        var window = ackDuration ?? TimeSpan.FromDays(90);
        if (window > TimeSpan.FromDays(365))
            return Result.Invalid(new ValidationError("Acknowledgment window cannot exceed 365 days."));
        AdminAcknowledgedUntil = DateTimeOffset.UtcNow.Add(window);
        return Result.Success();
    }

    /// <summary>
    /// Community UPSERT path: updates the entry's mutable fields and transitions it to
    /// <see cref="CatalogLifecycleState.Submitted"/> regardless of its current state.
    /// Allowed from any non-terminal state (Draft, Submitted, Approved, Published, Deprecated).
    /// Re-hashes the payload; re-runs validation; increments version.
    /// </summary>
    /// <param name="name">New display name (1–100 chars).</param>
    /// <param name="description">New description (0–500 chars).</param>
    /// <param name="visibility">New visibility scope.</param>
    /// <param name="payloadJson">Updated JSON payload (must pass <paramref name="validator"/>).</param>
    /// <param name="payloadSchemaVersion">Schema version identifier.</param>
    /// <param name="actorUserId">User performing the UPSERT.</param>
    /// <param name="validator">Payload validator (re-run to guard against schema drift).</param>
    public Result UpdateAndResubmit(
        string name,
        string? description,
        CatalogVisibility visibility,
        string payloadJson,
        string payloadSchemaVersion,
        Guid actorUserId,
        ICatalogPayloadValidator validator)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Invalid(new ValidationError("Name is required."));

        if (name.Length > MaxNameLength)
            return Result.Invalid(new ValidationError($"Name must be <= {MaxNameLength} characters."));

        var desc = description ?? string.Empty;
        if (desc.Length > MaxDescriptionLength)
            return Result.Invalid(new ValidationError($"Description must be <= {MaxDescriptionLength} characters."));

        if (!PayloadSchemaVersionRegex.IsMatch(payloadSchemaVersion))
            return Result.Invalid(new ValidationError($"Invalid PayloadSchemaVersion format: '{payloadSchemaVersion}'."));

        if (Encoding.UTF8.GetByteCount(payloadJson) > MaxPayloadSizeBytes)
            return Result.Invalid(new ValidationError($"PayloadJson exceeds {MaxPayloadSizeBytes} byte limit."));

        var validationResult = validator.Validate(Type, payloadSchemaVersion, payloadJson);
        if (!validationResult.IsSuccess)
            return Result.Error(string.Join("; ", validationResult.Errors));

        var now = DateTimeOffset.UtcNow;
        Name = name;
        Description = desc;
        Visibility = visibility;
        PayloadJson = payloadJson;
        PayloadSchemaVersion = payloadSchemaVersion;
        ContentHash = ComputeHash(payloadJson);
        State = CatalogLifecycleState.Submitted;
        UpdatedBy = actorUserId;
        UpdatedAt = now;
        Version++;

        _domainEvents.Add(new CatalogEntrySubmitted(Id, ContentHash, actorUserId, now));
        return Result.Success();
    }

    /// <summary>
    /// Returns all uncommitted domain events and clears the internal list.
    /// Call this after persisting the aggregate.
    /// </summary>
    public IReadOnlyList<ICatalogDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList().AsReadOnly();
        _domainEvents.Clear();
        return events;
    }

    private static string ComputeHash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
