using Ardalis.Result;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Catalog.Events;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class CatalogEntryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly ICatalogPayloadValidator ValidValidator = new AlwaysValidValidator();
    private static readonly ICatalogPayloadValidator InvalidValidator = new AlwaysInvalidValidator();

    private static readonly string ValidPayload = """{"role":"Shelf","priority":1}""";
    // Schema version must satisfy ^[a-z][a-z0-9_]*/v\d+$ (all-lowercase segment before the slash)
    private static readonly string ValidSchema = "horizontal_role/v1";

    private static CatalogEntry CreateDraftEntry() =>
        CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole, "Test Entry",
            "Description", CatalogVisibility.Private, ValidPayload, ValidSchema, ValidValidator).Value;

    private static CatalogEntry CreateSubmittedEntry()
    {
        var entry = CreateDraftEntry();
        entry.Submit(ActorId, ValidValidator);
        return entry;
    }

    private static CatalogEntry CreateApprovedEntry()
    {
        var entry = CreateSubmittedEntry();
        entry.Approve(ActorId);
        return entry;
    }

    private static CatalogEntry CreatePublishedEntry()
    {
        var entry = CreateApprovedEntry();
        entry.Publish(ActorId);
        return entry;
    }

    // ── CreateDraft ───────────────────────────────────────────────────────────

    [Fact]
    public void CreateDraft_ValidInput_ReturnsSuccess()
    {
        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "My Entry", "Desc", CatalogVisibility.Private, ValidPayload, ValidSchema, ValidValidator);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void CreateDraft_EmptyName_ReturnsInvalid()
    {
        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "", null, CatalogVisibility.Private, ValidPayload, ValidSchema, ValidValidator);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void CreateDraft_NameTooLong_ReturnsInvalid()
    {
        var longName = new string('x', CatalogEntry.MaxNameLength + 1);

        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            longName, null, CatalogVisibility.Private, ValidPayload, ValidSchema, ValidValidator);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void CreateDraft_DescriptionTooLong_ReturnsInvalid()
    {
        var longDesc = new string('d', CatalogEntry.MaxDescriptionLength + 1);

        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "Valid Name", longDesc, CatalogVisibility.Private, ValidPayload, ValidSchema, ValidValidator);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void CreateDraft_InvalidSchemaVersion_ReturnsInvalid()
    {
        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "Valid Name", null, CatalogVisibility.Private, ValidPayload, "INVALID", ValidValidator);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void CreateDraft_CuratedWithWrongTenant_ReturnsError()
    {
        var nonSystemTenant = Guid.NewGuid();

        var result = CatalogEntry.CreateDraft(nonSystemTenant, ActorId, CatalogType.HorizontalRole,
            "Valid Name", null, CatalogVisibility.Curated, ValidPayload, ValidSchema, ValidValidator);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void CreateDraft_CuratedWithSystemTenant_ReturnsSuccess()
    {
        var result = CatalogEntry.CreateDraft(SystemCatalog.TenantId, SystemCatalog.ActorUserId,
            CatalogType.HorizontalRole, "Curated Entry", null, CatalogVisibility.Curated,
            ValidPayload, ValidSchema, ValidValidator);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CreateDraft_OversizedPayload_ReturnsInvalid()
    {
        // Build a JSON string that exceeds 64 KB in UTF-8
        var bigValue = new string('a', CatalogEntry.MaxPayloadSizeBytes + 1);
        var oversizedPayload = $$$"""{"role":"{{{bigValue}}}","priority":1}""";

        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "Valid Name", null, CatalogVisibility.Private, oversizedPayload, ValidSchema, ValidValidator);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void CreateDraft_InvalidPayload_ReturnsError()
    {
        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "Valid Name", null, CatalogVisibility.Private, ValidPayload, ValidSchema, InvalidValidator);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void CreateDraft_SetsStateToDraft()
    {
        var entry = CreateDraftEntry();

        Assert.Equal(CatalogLifecycleState.Draft, entry.State);
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    [Fact]
    public void Submit_FromDraft_Succeeds()
    {
        var entry = CreateDraftEntry();

        var result = entry.Submit(ActorId, ValidValidator);

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Submitted, entry.State);
    }

    [Fact]
    public void Submit_FromSubmitted_ReturnsError()
    {
        var entry = CreateSubmittedEntry();

        var result = entry.Submit(ActorId, ValidValidator);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── Approve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_FromSubmitted_Succeeds()
    {
        var entry = CreateSubmittedEntry();

        var result = entry.Approve(ActorId);

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Approved, entry.State);
    }

    [Fact]
    public void Approve_FromDraft_ReturnsError()
    {
        var entry = CreateDraftEntry();

        var result = entry.Approve(ActorId);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── Reject ────────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_FromSubmitted_Succeeds()
    {
        var entry = CreateSubmittedEntry();

        var result = entry.Reject(ActorId, "Does not meet standards.");

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Rejected, entry.State);
    }

    [Fact]
    public void Reject_EmptyReason_ReturnsInvalid()
    {
        var entry = CreateSubmittedEntry();

        var result = entry.Reject(ActorId, "");

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void Reject_FromApproved_ReturnsError()
    {
        var entry = CreateApprovedEntry();

        var result = entry.Reject(ActorId, "Some reason.");

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── Publish ───────────────────────────────────────────────────────────────

    [Fact]
    public void Publish_FromApproved_Succeeds()
    {
        var entry = CreateApprovedEntry();

        var result = entry.Publish(ActorId);

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Published, entry.State);
    }

    [Fact]
    public void Publish_FromDraft_ReturnsError()
    {
        var entry = CreateDraftEntry();

        var result = entry.Publish(ActorId);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── Deprecate ─────────────────────────────────────────────────────────────

    [Fact]
    public void Deprecate_FromPublished_Succeeds()
    {
        var entry = CreatePublishedEntry();

        var result = entry.Deprecate(ActorId);

        Assert.True(result.IsSuccess);
        Assert.Equal(CatalogLifecycleState.Deprecated, entry.State);
    }

    [Fact]
    public void Deprecate_FromDraft_ReturnsError()
    {
        var entry = CreateDraftEntry();

        var result = entry.Deprecate(ActorId);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── Version ───────────────────────────────────────────────────────────────

    [Fact]
    public void Version_IncrementsOnEachTransition()
    {
        var entry = CreateDraftEntry();
        Assert.Equal(1, entry.Version);

        entry.Submit(ActorId, ValidValidator);
        Assert.Equal(2, entry.Version);

        entry.Approve(ActorId);
        Assert.Equal(3, entry.Version);

        entry.Publish(ActorId);
        Assert.Equal(4, entry.Version);

        entry.Deprecate(ActorId);
        Assert.Equal(5, entry.Version);
    }

    // ── ContentHash ───────────────────────────────────────────────────────────

    [Fact]
    public void ContentHash_IsNotEmpty()
    {
        var entry = CreateDraftEntry();

        Assert.False(string.IsNullOrWhiteSpace(entry.ContentHash));
    }

    // ── Domain Events ─────────────────────────────────────────────────────────

    [Fact]
    public void DomainEvents_CorrectEventPerTransition()
    {
        var entry = CreateDraftEntry();
        entry.PopDomainEvents(); // clear creation event

        entry.Submit(ActorId, ValidValidator);
        var submitted = entry.PopDomainEvents();
        Assert.Single(submitted);
        Assert.IsType<CatalogEntrySubmitted>(submitted[0]);

        entry.Approve(ActorId);
        var approved = entry.PopDomainEvents();
        Assert.Single(approved);
        Assert.IsType<CatalogEntryApproved>(approved[0]);

        entry.Publish(ActorId);
        var published = entry.PopDomainEvents();
        Assert.Single(published);
        Assert.IsType<CatalogEntryPublished>(published[0]);

        entry.Deprecate(ActorId);
        var deprecated = entry.PopDomainEvents();
        Assert.Single(deprecated);
        Assert.IsType<CatalogEntryDeprecated>(deprecated[0]);
    }

    [Fact]
    public void PopDomainEvents_ClearsEvents()
    {
        var entry = CreateDraftEntry();

        var firstPop = entry.PopDomainEvents();
        var secondPop = entry.PopDomainEvents();

        Assert.Single(firstPop);
        Assert.Empty(secondPop);
    }

    [Fact]
    public void CreateDraft_RaisesCatalogEntryCreatedEvent()
    {
        var result = CatalogEntry.CreateDraft(TenantId, ActorId, CatalogType.HorizontalRole,
            "My Entry", null, CatalogVisibility.Private, ValidPayload, ValidSchema, ValidValidator);
        var entry = result.Value;

        var events = entry.PopDomainEvents();

        Assert.Single(events);
        var created = Assert.IsType<CatalogEntryCreated>(events[0]);
        Assert.Equal(entry.Id, created.CatalogEntryId);
        Assert.Equal(ActorId, created.ActorUserId);
    }

    // ── Timestamps ────────────────────────────────────────────────────────────

    [Fact]
    public void PublishedAt_SetOnPublish()
    {
        var entry = CreateApprovedEntry();
        Assert.Null(entry.PublishedAt);

        entry.Publish(ActorId);

        Assert.NotNull(entry.PublishedAt);
    }

    [Fact]
    public void DeprecatedAt_SetOnDeprecate()
    {
        var entry = CreatePublishedEntry();
        Assert.Null(entry.DeprecatedAt);

        entry.Deprecate(ActorId);

        Assert.NotNull(entry.DeprecatedAt);
    }
}

internal sealed class AlwaysValidValidator : ICatalogPayloadValidator
{
    public Result Validate(CatalogType type, string schemaVersion, string payloadJson)
        => Result.Success();
}

internal sealed class AlwaysInvalidValidator : ICatalogPayloadValidator
{
    public Result Validate(CatalogType type, string schemaVersion, string payloadJson)
        => Result.Error("Invalid payload.");
}
