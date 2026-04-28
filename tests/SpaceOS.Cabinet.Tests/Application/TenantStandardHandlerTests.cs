using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Application.Handlers;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Application.Validators;
using SpaceOS.Cabinet.Tests.Infrastructure;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Application;

/// <summary>
/// Unit tests for TenantStandard CQRS handlers and validators using an in-memory repository.
/// </summary>
public class TenantStandardHandlerTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    private static CreateTenantStandardCommand ValidCreateCommand(Guid? tenantId = null) =>
        new(
            TenantId: tenantId ?? TenantId,
            CarcassMaterial: "MDF",
            CarcassThicknessMm: 18.0,
            BackPanelMaterial: "HDF",
            BackPanelThicknessMm: 5.0,
            BackPanelAttachment: BackPanelAttachmentDefault.Groove,
            TopType: TopType.FullTop,
            LineBoreEnabled: true,
            FirstHoleOffsetMm: 37.0,
            SpacingMm: 32.0,
            DiameterMm: 5.0,
            TallCabinetHeightMm: 2100.0,
            LongShelfMm: 800.0,
            ActorUserId: ActorId);

    private static (InMemoryTenantStandardRepository repo, CreateTenantStandardCommandHandler handler)
        MakeCreateHandler()
    {
        var repo = new InMemoryTenantStandardRepository();
        return (repo, new CreateTenantStandardCommandHandler(repo));
    }

    // ── CreateTenantStandardCommandHandler ────────────────────────────────────

    [Fact]
    public async Task CreateTenantStandardHandler_ValidCommand_ReturnsSuccessWithId()
    {
        var (_, handler) = MakeCreateHandler();

        var result = await handler.Handle(ValidCreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task CreateTenantStandardHandler_ValidCommand_PersistsAggregate()
    {
        var (repo, handler) = MakeCreateHandler();
        var cmd = ValidCreateCommand();

        var result = await handler.Handle(cmd, CancellationToken.None);

        var stored = await repo.GetByIdAsync(result.Value);
        Assert.NotNull(stored);
        Assert.Equal(TenantId, stored.TenantId);
    }

    [Fact]
    public async Task CreateTenantStandardHandler_EmptyTenantId_ReturnsError()
    {
        var (_, handler) = MakeCreateHandler();

        var result = await handler.Handle(ValidCreateCommand(tenantId: Guid.Empty), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── UpdateMaterialDefaultsCommandHandler ──────────────────────────────────

    [Fact]
    public async Task UpdateMaterialsHandler_CorrectVersion_Succeeds()
    {
        var (repo, createHandler) = MakeCreateHandler();
        var createResult = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var standard = await repo.GetByIdAsync(createResult.Value);
        var updateHandler = new UpdateMaterialDefaultsCommandHandler(repo);

        var result = await updateHandler.Handle(new UpdateMaterialDefaultsCommand(
            createResult.Value, TenantId,
            "Birch", 18.0, "Poplar", 6.0,
            ActorId, standard!.Version), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(createResult.Value);
        Assert.Equal("Birch", updated!.Materials.CarcassMaterial);
    }

    [Fact]
    public async Task UpdateMaterialsHandler_WrongVersion_ReturnsError()
    {
        var (repo, createHandler) = MakeCreateHandler();
        var createResult = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var updateHandler = new UpdateMaterialDefaultsCommandHandler(repo);

        var result = await updateHandler.Handle(new UpdateMaterialDefaultsCommand(
            createResult.Value, TenantId,
            "Birch", 18.0, "Poplar", 6.0,
            ActorId, ExpectedVersion: 999L), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateMaterialsHandler_NotFound_ReturnsError()
    {
        var repo = new InMemoryTenantStandardRepository();
        var handler = new UpdateMaterialDefaultsCommandHandler(repo);

        var result = await handler.Handle(new UpdateMaterialDefaultsCommand(
            Guid.NewGuid(), TenantId,
            "MDF", 18.0, "HDF", 5.0,
            ActorId, ExpectedVersion: 1L), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── UpdateLineBoreSettingsCommandHandler ──────────────────────────────────

    [Fact]
    public async Task UpdateLineBoreHandler_ValidCommand_Succeeds()
    {
        var (repo, createHandler) = MakeCreateHandler();
        var createResult = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var standard = await repo.GetByIdAsync(createResult.Value);
        var handler = new UpdateLineBoreSettingsCommandHandler(repo);

        var result = await handler.Handle(new UpdateLineBoreSettingsCommand(
            createResult.Value, TenantId,
            Enabled: false, 37.0, 32.0, 5.0,
            ActorId, standard!.Version), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(createResult.Value);
        Assert.False(updated!.LineBore.Enabled);
    }

    // ── UpdateRuleThresholdsCommandHandler ────────────────────────────────────

    [Fact]
    public async Task UpdateRuleThresholdsHandler_ValidCommand_Succeeds()
    {
        var (repo, createHandler) = MakeCreateHandler();
        var createResult = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var standard = await repo.GetByIdAsync(createResult.Value);
        var handler = new UpdateRuleThresholdsCommandHandler(repo);

        var result = await handler.Handle(new UpdateRuleThresholdsCommand(
            createResult.Value, TenantId,
            2200.0, 900.0,
            ActorId, standard!.Version), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(createResult.Value);
        Assert.Equal(2200.0, updated!.Thresholds.TallCabinetHeightMm);
    }

    // ── OverrideRuleSeverityCommandHandler ────────────────────────────────────

    [Fact]
    public async Task OverrideRuleSeverityHandler_ValidCommand_Succeeds()
    {
        var (repo, createHandler) = MakeCreateHandler();
        var createResult = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var standard = await repo.GetByIdAsync(createResult.Value);
        var handler = new OverrideRuleSeverityCommandHandler(repo);

        var result = await handler.Handle(new OverrideRuleSeverityCommand(
            createResult.Value, TenantId,
            "ShelfSagRule", AdvisorySeverity.Critical,
            ActorId, standard!.Version), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(createResult.Value);
        Assert.True(updated!.RuleSeverityOverrides.ContainsKey("ShelfSagRule"));
    }

    // ── GetTenantStandardQueryHandler ─────────────────────────────────────────

    [Fact]
    public async Task GetTenantStandardQuery_ExistingTenant_ReturnsSnapshot()
    {
        var (repo, createHandler) = MakeCreateHandler();
        await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var queryHandler = new GetTenantStandardQueryHandler(repo);

        var result = await queryHandler.Handle(new GetTenantStandardQuery(TenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(TenantId, result.Value!.TenantId);
    }

    [Fact]
    public async Task GetTenantStandardQuery_NonExistingTenant_ReturnsNull()
    {
        var repo = new InMemoryTenantStandardRepository();
        var handler = new GetTenantStandardQueryHandler(repo);

        var result = await handler.Handle(new GetTenantStandardQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    // ── ListTenantStandardsQueryHandler ───────────────────────────────────────

    [Fact]
    public async Task ListTenantStandardsQuery_MultipleStandards_ReturnsAll()
    {
        var repo = new InMemoryTenantStandardRepository();
        var createHandler = new CreateTenantStandardCommandHandler(repo);
        // Create two standards for the same tenant
        await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var queryHandler = new ListTenantStandardsQueryHandler(repo);

        var result = await queryHandler.Handle(new ListTenantStandardsQuery(TenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task ListTenantStandardsQuery_DifferentTenant_ReturnsEmpty()
    {
        var (repo, createHandler) = MakeCreateHandler();
        await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var queryHandler = new ListTenantStandardsQueryHandler(repo);

        var result = await queryHandler.Handle(new ListTenantStandardsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── FluentValidation: CreateTenantStandardCommandValidator ────────────────

    [Fact]
    public void Validator_CreateTenantStandard_EmptyTenantId_Invalid()
    {
        var validator = new CreateTenantStandardCommandValidator();
        var cmd = ValidCreateCommand() with { TenantId = Guid.Empty };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validator_CreateTenantStandard_EmptyActorUserId_Invalid()
    {
        var validator = new CreateTenantStandardCommandValidator();
        var cmd = ValidCreateCommand() with { ActorUserId = Guid.Empty };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
    }

    [Fact]
    public void Validator_CreateTenantStandard_EmptyCarcassMaterial_Invalid()
    {
        var validator = new CreateTenantStandardCommandValidator();
        var cmd = ValidCreateCommand() with { CarcassMaterial = "" };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CarcassMaterial");
    }

    [Fact]
    public void Validator_CreateTenantStandard_ValidCommand_Passes()
    {
        var validator = new CreateTenantStandardCommandValidator();

        var result = validator.Validate(ValidCreateCommand());

        Assert.True(result.IsValid);
    }
}
