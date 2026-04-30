using FluentValidation;
using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Application.Handlers;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Application.Validators;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Catalog.Infrastructure;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Application;

/// <summary>
/// Unit tests for <see cref="SubmitCommunityCatalogEntryCommandHandler"/> (UPSERT) and
/// <see cref="GetCatalogEntryWithRatingsQueryHandler"/>.
/// </summary>
public class SubmitCommunityEntryHandlerTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    // payload has vendor+code+variant so DefaultCatalogFingerprintExtractor can derive a fingerprint
    private const string FingerprintablePayload = """{"vendor":"acme","code":"shelf-std","variant":"v1","role":"Shelf"}""";
    private const string ValidSchema = "horizontal_role/v1";
    private const string NoFingerprintPayload = """{"role":"Shelf","priority":1}""";

    private static SubmitCommunityCatalogEntryCommand ValidCommand(
        string? payload = null, string? schema = null, CatalogVisibility visibility = CatalogVisibility.Community) =>
        new(
            TenantId: TenantId,
            ActorUserId: ActorId,
            Type: CatalogType.HorizontalRole,
            Name: "Community Shelf",
            Description: "Test community shelf entry",
            Visibility: visibility,
            PayloadJson: payload ?? FingerprintablePayload,
            PayloadSchemaVersion: schema ?? ValidSchema);

    private static (InMemoryFingerprintRepo repo, SubmitCommunityCatalogEntryCommandHandler handler)
        MakeHandler()
    {
        var repo = new InMemoryFingerprintRepo();
        var extractor = new DefaultCatalogFingerprintExtractor();
        var handler = new SubmitCommunityCatalogEntryCommandHandler(
            repo, NullCatalogPayloadValidator.Instance, extractor);
        return (repo, handler);
    }

    // ── CREATE path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCommunity_NewEntry_CreatesAndReturnsId()
    {
        var (_, handler) = MakeHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task SubmitCommunity_NewEntry_IsInSubmittedState()
    {
        var (repo, handler) = MakeHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        var stored = await repo.GetByIdAsync(result.Value);
        Assert.NotNull(stored);
        Assert.Equal(CatalogLifecycleState.Submitted, stored!.State);
    }

    [Fact]
    public async Task SubmitCommunity_NewEntry_AssignsFingerprintServerSide()
    {
        var (repo, handler) = MakeHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        var stored = await repo.GetByIdAsync(result.Value);
        // fingerprintable payload: type:vendor:code:variant → "horizontalrole:acme:shelf-std:v1"
        Assert.Equal("horizontalrole:acme:shelf-std:v1", stored!.SimilarityFingerprint);
    }

    [Fact]
    public async Task SubmitCommunity_NoFingerprintPayload_CreatesEntryWithNullFingerprint()
    {
        var (repo, handler) = MakeHandler();

        var result = await handler.Handle(ValidCommand(payload: NoFingerprintPayload), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await repo.GetByIdAsync(result.Value);
        Assert.Null(stored!.SimilarityFingerprint);
    }

    [Fact]
    public async Task SubmitCommunity_InvalidJson_ReturnsInvalid()
    {
        var (_, handler) = MakeHandler();

        var result = await handler.Handle(ValidCommand(payload: "not-json"), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SubmitCommunity_EmptyName_ReturnsError()
    {
        var (_, handler) = MakeHandler();
        var cmd = ValidCommand() with { Name = "" };

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SubmitCommunity_OversizedPayload_ReturnsError()
    {
        var (_, handler) = MakeHandler();
        var bigPayload = "{\"vendor\":\"x\",\"code\":\"" + new string('a', 70000) + "\",\"variant\":\"v1\"}";
        var cmd = ValidCommand(payload: bigPayload);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── UPSERT path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitCommunity_SameTenantAndFingerprint_ReturnsSameId()
    {
        var (repo, handler) = MakeHandler();

        var first = await handler.Handle(ValidCommand(), CancellationToken.None);
        // Simulate the repo storing the fingerprint so the second call detects the duplicate
        repo.SetFingerprint(first.Value, "horizontalrole:acme:shelf-std:v1");

        var second = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value, second.Value);
    }

    [Fact]
    public async Task SubmitCommunity_Upsert_UpdatesNameAndPayload()
    {
        var (repo, handler) = MakeHandler();

        var first = await handler.Handle(ValidCommand(), CancellationToken.None);
        repo.SetFingerprint(first.Value, "horizontalrole:acme:shelf-std:v1");

        var updated = ValidCommand() with { Name = "Updated Shelf" };
        await handler.Handle(updated, CancellationToken.None);

        var stored = await repo.GetByIdAsync(first.Value);
        Assert.Equal("Updated Shelf", stored!.Name);
    }

    [Fact]
    public async Task SubmitCommunity_Upsert_EntryIsSubmittedAfterUpdate()
    {
        var (repo, handler) = MakeHandler();

        var first = await handler.Handle(ValidCommand(), CancellationToken.None);
        repo.SetFingerprint(first.Value, "horizontalrole:acme:shelf-std:v1");

        // Re-submit (UPSERT)
        await handler.Handle(ValidCommand() with { Name = "Updated" }, CancellationToken.None);

        var stored = await repo.GetByIdAsync(first.Value);
        Assert.Equal(CatalogLifecycleState.Submitted, stored!.State);
    }

    [Fact]
    public async Task SubmitCommunity_NoFingerprintPayload_DifferentCallsCreateSeparateEntries()
    {
        var (repo, handler) = MakeHandler();

        // Payloads without vendor/code/variant cannot have a fingerprint: each call creates a new entry
        var r1 = await handler.Handle(ValidCommand(payload: NoFingerprintPayload), CancellationToken.None);
        var r2 = await handler.Handle(ValidCommand(payload: NoFingerprintPayload), CancellationToken.None);

        Assert.True(r1.IsSuccess);
        Assert.True(r2.IsSuccess);
        Assert.NotEqual(r1.Value, r2.Value);
    }

    [Fact]
    public async Task SubmitCommunity_DifferentTenants_CreateSeparateEntries()
    {
        var repo = new InMemoryFingerprintRepo();
        var extractor = new DefaultCatalogFingerprintExtractor();
        var h1 = new SubmitCommunityCatalogEntryCommandHandler(repo, NullCatalogPayloadValidator.Instance, extractor);

        var tenant2 = Guid.NewGuid();
        var r1 = await h1.Handle(ValidCommand(), CancellationToken.None);
        repo.SetFingerprint(r1.Value, "horizontalrole:acme:shelf-std:v1");

        var cmdTenant2 = ValidCommand() with { TenantId = tenant2 };
        var r2 = await h1.Handle(cmdTenant2, CancellationToken.None);

        Assert.True(r2.IsSuccess);
        Assert.NotEqual(r1.Value, r2.Value);
    }

    // ── GetCatalogEntryWithRatingsQueryHandler ─────────────────────────────────

    [Fact]
    public async Task GetCatalogEntryWithRatings_NoRatings_ReturnsZeroCount()
    {
        var repo = new InMemoryFingerprintRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new GetCatalogEntryWithRatingsQueryHandler(repo);

        var result = await handler.Handle(new GetCatalogEntryWithRatingsQuery(entry.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.RatingCount);
        Assert.Equal(0m, result.Value.AverageStars);
        Assert.Null(result.Value.LastRatedAt);
    }

    [Fact]
    public async Task GetCatalogEntryWithRatings_WithRatings_ReturnsCorrectRollup()
    {
        var repo = new InMemoryFingerprintRepo();
        var entry = CreatePublishedEntry();
        var ratingRepo = new SpaceOS.Cabinet.Tests.Infrastructure.InMemoryRatingRepository();
        var rateHandler = new RateCatalogEntryCommandHandler(repo, ratingRepo);

        var raterTenantId = Guid.NewGuid();
        await repo.AddAsync(entry);
        await rateHandler.Handle(
            new RateCatalogEntryCommand(entry.Id, TenantId, raterTenantId, ActorId, 4, null),
            CancellationToken.None);

        var queryHandler = new GetCatalogEntryWithRatingsQueryHandler(repo);
        var result = await queryHandler.Handle(
            new GetCatalogEntryWithRatingsQuery(entry.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.RatingCount);
        Assert.Equal(4m, result.Value.AverageStars);
        Assert.NotNull(result.Value.LastRatedAt);
    }

    [Fact]
    public async Task GetCatalogEntryWithRatings_EntryNotFound_ReturnsError()
    {
        var repo = new InMemoryFingerprintRepo();
        var handler = new GetCatalogEntryWithRatingsQueryHandler(repo);

        var result = await handler.Handle(
            new GetCatalogEntryWithRatingsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetCatalogEntryWithRatings_ReturnsCorrectEntry()
    {
        var repo = new InMemoryFingerprintRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new GetCatalogEntryWithRatingsQueryHandler(repo);

        var result = await handler.Handle(
            new GetCatalogEntryWithRatingsQuery(entry.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(entry.Id, result.Value.Entry.Id);
        Assert.Equal(TenantId, result.Value.Entry.TenantId);
    }

    // ── SubmitCommunityCatalogEntryCommandValidator ────────────────────────────

    [Fact]
    public void Validator_SubmitCommunity_ValidCommand_Passes()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();

        var result = validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_SubmitCommunity_EmptyTenantId_Invalid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var cmd = ValidCommand() with { TenantId = Guid.Empty };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validator_SubmitCommunity_EmptyActorId_Invalid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var cmd = ValidCommand() with { ActorUserId = Guid.Empty };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ActorUserId");
    }

    [Fact]
    public void Validator_SubmitCommunity_EmptyName_Invalid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var cmd = ValidCommand() with { Name = "" };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validator_SubmitCommunity_CuratedVisibility_Invalid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var cmd = ValidCommand(visibility: CatalogVisibility.Curated);

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Visibility");
    }

    [Fact]
    public void Validator_SubmitCommunity_OversizedPayload_Invalid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var bigPayload = new string('x', 70000);
        var cmd = ValidCommand(payload: bigPayload);

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PayloadJson");
    }

    [Fact]
    public void Validator_SubmitCommunity_EmptySchema_Invalid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var cmd = ValidCommand() with { PayloadSchemaVersion = "" };

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PayloadSchemaVersion");
    }

    [Fact]
    public void Validator_SubmitCommunity_SharedVisibility_Valid()
    {
        var validator = new SubmitCommunityCatalogEntryCommandValidator();
        var cmd = ValidCommand(visibility: CatalogVisibility.Shared);

        var result = validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    // ── CatalogEntry.UpdateAndResubmit domain method ──────────────────────────

    [Fact]
    public void UpdateAndResubmit_FromDraft_TransitionsToSubmitted()
    {
        var entry = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole,
            "Original", "Desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;

        var result = entry.UpdateAndResubmit(
            "Updated", "New desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Submitted, entry.State);
    }

    [Fact]
    public void UpdateAndResubmit_FromPublished_TransitionsToSubmitted()
    {
        var entry = BuildPublishedEntry();

        var result = entry.UpdateAndResubmit(
            "Updated", "desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Submitted, entry.State);
    }

    [Fact]
    public void UpdateAndResubmit_UpdatesName()
    {
        var entry = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole,
            "Original", "Desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;

        entry.UpdateAndResubmit(
            "New Name", "desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.Equal("New Name", entry.Name);
    }

    [Fact]
    public void UpdateAndResubmit_EmptyName_ReturnsInvalid()
    {
        var entry = BuildPublishedEntry();

        var result = entry.UpdateAndResubmit(
            "", "desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.False(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Published, entry.State); // unchanged
    }

    [Fact]
    public void UpdateAndResubmit_OversizedPayload_ReturnsInvalid()
    {
        var entry = BuildPublishedEntry();
        var bigPayload = "{\"vendor\":\"x\",\"code\":\"" + new string('a', 70000) + "\",\"variant\":\"v1\"}";

        var result = entry.UpdateAndResubmit(
            "Name", "desc", CatalogVisibility.Community,
            bigPayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void UpdateAndResubmit_IncreasesVersion()
    {
        var entry = BuildPublishedEntry();
        var versionBefore = entry.Version;

        entry.UpdateAndResubmit(
            "Updated", "desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.True(entry.Version > versionBefore);
    }

    [Fact]
    public void UpdateAndResubmit_RaisesSubmittedDomainEvent()
    {
        var entry = BuildPublishedEntry();
        entry.PopDomainEvents(); // clear prior events

        entry.UpdateAndResubmit(
            "Updated", "desc", CatalogVisibility.Community,
            FingerprintablePayload, ValidSchema, ActorId,
            NullCatalogPayloadValidator.Instance);

        Assert.Contains(entry.DomainEvents, e => e is SpaceOS.Cabinet.Catalog.Events.CatalogEntrySubmitted);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static CatalogEntry CreatePublishedEntry()
    {
        var adminId = Guid.NewGuid();
        var entry = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole, "Entry", "Desc",
            CatalogVisibility.Community, FingerprintablePayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry.Approve(adminId);
        entry.Publish(adminId);
        return entry;
    }

    private static CatalogEntry BuildPublishedEntry() => CreatePublishedEntry();

    // ── In-memory repo with fingerprint support ────────────────────────────────

    private sealed class InMemoryFingerprintRepo : ICatalogEntryRepository
    {
        private readonly Dictionary<Guid, CatalogEntry> _store = new();

        public Task<CatalogEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.TryGetValue(id, out var e) ? e : null);

        public Task<IReadOnlyList<CatalogEntry>> ListAsync(
            Guid tenantId, CatalogType? type, CatalogLifecycleState? state, CancellationToken ct = default)
        {
            var q = _store.Values.Where(e => e.TenantId == tenantId);
            if (type.HasValue) q = q.Where(e => e.Type == type.Value);
            if (state.HasValue) q = q.Where(e => e.State == state.Value);
            return Task.FromResult<IReadOnlyList<CatalogEntry>>(q.ToList());
        }

        public Task AddAsync(CatalogEntry entry, CancellationToken ct = default)
        {
            _store[entry.Id] = entry;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(CatalogEntry entry, CancellationToken ct = default)
        {
            _store[entry.Id] = entry;
            return Task.CompletedTask;
        }

        public Task<CatalogEntry?> GetByFingerprintAsync(Guid tenantId, string fingerprint, CancellationToken ct = default)
            => Task.FromResult(_store.Values.FirstOrDefault(e =>
                e.TenantId == tenantId && e.SimilarityFingerprint == fingerprint));

        /// <summary>Test helper: manually assign a fingerprint on a stored entry to set up UPSERT scenarios.</summary>
        public void SetFingerprint(Guid entryId, string fingerprint)
        {
            if (_store.TryGetValue(entryId, out var entry))
                entry.AssignFingerprintAndCluster(fingerprint, null);
        }
    }
}
