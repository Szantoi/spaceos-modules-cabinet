namespace SpaceOS.Cabinet.Domain.Events;

using SpaceOS.Cabinet.Abstractions;

/// <summary>Raised when a new <see cref="TenantStandard"/> is created.</summary>
public sealed record TenantStandardCreated(
    Guid TenantStandardId,
    Guid TenantId,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when the material defaults of a <see cref="TenantStandard"/> are updated.</summary>
public sealed record TenantStandardMaterialsUpdated(
    Guid TenantStandardId,
    Guid TenantId,
    MaterialDefaults NewMaterials,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when the line-bore settings of a <see cref="TenantStandard"/> are updated.</summary>
public sealed record TenantStandardLineBoreUpdated(
    Guid TenantStandardId,
    Guid TenantId,
    LineBoreSettings NewSettings,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when the rule thresholds of a <see cref="TenantStandard"/> are updated.</summary>
public sealed record TenantStandardThresholdsUpdated(
    Guid TenantStandardId,
    Guid TenantId,
    RuleThresholds NewThresholds,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when the construction defaults (back-panel attachment, top type) are updated.</summary>
public sealed record TenantStandardConstructionDefaultsUpdated(
    Guid TenantStandardId,
    Guid TenantId,
    BackPanelAttachmentDefault NewBpa,
    TopType NewTopType,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when a per-rule severity override is added or changed.</summary>
public sealed record TenantStandardRuleSeverityOverridden(
    Guid TenantStandardId,
    Guid TenantId,
    string RuleId,
    AdvisorySeverity Severity,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when a per-rule severity override is cleared.</summary>
public sealed record TenantStandardRuleSeverityCleared(
    Guid TenantStandardId,
    Guid TenantId,
    string RuleId,
    Guid ActorUserId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;
