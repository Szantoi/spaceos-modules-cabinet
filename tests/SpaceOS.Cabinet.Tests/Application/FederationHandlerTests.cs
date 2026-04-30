using Ardalis.Result;
using FluentValidation;
using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Application.Handlers;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Application.Validators;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Tests.Infrastructure;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Application;

/// <summary>
/// Unit tests for federation CQRS handlers and validators.
/// </summary>
public class FederationHandlerTests
{
    // ── Shared helpers ─────────────────────────────────────────────────────────

    private static readonly Guid OwnerTenantId = Guid.NewGuid();
    private static readonly Guid RaterTenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();

    private const string ValidPayload = """{"role":"Shelf","priority":1}""";
    private const string ValidSchema = "horizontal_role/v1";

    private static CatalogEntry CreatePublishedEntry(
        Guid? tenantId = null,
        CatalogVisibility visibility = CatalogVisibility.Community)
    {
        var entry = CatalogEntry.CreateDraft(
            tenantId ?? OwnerTenantId, ActorId,
            CatalogType.HorizontalRole, "Test Entry", "Desc",
            visibility, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry.Approve(AdminId);
        entry.Publish(AdminId);
        return entry;
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

    // ── RateCatalogEntryCommandHandler ────────────────────────────────────────

    [Fact]
    public async Task RateCatalogEntryHandler_ValidRating_Succeeds()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var ratingRepo = new InMemoryRatingRepository();
        var handler = new RateCatalogEntryCommandHandler(repo, ratingRepo);

        var result = await handler.Handle(new RateCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId, 4, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RateCatalogEntryHandler_SelfRating_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var ratingRepo = new InMemoryRatingRepository();
        var handler = new RateCatalogEntryCommandHandler(repo, ratingRepo);

        // Same tenant for rater and owner
        var result = await handler.Handle(new RateCatalogEntryCommand(
            entry.Id, OwnerTenantId, OwnerTenantId, ActorId, 4, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RateCatalogEntryHandler_EntryNotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var ratingRepo = new InMemoryRatingRepository();
        var handler = new RateCatalogEntryCommandHandler(repo, ratingRepo);

        var result = await handler.Handle(new RateCatalogEntryCommand(
            Guid.NewGuid(), OwnerTenantId, RaterTenantId, ActorId, 4, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RateCatalogEntryHandler_Rerate_UpdatesAverage()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var ratingRepo = new InMemoryRatingRepository();
        var handler = new RateCatalogEntryCommandHandler(repo, ratingRepo);

        // First rating
        await handler.Handle(new RateCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId, 2, null), CancellationToken.None);
        ratingRepo.Set(entry.Id, RaterTenantId, 2);

        // Re-rate with 4 stars
        var result = await handler.Handle(new RateCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId, 4, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        // After re-rate the average should reflect the new stars
        Assert.True(updated!.Ratings.AverageStars > 0);
    }

    [Fact]
    public async Task RateCatalogEntryHandler_UpsertIdempotency_UpdatesNotDuplicates()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var ratingRepo = new InMemoryRatingRepository();
        var handler = new RateCatalogEntryCommandHandler(repo, ratingRepo);

        // First rating (count goes to 1)
        await handler.Handle(new RateCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId, 3, null), CancellationToken.None);
        ratingRepo.Set(entry.Id, RaterTenantId, 3);

        // Second call for same tenant = re-rate, not a second vote
        var result = await handler.Handle(new RateCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId, 5, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        // Count stays at 1 (re-rate path, not new rating)
        Assert.Equal(1, updated!.Ratings.Count);
    }

    // ── FlagCatalogEntryCommandHandler ────────────────────────────────────────

    [Fact]
    public async Task FlagCatalogEntryHandler_ValidFlag_Succeeds()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new FlagCatalogEntryCommandHandler(repo);

        var result = await handler.Handle(new FlagCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId,
            FlagReason.Spam, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task FlagCatalogEntryHandler_SelfFlag_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new FlagCatalogEntryCommandHandler(repo);

        var result = await handler.Handle(new FlagCatalogEntryCommand(
            entry.Id, OwnerTenantId, OwnerTenantId, ActorId,
            FlagReason.Spam, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task FlagCatalogEntryHandler_EntryNotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new FlagCatalogEntryCommandHandler(repo);

        var result = await handler.Handle(new FlagCatalogEntryCommand(
            Guid.NewGuid(), OwnerTenantId, RaterTenantId, ActorId,
            FlagReason.Inappropriate, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task FlagCatalogEntryHandler_IncrementsFlagCount()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new FlagCatalogEntryCommandHandler(repo);

        await handler.Handle(new FlagCatalogEntryCommand(
            entry.Id, OwnerTenantId, RaterTenantId, ActorId,
            FlagReason.Spam, null), CancellationToken.None);

        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.Equal(1, updated!.ActiveFlagCount);
    }

    // ── ClearFlagsByAdminCommandHandler ───────────────────────────────────────

    [Fact]
    public async Task ClearFlagsByAdminHandler_ValidAdmin_ClearsFlags()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new ClearFlagsByAdminCommandHandler(repo);

        var result = await handler.Handle(new ClearFlagsByAdminCommand(
            entry.Id, AdminId, AckDays: 30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.NotNull(updated!.AdminAcknowledgedUntil);
    }

    [Fact]
    public async Task ClearFlagsByAdminHandler_EntryNotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new ClearFlagsByAdminCommandHandler(repo);

        var result = await handler.Handle(new ClearFlagsByAdminCommand(
            Guid.NewGuid(), AdminId, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── AssignFingerprintCommandHandler ───────────────────────────────────────

    [Fact]
    public async Task AssignFingerprintHandler_ValidFingerprint_Succeeds()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new AssignFingerprintCommandHandler(repo);

        var result = await handler.Handle(new AssignFingerprintCommand(
            entry.Id, ActorId, "horizontalrole:vendor:code:v1", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = await repo.GetByIdAsync(entry.Id);
        Assert.Equal("horizontalrole:vendor:code:v1", updated!.SimilarityFingerprint);
    }

    [Fact]
    public async Task AssignFingerprintHandler_EntryNotFound_ReturnsError()
    {
        var repo = new InMemoryCatalogRepo();
        var handler = new AssignFingerprintCommandHandler(repo);

        var result = await handler.Handle(new AssignFingerprintCommand(
            Guid.NewGuid(), ActorId, "fp", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── RecomputeClustersCommandHandler ───────────────────────────────────────

    [Fact]
    public async Task RecomputeClustersHandler_ValidTenantId_Succeeds()
    {
        var handler = new RecomputeClustersCommandHandler();

        var result = await handler.Handle(new RecomputeClustersCommand(
            Guid.NewGuid(), ActorId), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RecomputeClustersHandler_EmptyTenantId_ReturnsInvalid()
    {
        var handler = new RecomputeClustersCommandHandler();

        var result = await handler.Handle(new RecomputeClustersCommand(
            Guid.Empty, ActorId), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    // ── ListCommunityEntriesQueryHandler ──────────────────────────────────────

    [Fact]
    public async Task ListCommunityEntriesQuery_SharedEntry_ReturnsIt()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry(visibility: CatalogVisibility.Shared);
        await repo.AddAsync(entry);
        var handler = new ListCommunityEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCommunityEntriesQuery(OwnerTenantId, null, null, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task ListCommunityEntriesQuery_CommunityEntry_ReturnsIt()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry(visibility: CatalogVisibility.Community);
        await repo.AddAsync(entry);
        var handler = new ListCommunityEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCommunityEntriesQuery(OwnerTenantId, null, null, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task ListCommunityEntriesQuery_PrivateEntry_DoesNotReturn()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry(visibility: CatalogVisibility.Private);
        await repo.AddAsync(entry);
        var handler = new ListCommunityEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCommunityEntriesQuery(OwnerTenantId, null, null, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ListCommunityEntriesQuery_OnlyVisible_ExcludesHidden()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry(visibility: CatalogVisibility.Community);
        // Add 3 flags to trigger auto-hide
        entry.IngestFlag(CatalogEntryFlag.Create(
            entry.Id, RaterTenantId, ActorId, FlagReason.Spam, null, OwnerTenantId).Value);
        entry.IngestFlag(CatalogEntryFlag.Create(
            entry.Id, RaterTenantId, ActorId, FlagReason.Inappropriate, null, OwnerTenantId).Value);
        entry.IngestFlag(CatalogEntryFlag.Create(
            entry.Id, RaterTenantId, ActorId, FlagReason.Plagiarism, null, OwnerTenantId).Value);
        await repo.AddAsync(entry);
        var handler = new ListCommunityEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCommunityEntriesQuery(OwnerTenantId, null, null, OnlyVisible: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ListCommunityEntriesQuery_FilterByType_Works()
    {
        var repo = new InMemoryCatalogRepo();
        // Add one HorizontalRole entry (community) and one MaterialThickness entry
        var entry1 = CreatePublishedEntry(visibility: CatalogVisibility.Community);
        var entry2 = CatalogEntry.CreateDraft(
            OwnerTenantId, ActorId, CatalogType.MaterialThickness, "MatEntry", "d",
            CatalogVisibility.Community, """{"thickness":18}""", "material_thickness/v1",
            NullCatalogPayloadValidator.Instance).Value;
        entry2.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry2.Approve(AdminId);
        entry2.Publish(AdminId);
        await repo.AddAsync(entry1);
        await repo.AddAsync(entry2);
        var handler = new ListCommunityEntriesQueryHandler(repo);

        var result = await handler.Handle(
            new ListCommunityEntriesQuery(OwnerTenantId, CatalogType.HorizontalRole, null, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(CatalogType.HorizontalRole, result.Value[0].Type);
    }

    // ── GetModerationQueueQueryHandler ────────────────────────────────────────

    [Fact]
    public async Task GetModerationQueueQuery_FlaggedEntry_ReturnsIt()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        entry.IngestFlag(CatalogEntryFlag.Create(
            entry.Id, RaterTenantId, ActorId, FlagReason.Spam, null, OwnerTenantId).Value);
        await repo.AddAsync(entry);
        var handler = new GetModerationQueueQueryHandler(repo);

        var result = await handler.Handle(
            new GetModerationQueueQuery(OwnerTenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task GetModerationQueueQuery_CleanEntry_DoesNotReturn()
    {
        var repo = new InMemoryCatalogRepo();
        var entry = CreatePublishedEntry();
        await repo.AddAsync(entry);
        var handler = new GetModerationQueueQueryHandler(repo);

        var result = await handler.Handle(
            new GetModerationQueueQuery(OwnerTenantId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── FluentValidation ─────────────────────────────────────────────────────

    [Fact]
    public void Validator_RateCatalogEntry_SelfRating_Invalid()
    {
        var validator = new RateCatalogEntryCommandValidator();
        var sameTenant = Guid.NewGuid();
        var cmd = new RateCatalogEntryCommand(Guid.NewGuid(), sameTenant, sameTenant, ActorId, 4, null);

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RateCatalogEntry_Stars0_Invalid()
    {
        var validator = new RateCatalogEntryCommandValidator();
        var cmd = new RateCatalogEntryCommand(Guid.NewGuid(), OwnerTenantId, RaterTenantId, ActorId, 0, null);

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Stars");
    }

    [Fact]
    public void Validator_RateCatalogEntry_Stars6_Invalid()
    {
        var validator = new RateCatalogEntryCommandValidator();
        var cmd = new RateCatalogEntryCommand(Guid.NewGuid(), OwnerTenantId, RaterTenantId, ActorId, 6, null);

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Stars");
    }

    [Fact]
    public void Validator_RateCatalogEntry_ValidCommand_Passes()
    {
        var validator = new RateCatalogEntryCommandValidator();
        var cmd = new RateCatalogEntryCommand(Guid.NewGuid(), OwnerTenantId, RaterTenantId, ActorId, 4, null);

        var result = validator.Validate(cmd);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_FlagCatalogEntry_SelfFlag_Invalid()
    {
        var validator = new FlagCatalogEntryCommandValidator();
        var sameTenant = Guid.NewGuid();
        var cmd = new FlagCatalogEntryCommand(Guid.NewGuid(), sameTenant, sameTenant, ActorId, FlagReason.Spam, null);

        var result = validator.Validate(cmd);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_FlagCatalogEntry_ValidCommand_Passes()
    {
        var validator = new FlagCatalogEntryCommandValidator();
        var cmd = new FlagCatalogEntryCommand(Guid.NewGuid(), OwnerTenantId, RaterTenantId, ActorId, FlagReason.Spam, null);

        var result = validator.Validate(cmd);

        Assert.True(result.IsValid);
    }
}
