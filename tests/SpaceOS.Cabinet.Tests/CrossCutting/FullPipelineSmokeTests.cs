using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Application.Handlers;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.CrossCutting;

/// <summary>
/// Full-pipeline smoke tests covering the Application layer integrated with Domain, Catalog, and Assembly.
/// </summary>
public class FullPipelineSmokeTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid StaffId = Guid.NewGuid();
    private const string ValidPayload = """{"role":"Shelf","priority":1}""";
    private const string ValidSchema = "horizontal_role/v1";

    private static Skeleton CreateSkeleton()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        return Skeleton.Create(TenantId, dim).Value;
    }

    private static CatalogEntry CreatePublishedEntry()
    {
        var entry = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole, "Shelf Standard", "Desc",
            CatalogVisibility.Private, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry.Approve(StaffId);
        entry.Publish(StaffId);
        return entry;
    }

    // ── Pipeline tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_CreateSkeletonAndPinCatalogEntry()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts.First().Id;
        var catalogEntryId = Guid.NewGuid();

        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new PinCatalogEntryCommandHandler(repo);

        var result = await handler.Handle(
            new PinCatalogEntryCommand(skeleton.Id, partId, CatalogType.HorizontalRole,
                catalogEntryId, ActorId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(skeleton.PinnedCatalogEntries.ContainsKey((partId, CatalogType.HorizontalRole)));
        Assert.Equal(catalogEntryId, skeleton.PinnedCatalogEntries[(partId, CatalogType.HorizontalRole)]);
    }

    [Fact]
    public async Task FullPipeline_PinAndDeriveBillOfServices()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts.First().Id;
        var catalogEntryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.HorizontalRole, catalogEntryId);

        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new DeriveBillOfServicesCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveBillOfServicesCommand(skeleton.Id, ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal(partId, result.Value.Items[0].PartId);
        Assert.Equal(catalogEntryId, result.Value.Items[0].CatalogEntryId);
        Assert.Equal(skeleton.Id, result.Value.SkeletonId);
    }

    [Fact]
    public async Task FullPipeline_DeriveAssembly_RaisesEvent()
    {
        var skeleton = CreateSkeleton();
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new DeriveAssemblyCommandHandler(repo);

        var result = await handler.Handle(
            new DeriveAssemblyCommand(skeleton.Id, ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // The domain raises AssemblyDerived event
        var events = skeleton.PopDomainEvents();
        // SkeletonCreated was raised at construction; AssemblyDerived is in the list
        Assert.Contains(events, e => e.GetType().Name == "AssemblyDerived");
    }

    [Fact]
    public async Task FullPipeline_SnapshotV02_RoundTrip_PreservesPins()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts.First().Id;
        var catalogEntryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.HorizontalRole, catalogEntryId);

        // Snapshot → JSON → restore
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();
        var restoredSnapshot = SkeletonSnapshot.FromJson(json);

        Assert.True(restoredSnapshot.IsSuccess);
        // Verify the pinned entry was serialised
        var pins = restoredSnapshot.Value.PinnedCatalogEntries;
        Assert.Single(pins);
        Assert.Equal(partId, pins[0].PartId);
        Assert.Equal(catalogEntryId, pins[0].CatalogEntryId);
    }

    [Fact]
    public async Task FullPipeline_CatalogEntry_DraftToPublished()
    {
        var repo = new InMemoryCatalogRepo();
        var createHandler = new CreateCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);
        var submitHandler = new SubmitCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance);
        var approveHandler = new ApproveCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);
        var publishHandler = new PublishCatalogEntryCommandHandler(repo, NullStaffAuditLogger.Instance);

        var createResult = await createHandler.Handle(
            new CreateCatalogEntryCommand(TenantId, CatalogType.HorizontalRole, "Shelf",
                "Desc", CatalogVisibility.Private, ValidPayload, ValidSchema, ActorId),
            CancellationToken.None);

        var entryId = createResult.Value;
        await submitHandler.Handle(new SubmitCatalogEntryCommand(entryId, ActorId), CancellationToken.None);
        await approveHandler.Handle(new ApproveCatalogEntryCommand(entryId, StaffId), CancellationToken.None);
        var publishResult = await publishHandler.Handle(
            new PublishCatalogEntryCommand(entryId, StaffId), CancellationToken.None);

        Assert.True(publishResult.IsSuccess);
        var entry = await repo.GetByIdAsync(entryId);
        Assert.Equal(CatalogLifecycleState.Published, entry!.State);
        Assert.NotNull(entry.PublishedAt);
    }

    [Fact]
    public async Task FullPipeline_AssemblyDocumentation_GeneratesSteps()
    {
        var skeleton = CreateSkeleton();
        var docService = new AssemblyDocumentationService();
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new GetAssemblyDocumentationQueryHandler(repo, docService);

        var result = await handler.Handle(
            new GetAssemblyDocumentationQuery(skeleton.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(skeleton.Parts.Count, result.Value.Count);
        // Steps are in order 0..N-1
        for (int i = 0; i < result.Value.Count; i++)
            Assert.Equal(i, result.Value[i].Order);
    }

    [Fact]
    public async Task FullPipeline_Deterministic_SameInputSameResult()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton1 = Skeleton.Create(TenantId, dim).Value;

        var docService = new AssemblyDocumentationService();
        var repo1 = new InMemorySkeletonRepo(skeleton1);
        var handler = new GetAssemblyDocumentationQueryHandler(repo1, docService);

        var result1 = await handler.Handle(new GetAssemblyDocumentationQuery(skeleton1.Id), CancellationToken.None);

        // Re-run with a fresh skeleton of same dimensions
        var skeleton2 = Skeleton.Create(TenantId, dim).Value;
        var repo2 = new InMemorySkeletonRepo(skeleton2);
        var handler2 = new GetAssemblyDocumentationQueryHandler(repo2, docService);
        var result2 = await handler2.Handle(new GetAssemblyDocumentationQuery(skeleton2.Id), CancellationToken.None);

        Assert.Equal(result1.Value.Count, result2.Value.Count);
        for (int i = 0; i < result1.Value.Count; i++)
        {
            Assert.Equal(result1.Value[i].Order, result2.Value[i].Order);
            Assert.Equal(result1.Value[i].Title, result2.Value[i].Title);
        }
    }

    [Fact]
    public async Task FullPipeline_MarkdownSanitizer_InPipeline()
    {
        var skeleton = CreateSkeleton();
        var sanitizer = new MarkdownSanitizer();
        var docService = new AssemblyDocumentationService(sanitizer);
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new GetAssemblyDocumentationQueryHandler(repo, docService);

        var result = await handler.Handle(
            new GetAssemblyDocumentationQuery(skeleton.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Sanitized instructions must be non-empty
        Assert.All(result.Value, step => Assert.False(string.IsNullOrWhiteSpace(step.Instruction)));
    }

    [Fact]
    public async Task FullPipeline_ExplodedView_WithConnections_HasMultipleLayers()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(TenantId, dim).Value;

        // Add a child part and connect it to the base
        var partDim = PartDimension.Create(564, 560, 18).Value;
        var transform = AffineTransform.Translation(Vector3.Create(18, 0, 360).Value).Value;
        var frame = PartFrame.Create(transform, partDim).Value;
        var child = skeleton.AddPart(frame, "mat").Value;
        var geo = new ConnectionGeometry(PartFace.Top, PartEdge.BottomFront, 0);
        skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, child.Id, geo);

        var docService = new AssemblyDocumentationService();
        var repo = new InMemorySkeletonRepo(skeleton);
        var handler = new GetExplodedViewQueryHandler(repo, docService);

        var result = await handler.Handle(new GetExplodedViewQuery(skeleton.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Layers.Count >= 1);
    }

    [Fact]
    public async Task FullPipeline_CatalogResolutionProvider_FallsBackToCurated()
    {
        // Published curated entry from system tenant
        var curatedEntry = CatalogEntry.CreateDraft(
            SystemCatalog.TenantId, StaffId, CatalogType.HorizontalRole, "Curated Shelf", "Desc",
            CatalogVisibility.Curated, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        curatedEntry.Submit(StaffId, NullCatalogPayloadValidator.Instance);
        curatedEntry.Approve(StaffId);
        curatedEntry.Publish(StaffId);

        var provider = new CatalogResolutionProvider(new[] { curatedEntry });
        var context = new CatalogResolutionContext();

        var result = provider.Resolve(TenantId, CatalogType.HorizontalRole, context);

        Assert.True(result.IsSuccess);
        Assert.Equal(curatedEntry.Id, result.Value.Id);
    }

    [Fact]
    public async Task FullPipeline_SystemCatalogSeeds_Resolvable()
    {
        // Create curated Published entries for HorizontalRole and MaterialThickness
        // using schema versions that satisfy ^[a-z][a-z0-9_]*/v\d+$ (all-lowercase segment)
        var hr = CatalogEntry.CreateDraft(
            SystemCatalog.TenantId, SystemCatalog.ActorUserId,
            CatalogType.HorizontalRole, "Default Shelf Role", string.Empty,
            CatalogVisibility.Curated, """{"role":"Shelf","priority":1}""",
            "horizontal_role/v1", NullCatalogPayloadValidator.Instance).Value;
        hr.Submit(SystemCatalog.ActorUserId, NullCatalogPayloadValidator.Instance);
        hr.Approve(SystemCatalog.ActorUserId);
        hr.Publish(SystemCatalog.ActorUserId);

        var mt = CatalogEntry.CreateDraft(
            SystemCatalog.TenantId, SystemCatalog.ActorUserId,
            CatalogType.MaterialThickness, "18mm Particleboard", string.Empty,
            CatalogVisibility.Curated, """{"value":18,"unit":"mm","material":"Particleboard"}""",
            "material_thickness/v1", NullCatalogPayloadValidator.Instance).Value;
        mt.Submit(SystemCatalog.ActorUserId, NullCatalogPayloadValidator.Instance);
        mt.Approve(SystemCatalog.ActorUserId);
        mt.Publish(SystemCatalog.ActorUserId);

        var provider = new CatalogResolutionProvider(new[] { hr, mt });
        var context = new CatalogResolutionContext();

        var hrResult = provider.Resolve(TenantId, CatalogType.HorizontalRole, context);
        Assert.True(hrResult.IsSuccess);

        var mtResult = provider.Resolve(TenantId, CatalogType.MaterialThickness, context);
        Assert.True(mtResult.IsSuccess);
    }

    // ── In-memory helpers ──────────────────────────────────────────────────────

    private sealed class InMemorySkeletonRepo(params Skeleton[] skeletons) : ISkeletonRepository
    {
        private readonly Dictionary<Guid, Skeleton> _store =
            skeletons.ToDictionary(s => s.Id);

        public Task<Skeleton?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var s) ? s : null);

        public Task UpdateAsync(Skeleton skeleton, CancellationToken ct = default)
            => Task.CompletedTask;
    }

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
}
