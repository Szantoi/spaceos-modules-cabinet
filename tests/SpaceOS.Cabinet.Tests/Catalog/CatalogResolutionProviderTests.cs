using Ardalis.Result;
using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class CatalogResolutionProviderTests
{
    private static readonly ICatalogPayloadValidator AlwaysValid = new ResolutionAlwaysValidValidator();

    private static CatalogEntry CreatePublishedEntry(Guid tenantId, CatalogType type, CatalogVisibility visibility)
    {
        // Schema must satisfy ^[a-z][a-z0-9_]*/v\d+$ — all lowercase before the slash
        var entry = CatalogEntry.CreateDraft(tenantId, Guid.NewGuid(), type, "test", null,
            visibility, """{"role":"Shelf","priority":1}""", "horizontal_role/v1", AlwaysValid).Value;
        entry.Submit(Guid.NewGuid(), AlwaysValid);
        entry.Approve(Guid.NewGuid());
        entry.Publish(Guid.NewGuid());
        return entry;
    }

    private static CatalogResolutionContext EmptyContext() => new();

    [Fact]
    public void Resolve_TenantPrivate_ReturnsPrivateEntry()
    {
        var tenantId = Guid.NewGuid();
        var privateEntry = CreatePublishedEntry(tenantId, CatalogType.HorizontalRole, CatalogVisibility.Private);
        var provider = new CatalogResolutionProvider([privateEntry]);

        var result = provider.Resolve(tenantId, CatalogType.HorizontalRole, EmptyContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(privateEntry.Id, result.Value.Id);
    }

    [Fact]
    public void Resolve_NoEntries_ReturnsError()
    {
        var provider = new CatalogResolutionProvider([]);

        var result = provider.Resolve(Guid.NewGuid(), CatalogType.HorizontalRole, EmptyContext());

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Resolve_FallbackToCurated_WhenNoPrivate()
    {
        var tenantId = Guid.NewGuid();
        var curatedEntry = CreatePublishedEntry(SystemCatalog.TenantId, CatalogType.HorizontalRole, CatalogVisibility.Curated);
        var provider = new CatalogResolutionProvider([curatedEntry]);

        var result = provider.Resolve(tenantId, CatalogType.HorizontalRole, EmptyContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(curatedEntry.Id, result.Value.Id);
    }

    [Fact]
    public void Resolve_CacheHit_ReturnsSameInstance()
    {
        var tenantId = Guid.NewGuid();
        var privateEntry = CreatePublishedEntry(tenantId, CatalogType.HorizontalRole, CatalogVisibility.Private);
        var provider = new CatalogResolutionProvider([privateEntry]);
        var context = EmptyContext();

        var first = provider.Resolve(tenantId, CatalogType.HorizontalRole, context);
        var second = provider.Resolve(tenantId, CatalogType.HorizontalRole, context);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Same(first.Value, second.Value);
    }

    [Fact]
    public void Resolve_TenantPrivatePreferredOverCurated()
    {
        var tenantId = Guid.NewGuid();
        var privateEntry = CreatePublishedEntry(tenantId, CatalogType.HorizontalRole, CatalogVisibility.Private);
        var curatedEntry = CreatePublishedEntry(SystemCatalog.TenantId, CatalogType.HorizontalRole, CatalogVisibility.Curated);
        var provider = new CatalogResolutionProvider([curatedEntry, privateEntry]);

        var result = provider.Resolve(tenantId, CatalogType.HorizontalRole, EmptyContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(privateEntry.Id, result.Value.Id);
    }

    [Fact]
    public void Resolve_DifferentType_ReturnsDifferentEntry()
    {
        var tenantId = Guid.NewGuid();
        var horizontalEntry = CreatePublishedEntry(tenantId, CatalogType.HorizontalRole, CatalogVisibility.Private);
        var materialEntry = CreatePublishedEntry(tenantId, CatalogType.MaterialThickness, CatalogVisibility.Private);
        var provider = new CatalogResolutionProvider([horizontalEntry, materialEntry]);

        var horizontal = provider.Resolve(tenantId, CatalogType.HorizontalRole, EmptyContext());
        var material = provider.Resolve(tenantId, CatalogType.MaterialThickness, EmptyContext());

        Assert.True(horizontal.IsSuccess);
        Assert.True(material.IsSuccess);
        Assert.NotEqual(horizontal.Value.Id, material.Value.Id);
    }
}

internal sealed class ResolutionAlwaysValidValidator : ICatalogPayloadValidator
{
    public Result Validate(CatalogType type, string schemaVersion, string payloadJson)
        => Result.Success();
}
