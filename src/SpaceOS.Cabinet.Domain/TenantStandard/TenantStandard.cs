namespace SpaceOS.Cabinet.Domain;

using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Events;

/// <summary>
/// Tenant-scoped cabinet construction standards aggregate (§3.1).
/// One row per tenant per module. Controls material defaults, line-bore configuration,
/// advisory rule thresholds and per-rule severity overrides.
/// </summary>
public sealed class TenantStandard : AggregateRoot
{
    /// <summary>Unique identifier of this TenantStandard record.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant identifier.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Default carcass and back-panel material specifications.</summary>
    public MaterialDefaults Materials { get; private set; } = null!;

    /// <summary>Default back-panel attachment method.</summary>
    public BackPanelAttachmentDefault BackPanelAttachment { get; private set; }

    /// <summary>Default cabinet top construction type.</summary>
    public TopType TopType { get; private set; }

    /// <summary>Line-bore configuration defaults.</summary>
    public LineBoreSettings LineBore { get; private set; } = null!;

    /// <summary>Advisory rule thresholds (tall cabinet height, long shelf).</summary>
    public RuleThresholds Thresholds { get; private set; } = null!;

    private readonly Dictionary<string, AdvisorySeverity> _ruleSeverityOverrides = new();

    /// <summary>Per-rule severity overrides keyed by rule identifier.</summary>
    public IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides
        => _ruleSeverityOverrides.AsReadOnly();

    /// <summary>UTC timestamp when this standard was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>UTC timestamp of the last mutation.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Optimistic concurrency version counter; incremented on every mutation.</summary>
    public long Version { get; private set; }

    private TenantStandard() { }  // EF Core / deserialization

    /// <summary>
    /// Creates a new <see cref="TenantStandard"/> for the given tenant.
    /// </summary>
    /// <param name="tenantId">Owning tenant. Must not be <see cref="Guid.Empty"/>.</param>
    /// <param name="materials">Default material specifications.</param>
    /// <param name="backPanelAttachment">Default back-panel attachment method.</param>
    /// <param name="topType">Default top construction type.</param>
    /// <param name="lineBore">Line-bore configuration defaults.</param>
    /// <param name="thresholds">Advisory rule thresholds.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <returns>Success with the new aggregate, or a validation result.</returns>
    public static Result<TenantStandard> Create(
        Guid tenantId,
        MaterialDefaults materials,
        BackPanelAttachmentDefault backPanelAttachment,
        TopType topType,
        LineBoreSettings lineBore,
        RuleThresholds thresholds,
        Guid actorUserId)
    {
        if (tenantId == Guid.Empty)
            return Result<TenantStandard>.Invalid(new ValidationError("TenantId required."));
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(lineBore);
        ArgumentNullException.ThrowIfNull(thresholds);

        var now = DateTimeOffset.UtcNow;
        var ts = new TenantStandard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Materials = materials,
            BackPanelAttachment = backPanelAttachment,
            TopType = topType,
            LineBore = lineBore,
            Thresholds = thresholds,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };
        ts.RaiseEvent(new TenantStandardCreated(
            ts.Id, tenantId, actorUserId, now.UtcDateTime, ts.NextSeq()));
        return Result<TenantStandard>.Success(ts);
    }

    /// <summary>
    /// Updates the material defaults with optimistic concurrency check.
    /// </summary>
    /// <param name="materials">New material defaults.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="expectedVersion">Version the caller expects; must match <see cref="Version"/>.</param>
    public Result UpdateMaterials(MaterialDefaults materials, Guid actorUserId, long expectedVersion)
    {
        if (Version != expectedVersion)
            return Result.Error("TenantStandard version mismatch.");
        ArgumentNullException.ThrowIfNull(materials);
        Materials = materials;
        BumpVersion();
        RaiseEvent(new TenantStandardMaterialsUpdated(
            Id, TenantId, materials, actorUserId, UpdatedAt.UtcDateTime, NextSeq()));
        return Result.Success();
    }

    /// <summary>
    /// Updates the line-bore settings with optimistic concurrency check.
    /// </summary>
    /// <param name="lineBore">New line-bore settings.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="expectedVersion">Version the caller expects; must match <see cref="Version"/>.</param>
    public Result UpdateLineBore(LineBoreSettings lineBore, Guid actorUserId, long expectedVersion)
    {
        if (Version != expectedVersion)
            return Result.Error("TenantStandard version mismatch.");
        ArgumentNullException.ThrowIfNull(lineBore);
        LineBore = lineBore;
        BumpVersion();
        RaiseEvent(new TenantStandardLineBoreUpdated(
            Id, TenantId, lineBore, actorUserId, UpdatedAt.UtcDateTime, NextSeq()));
        return Result.Success();
    }

    /// <summary>
    /// Updates the advisory rule thresholds with optimistic concurrency check.
    /// </summary>
    /// <param name="thresholds">New rule thresholds.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="expectedVersion">Version the caller expects; must match <see cref="Version"/>.</param>
    public Result UpdateThresholds(RuleThresholds thresholds, Guid actorUserId, long expectedVersion)
    {
        if (Version != expectedVersion)
            return Result.Error("TenantStandard version mismatch.");
        ArgumentNullException.ThrowIfNull(thresholds);
        Thresholds = thresholds;
        BumpVersion();
        RaiseEvent(new TenantStandardThresholdsUpdated(
            Id, TenantId, thresholds, actorUserId, UpdatedAt.UtcDateTime, NextSeq()));
        return Result.Success();
    }

    /// <summary>
    /// Updates back-panel attachment and top-type construction defaults with optimistic concurrency check.
    /// </summary>
    /// <param name="bpa">New back-panel attachment default.</param>
    /// <param name="tt">New top type default.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="expectedVersion">Version the caller expects; must match <see cref="Version"/>.</param>
    public Result UpdateConstructionDefaults(
        BackPanelAttachmentDefault bpa, TopType tt,
        Guid actorUserId, long expectedVersion)
    {
        if (Version != expectedVersion)
            return Result.Error("TenantStandard version mismatch.");
        BackPanelAttachment = bpa;
        TopType = tt;
        BumpVersion();
        RaiseEvent(new TenantStandardConstructionDefaultsUpdated(
            Id, TenantId, bpa, tt, actorUserId, UpdatedAt.UtcDateTime, NextSeq()));
        return Result.Success();
    }

    /// <summary>
    /// Adds or replaces a per-rule severity override with optimistic concurrency check.
    /// </summary>
    /// <param name="ruleId">Non-empty rule identifier.</param>
    /// <param name="severity">Desired override severity.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="expectedVersion">Version the caller expects; must match <see cref="Version"/>.</param>
    public Result OverrideRuleSeverity(
        string ruleId, AdvisorySeverity severity,
        Guid actorUserId, long expectedVersion)
    {
        if (Version != expectedVersion)
            return Result.Error("TenantStandard version mismatch.");
        if (string.IsNullOrWhiteSpace(ruleId))
            return Result.Invalid(new ValidationError("RuleId required."));
        _ruleSeverityOverrides[ruleId] = severity;
        BumpVersion();
        RaiseEvent(new TenantStandardRuleSeverityOverridden(
            Id, TenantId, ruleId, severity, actorUserId, UpdatedAt.UtcDateTime, NextSeq()));
        return Result.Success();
    }

    /// <summary>
    /// Removes an existing per-rule severity override with optimistic concurrency check.
    /// </summary>
    /// <param name="ruleId">Rule identifier of the override to remove.</param>
    /// <param name="actorUserId">User performing the operation.</param>
    /// <param name="expectedVersion">Version the caller expects; must match <see cref="Version"/>.</param>
    public Result ClearRuleSeverityOverride(
        string ruleId, Guid actorUserId, long expectedVersion)
    {
        if (Version != expectedVersion)
            return Result.Error("TenantStandard version mismatch.");
        if (!_ruleSeverityOverrides.Remove(ruleId))
            return Result.Invalid(new ValidationError($"Override for rule '{ruleId}' not found."));
        BumpVersion();
        RaiseEvent(new TenantStandardRuleSeverityCleared(
            Id, TenantId, ruleId, actorUserId, UpdatedAt.UtcDateTime, NextSeq()));
        return Result.Success();
    }

    private void BumpVersion()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }
}
