namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Domain;

/// <summary>
/// Handles <see cref="CreateTenantStandardCommand"/>: creates a new TenantStandard aggregate and persists it.
/// </summary>
public sealed class CreateTenantStandardCommandHandler
    : IRequestHandler<CreateTenantStandardCommand, Result<Guid>>
{
    private readonly ITenantStandardWriteRepository _repo;

    /// <summary>Initializes the handler with the write repository.</summary>
    public CreateTenantStandardCommandHandler(ITenantStandardWriteRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(
        CreateTenantStandardCommand request, CancellationToken cancellationToken)
    {
        var materials = new MaterialDefaults(
            request.CarcassMaterial,
            request.CarcassThicknessMm,
            request.BackPanelMaterial,
            request.BackPanelThicknessMm);

        var lineBore = new LineBoreSettings(
            request.LineBoreEnabled,
            request.FirstHoleOffsetMm,
            request.SpacingMm,
            request.DiameterMm);

        var thresholds = new RuleThresholds(
            request.TallCabinetHeightMm,
            request.LongShelfMm);

        var result = TenantStandard.Create(
            request.TenantId,
            materials,
            request.BackPanelAttachment,
            request.TopType,
            lineBore,
            thresholds,
            request.ActorUserId);

        if (!result.IsSuccess)
            return Result<Guid>.Error(string.Join("; ", result.Errors));

        await _repo.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(result.Value.Id);
    }
}

/// <summary>
/// Handles <see cref="UpdateMaterialDefaultsCommand"/>: loads the aggregate, applies the change, and saves.
/// </summary>
public sealed class UpdateMaterialDefaultsCommandHandler
    : IRequestHandler<UpdateMaterialDefaultsCommand, Result>
{
    private readonly ITenantStandardWriteRepository _repo;

    /// <summary>Initializes the handler with the write repository.</summary>
    public UpdateMaterialDefaultsCommandHandler(ITenantStandardWriteRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        UpdateMaterialDefaultsCommand request, CancellationToken cancellationToken)
    {
        var standard = await _repo.GetByIdAsync(request.StandardId, cancellationToken).ConfigureAwait(false);
        if (standard is null)
            return Result.Error($"TenantStandard {request.StandardId} not found.");

        var materials = new MaterialDefaults(
            request.CarcassMaterial,
            request.CarcassThicknessMm,
            request.BackPanelMaterial,
            request.BackPanelThicknessMm);

        var result = standard.UpdateMaterials(materials, request.ActorUserId, request.ExpectedVersion);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(standard, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="UpdateLineBoreSettingsCommand"/>: loads the aggregate, applies the change, and saves.
/// </summary>
public sealed class UpdateLineBoreSettingsCommandHandler
    : IRequestHandler<UpdateLineBoreSettingsCommand, Result>
{
    private readonly ITenantStandardWriteRepository _repo;

    /// <summary>Initializes the handler with the write repository.</summary>
    public UpdateLineBoreSettingsCommandHandler(ITenantStandardWriteRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        UpdateLineBoreSettingsCommand request, CancellationToken cancellationToken)
    {
        var standard = await _repo.GetByIdAsync(request.StandardId, cancellationToken).ConfigureAwait(false);
        if (standard is null)
            return Result.Error($"TenantStandard {request.StandardId} not found.");

        var lineBore = new LineBoreSettings(
            request.Enabled,
            request.FirstHoleOffsetMm,
            request.SpacingMm,
            request.DiameterMm);

        var result = standard.UpdateLineBore(lineBore, request.ActorUserId, request.ExpectedVersion);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(standard, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="UpdateRuleThresholdsCommand"/>: loads the aggregate, applies the change, and saves.
/// </summary>
public sealed class UpdateRuleThresholdsCommandHandler
    : IRequestHandler<UpdateRuleThresholdsCommand, Result>
{
    private readonly ITenantStandardWriteRepository _repo;

    /// <summary>Initializes the handler with the write repository.</summary>
    public UpdateRuleThresholdsCommandHandler(ITenantStandardWriteRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        UpdateRuleThresholdsCommand request, CancellationToken cancellationToken)
    {
        var standard = await _repo.GetByIdAsync(request.StandardId, cancellationToken).ConfigureAwait(false);
        if (standard is null)
            return Result.Error($"TenantStandard {request.StandardId} not found.");

        var thresholds = new RuleThresholds(request.TallCabinetHeightMm, request.LongShelfMm);
        var result = standard.UpdateThresholds(thresholds, request.ActorUserId, request.ExpectedVersion);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(standard, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="OverrideRuleSeverityCommand"/>: loads the aggregate, applies the override, and saves.
/// </summary>
public sealed class OverrideRuleSeverityCommandHandler
    : IRequestHandler<OverrideRuleSeverityCommand, Result>
{
    private readonly ITenantStandardWriteRepository _repo;

    /// <summary>Initializes the handler with the write repository.</summary>
    public OverrideRuleSeverityCommandHandler(ITenantStandardWriteRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        OverrideRuleSeverityCommand request, CancellationToken cancellationToken)
    {
        var standard = await _repo.GetByIdAsync(request.StandardId, cancellationToken).ConfigureAwait(false);
        if (standard is null)
            return Result.Error($"TenantStandard {request.StandardId} not found.");

        var result = standard.OverrideRuleSeverity(
            request.RuleId, request.Severity, request.ActorUserId, request.ExpectedVersion);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(standard, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
