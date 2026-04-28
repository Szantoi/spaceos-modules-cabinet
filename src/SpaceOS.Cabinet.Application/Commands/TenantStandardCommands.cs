namespace SpaceOS.Cabinet.Application.Commands;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Abstractions;

/// <summary>Creates a new <see cref="SpaceOS.Cabinet.Domain.TenantStandard"/> for the given tenant.</summary>
public sealed record CreateTenantStandardCommand(
    Guid TenantId,
    string CarcassMaterial,
    double CarcassThicknessMm,
    string BackPanelMaterial,
    double BackPanelThicknessMm,
    BackPanelAttachmentDefault BackPanelAttachment,
    TopType TopType,
    bool LineBoreEnabled,
    double FirstHoleOffsetMm,
    double SpacingMm,
    double DiameterMm,
    double TallCabinetHeightMm,
    double LongShelfMm,
    Guid ActorUserId) : IRequest<Result<Guid>>;

/// <summary>Updates the material defaults on an existing <see cref="SpaceOS.Cabinet.Domain.TenantStandard"/>.</summary>
public sealed record UpdateMaterialDefaultsCommand(
    Guid StandardId,
    Guid TenantId,
    string CarcassMaterial,
    double CarcassThicknessMm,
    string BackPanelMaterial,
    double BackPanelThicknessMm,
    Guid ActorUserId,
    long ExpectedVersion) : IRequest<Result>;

/// <summary>Updates the line-bore settings on an existing <see cref="SpaceOS.Cabinet.Domain.TenantStandard"/>.</summary>
public sealed record UpdateLineBoreSettingsCommand(
    Guid StandardId,
    Guid TenantId,
    bool Enabled,
    double FirstHoleOffsetMm,
    double SpacingMm,
    double DiameterMm,
    Guid ActorUserId,
    long ExpectedVersion) : IRequest<Result>;

/// <summary>Updates the advisory rule thresholds on an existing <see cref="SpaceOS.Cabinet.Domain.TenantStandard"/>.</summary>
public sealed record UpdateRuleThresholdsCommand(
    Guid StandardId,
    Guid TenantId,
    double TallCabinetHeightMm,
    double LongShelfMm,
    Guid ActorUserId,
    long ExpectedVersion) : IRequest<Result>;

/// <summary>Adds or replaces a per-rule severity override on an existing <see cref="SpaceOS.Cabinet.Domain.TenantStandard"/>.</summary>
public sealed record OverrideRuleSeverityCommand(
    Guid StandardId,
    Guid TenantId,
    string RuleId,
    AdvisorySeverity Severity,
    Guid ActorUserId,
    long ExpectedVersion) : IRequest<Result>;
