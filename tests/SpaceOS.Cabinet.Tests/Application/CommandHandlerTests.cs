using Ardalis.Result;
using FluentValidation;
using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Application.Handlers;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Application.Validators;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Assembly;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Application;

/// <summary>
/// Unit tests for CQRS command and query handlers using in-memory repositories.
/// </summary>
public class CommandHandlerTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid StaffId = Guid.NewGuid();

    private const string ValidPayload = """{"role":"Shelf","priority":1}""";
    private const string ValidSchema = "horizontal_role/v1";

    private static CreateCatalogEntryCommand ValidCreateCommand(
        string name = "Test Entry",
        string payload = ValidPayload,
        string schema = ValidSchema) =>
        new(TenantId, CatalogType.HorizontalRole, name, "Desc",
            CatalogVisibility.Private, payload, schema, ActorId);

    private static Skeleton CreateSkeleton()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        return Skeleton.Create(TenantId, dim).Value;
    }

    private static (InMemoryCatalogRepo repo, IStaffAuditLogger audit)
        MakeRepoAndAudit() => (new InMemoryCatalogRepo(), NullStaffAuditLogger.Instance);

    private static (InMemoryCatalogRepo repo, TrackingAuditLogger audit)
        MakeRepoAndTrackingAudit() => (new InMemoryCatalogRepo(), new TrackingAuditLogger());

    // ── CreateCatalogEntryCommandHandler ──────────────────────────────────────

    [Fact]
    public async Task CreateCatalogEntryHandler_ValidCommand_ReturnsSuccessWithId()
    {
        var (repo, audit) = MakeRepoAndAudit();
        var handler = new CreateCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);

        var result = await handler.Handle(ValidCreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task CreateCatalogEntryHandler_PayloadTooLarge_ReturnsError()
    {
        var (repo, _) = MakeRepoAndAudit();
        var handler = new CreateCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);
        var bigPayload = new string('x', 70000);

        var result = await handler.Handle(ValidCreateCommand(payload: bigPayload), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task CreateCatalogEntryHandler_EmptyName_ReturnsError()
    {
        var (repo, _) = MakeRepoAndAudit();
        var handler = new CreateCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);

        var result = await handler.Handle(ValidCreateCommand(name: ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── SubmitCatalogEntryCommandHandler ──────────────────────────────────────

    [Fact]
    public async Task SubmitCatalogEntryHandler_ExistingEntry_TransitionsToSubmitted()
    {
        var (repo, audit) = MakeRepoAndAudit();
        var createHandler = new CreateCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);
        var createResult = await createHandler.Handle(ValidCreateCommand(), CancellationToken.None);
        var submitHandler = new SubmitCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);

        var result = await submitHandler.Handle(
            new SubmitCatalogEntryCommand(createResult.Value, ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = await repo.GetByIdAsync(createResult.Value);
        Assert.Equal(CatalogLifecycleState.Submitted, entry!.State);
    }

    [Fact]
    public async Task SubmitCatalogEntryHandler_NotFound_ReturnsError()
    {
        var (repo, _) = MakeRepoAndAudit();
        var handler = new SubmitCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);

        var result = await handler.Handle(
            new SubmitCatalogEntryCommand(Guid.NewGuid(), ActorId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── ApproveCatalogEntryCommandHandler ──────────────────────────────────────

    [Fact]
    public async Task ApproveCatalogEntryHandler_SubmittedEntry_TransitionsToApproved()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Submitted);
        var handler = new ApproveCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new ApproveCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.Equal(CatalogLifecycleState.Approved, updated!.State);
    }

    [Fact]
    public async Task ApproveCatalogEntryHandler_NotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new ApproveCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new ApproveCatalogEntryCommand(Guid.NewGuid(), StaffId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ApproveCatalogEntryHandler_DraftEntry_ReturnsInvalid()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Draft);
        var handler = new ApproveCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new ApproveCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        // Draft cannot be approved — FSM transition is invalid
        Assert.False(result.IsSuccess);
    }

    // ── RejectCatalogEntryCommandHandler ──────────────────────────────────────

    [Fact]
    public async Task RejectCatalogEntryHandler_SubmittedEntry_TransitionsToRejected()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Submitted);
        var handler = new RejectCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new RejectCatalogEntryCommand(entry.Id, StaffId, "Incomplete payload"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.Equal(CatalogLifecycleState.Rejected, updated!.State);
    }

    [Fact]
    public async Task RejectCatalogEntryHandler_NotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new RejectCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new RejectCatalogEntryCommand(Guid.NewGuid(), StaffId, "reason"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── PublishCatalogEntryCommandHandler ─────────────────────────────────────

    [Fact]
    public async Task PublishCatalogEntryHandler_ApprovedEntry_TransitionsToPublished()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Approved);
        var handler = new PublishCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new PublishCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.Equal(CatalogLifecycleState.Published, updated!.State);
    }

    [Fact]
    public async Task PublishCatalogEntryHandler_NotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new PublishCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new PublishCatalogEntryCommand(Guid.NewGuid(), StaffId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── DeprecateCatalogEntryCommandHandler ───────────────────────────────────

    [Fact]
    public async Task DeprecateCatalogEntryHandler_PublishedEntry_TransitionsToDeprecated()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Published);
        var handler = new DeprecateCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new DeprecateCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.Equal(CatalogLifecycleState.Deprecated, updated!.State);
    }

    [Fact]
    public async Task DeprecateCatalogEntryHandler_NotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new DeprecateCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var result = await handler.Handle(
            new DeprecateCatalogEntryCommand(Guid.NewGuid(), StaffId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── Audit logger calls ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveCatalogEntryHandler_LogsAuditEvent()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Submitted);
        var (_, tracking) = MakeRepoAndTrackingAudit();
        var handler = new ApproveCatalogEntryCommandHandler(repo, tracking);

        await handler.Handle(new ApproveCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        Assert.Single(tracking.Calls);
        Assert.Equal("Approve", tracking.Calls[0].action);
    }

    [Fact]
    public async Task PublishCatalogEntryHandler_LogsAuditEvent()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Approved);
        var (_, tracking) = MakeRepoAndTrackingAudit();
        var handler = new PublishCatalogEntryCommandHandler(repo, tracking);

        await handler.Handle(new PublishCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        Assert.Single(tracking.Calls);
        Assert.Equal("Publish", tracking.Calls[0].action);
    }

    [Fact]
    public async Task DeprecateCatalogEntryHandler_LogsAuditEvent()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Published);
        var (_, tracking) = MakeRepoAndTrackingAudit();
        var handler = new DeprecateCatalogEntryCommandHandler(repo, tracking);

        await handler.Handle(new DeprecateCatalogEntryCommand(entry.Id, StaffId), CancellationToken.None);

        Assert.Single(tracking.Calls);
        Assert.Equal("Deprecate", tracking.Calls[0].action);
    }

    [Fact]
    public async Task RejectCatalogEntryHandler_LogsAuditEvent()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Submitted);
        var (_, tracking) = MakeRepoAndTrackingAudit();
        var handler = new RejectCatalogEntryCommandHandler(repo, tracking);

        await handler.Handle(
            new RejectCatalogEntryCommand(entry.Id, StaffId, "reason"), CancellationToken.None);

        Assert.Single(tracking.Calls);
        Assert.Equal("Reject", tracking.Calls[0].action);
        Assert.Equal("reason", tracking.Calls[0].details);
    }

    // ── PinCatalogEntryCommandHandler ─────────────────────────────────────────

    [Fact]
    public async Task PinCatalogEntryHandler_ValidPartAndEntry_Succeeds()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts.First().Id;
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new PinCatalogEntryCommandHandler(repo);

        var result = await handler.Handle(
            new PinCatalogEntryCommand(skeleton.Id, partId, CatalogType.HorizontalRole,
                Guid.NewGuid(), ActorId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PinCatalogEntryHandler_NotFoundSkeleton_ReturnsError()
    {
        var repo = new InMemorySkeletonRepo();
        var handler = new PinCatalogEntryCommandHandler(repo);

        var result = await handler.Handle(
            new PinCatalogEntryCommand(Guid.NewGuid(), Guid.NewGuid(),
                CatalogType.HorizontalRole, Guid.NewGuid(), ActorId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task PinCatalogEntryHandler_InvalidPart_ReturnsError()
    {
        var skeleton = CreateSkeleton();
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new PinCatalogEntryCommandHandler(repo);

        // partId does not exist in the skeleton
        var result = await handler.Handle(
            new PinCatalogEntryCommand(skeleton.Id, Guid.NewGuid(),
                CatalogType.HorizontalRole, Guid.NewGuid(), ActorId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── DeriveAssemblyCommandHandler ──────────────────────────────────────────

    [Fact]
    public async Task DeriveAssemblyHandler_ValidSkeleton_Succeeds()
    {
        var skeleton = CreateSkeleton();
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new DeriveAssemblyCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveAssemblyCommand(skeleton.Id, ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeriveAssemblyHandler_NotFound_ReturnsError()
    {
        var repo = new InMemorySkeletonRepo();
        var handler = new DeriveAssemblyCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveAssemblyCommand(Guid.NewGuid(), ActorId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── DeriveBillOfServicesCommandHandler ────────────────────────────────────

    [Fact]
    public async Task DeriveBillOfServicesHandler_NoPins_ReturnsEmptyDto()
    {
        var skeleton = CreateSkeleton();
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new DeriveBillOfServicesCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveBillOfServicesCommand(skeleton.Id, ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task DeriveBillOfServicesHandler_WithPins_ReturnsDtoWithItems()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts.First().Id;
        skeleton.PinCatalogEntry(partId, CatalogType.HorizontalRole, Guid.NewGuid());
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new DeriveBillOfServicesCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveBillOfServicesCommand(skeleton.Id, ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(partId, result.Value.Items[0].PartId);
    }

    [Fact]
    public async Task DeriveBillOfServicesHandler_NotFound_ReturnsError()
    {
        var repo = new InMemorySkeletonRepo();
        var handler = new DeriveBillOfServicesCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveBillOfServicesCommand(Guid.NewGuid(), ActorId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── Query handlers ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalogEntryHandler_ExistingEntry_ReturnsIt()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Draft);
        var handler = new GetCatalogEntryQueryHandler(repo);

        var result = await handler.Handle(new GetCatalogEntryQuery(entry.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(entry.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetCatalogEntryHandler_NotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new GetCatalogEntryQueryHandler(repo);

        var result = await handler.Handle(new GetCatalogEntryQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ListCatalogEntriesHandler_FilterByType_ReturnsFiltered()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = await SeedEntryInState(repo, CatalogLifecycleState.Draft);
        var handler = new ListCatalogEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCatalogEntriesQuery(TenantId, CatalogType.HorizontalRole), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value, e => Assert.Equal(CatalogType.HorizontalRole, e.Type));
    }

    [Fact]
    public async Task ListCatalogEntriesHandler_FilterByState_ReturnsFiltered()
    {
        var repo = new InMemoryCatalogRepo();
        await SeedEntryInState(repo, CatalogLifecycleState.Draft);
        var handler = new ListCatalogEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCatalogEntriesQuery(TenantId, State: CatalogLifecycleState.Draft),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value, e => Assert.Equal(CatalogLifecycleState.Draft, e.State));
    }

    [Fact]
    public async Task GetAssemblyDocumentationHandler_ValidSkeleton_ReturnsSteps()
    {
        var skeleton = CreateSkeleton();
        var skeletonRepo = new InMemorySkeletonRepo(skeleton);
        var docService = new AssemblyDocumentationService();
        var handler = new GetAssemblyDocumentationQueryHandler(skeletonRepo, docService);

        var result = await handler.Handle(
            new GetAssemblyDocumentationQuery(skeleton.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public async Task GetAssemblyDocumentationHandler_NotFound_ReturnsError()
    {
        var repo = new InMemorySkeletonRepo();
        var handler = new GetAssemblyDocumentationQueryHandler(repo, new AssemblyDocumentationService());

        var result = await handler.Handle(
            new GetAssemblyDocumentationQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetExplodedViewHandler_ValidSkeleton_ReturnsDtoWithLayers()
    {
        var skeleton = CreateSkeleton();
        var skeletonRepo = new InMemorySkeletonRepo(skeleton);
        var docService = new AssemblyDocumentationService();
        var handler = new GetExplodedViewQueryHandler(skeletonRepo, docService);

        var result = await handler.Handle(
            new GetExplodedViewQuery(skeleton.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Layers);
    }

    [Fact]
    public async Task GetExplodedViewHandler_NotFound_ReturnsError()
    {
        var repo = new InMemorySkeletonRepo();
        var handler = new GetExplodedViewQueryHandler(repo, new AssemblyDocumentationService());

        var result = await handler.Handle(
            new GetExplodedViewQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── FluentValidation tests ────────────────────────────────────────────────

    [Fact]
    public void CreateCatalogEntryCommandValidator_ValidCommand_PassesValidation()
    {
        var validator = new CreateCatalogEntryCommandValidator();
        var result = validator.Validate(ValidCreateCommand());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateCatalogEntryCommandValidator_EmptyName_FailsValidation()
    {
        var validator = new CreateCatalogEntryCommandValidator();
        var result = validator.Validate(ValidCreateCommand(name: ""));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void CreateCatalogEntryCommandValidator_OversizedPayload_FailsValidation()
    {
        var validator = new CreateCatalogEntryCommandValidator();
        var bigPayload = new string('x', 70000);
        var result = validator.Validate(ValidCreateCommand(payload: bigPayload));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PayloadJson");
    }

    [Fact]
    public void SubmitCatalogEntryCommandValidator_ValidCommand_PassesValidation()
    {
        var validator = new SubmitCatalogEntryCommandValidator();
        var result = validator.Validate(new SubmitCatalogEntryCommand(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void SubmitCatalogEntryCommandValidator_EmptyEntryId_FailsValidation()
    {
        var validator = new SubmitCatalogEntryCommandValidator();
        var result = validator.Validate(new SubmitCatalogEntryCommand(Guid.Empty, Guid.NewGuid()));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "EntryId");
    }

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private static async Task<CatalogEntry> SeedEntryInState(
        InMemoryCatalogRepo repo, CatalogLifecycleState targetState)
    {
        var entry = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole, "Test", "Desc",
            CatalogVisibility.Private, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;

        await repo.AddAsync(entry, CancellationToken.None);

        if (targetState == CatalogLifecycleState.Draft)
            return entry;

        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        await repo.UpdateAsync(entry, CancellationToken.None);
        if (targetState == CatalogLifecycleState.Submitted)
            return entry;

        entry.Approve(StaffId);
        await repo.UpdateAsync(entry, CancellationToken.None);
        if (targetState == CatalogLifecycleState.Approved)
            return entry;

        entry.Publish(StaffId);
        await repo.UpdateAsync(entry, CancellationToken.None);
        if (targetState == CatalogLifecycleState.Published)
            return entry;

        // Deprecated is not reachable from Rejected branch; only from Published
        if (targetState == CatalogLifecycleState.Deprecated)
        {
            entry.Deprecate(StaffId);
            await repo.UpdateAsync(entry, CancellationToken.None);
        }

        return entry;
    }

    // ── In-memory implementations ─────────────────────────────────────────────

    private sealed class InMemoryCatalogRepo : ICatalogEntryRepository
    {
        private readonly List<CatalogEntry> _store = new();

        public Task<CatalogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.FirstOrDefault(e => e.Id == id));

        public Task<IReadOnlyList<CatalogEntry>> ListAsync(
            Guid tenantId, CatalogType? type, CatalogLifecycleState? state, CancellationToken ct = default)
        {
            var q = _store.Where(e => e.TenantId == tenantId);
            if (type.HasValue) q = q.Where(e => e.Type == type.Value);
            if (state.HasValue) q = q.Where(e => e.State == state.Value);
            return Task.FromResult<IReadOnlyList<CatalogEntry>>(q.ToList());
        }

        public Task AddAsync(CatalogEntry entry, CancellationToken ct = default)
        {
            _store.Add(entry);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogEntry entry, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<CatalogEntry?> GetByFingerprintAsync(Guid tenantId, string fingerprint, CancellationToken ct = default)
            => Task.FromResult(_store.FirstOrDefault(e =>
                e.TenantId == tenantId && e.SimilarityFingerprint == fingerprint));
    }

    private sealed class InMemorySkeletonRepo : ISkeletonRepository
    {
        private readonly Dictionary<Guid, Skeleton> _store = new();

        public InMemorySkeletonRepo(params Skeleton[] skeletons)
        {
            foreach (var s in skeletons)
                _store[s.Id] = s;
        }

        public Task<Skeleton?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var s) ? s : null);

        public Task UpdateAsync(Skeleton skeleton, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class TrackingAuditLogger : IStaffAuditLogger
    {
        public List<(Guid staffId, string action, Guid entryId, string? details)> Calls = new();

        public Task LogAsync(Guid staffUserId, string action, Guid catalogEntryId,
            string? details = null, CancellationToken cancellationToken = default)
        {
            Calls.Add((staffUserId, action, catalogEntryId, details));
            return Task.CompletedTask;
        }

        public Task LogSystemActorActivationAsync(Guid catalogEntryId, string reason,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
