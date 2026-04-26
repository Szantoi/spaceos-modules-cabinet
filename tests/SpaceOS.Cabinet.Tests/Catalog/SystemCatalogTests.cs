using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class SystemCatalogTests
{
    [Fact]
    public void TenantId_IsDeterministic()
    {
        var expected = Guid.Parse("00000000-0000-0000-0000-000000000001");

        Assert.Equal(expected, SystemCatalog.TenantId);
    }

    [Fact]
    public void ActorUserId_IsDeterministic()
    {
        var expected = Guid.Parse("00000000-0000-0000-0000-000000000002");

        Assert.Equal(expected, SystemCatalog.ActorUserId);
    }

    [Fact]
    public void Seeds_Has16Entries()
    {
        Assert.Equal(16, SystemCatalogSeeds.All.Count);
    }

    [Fact]
    public void Seeds_TwoPerCatalogType()
    {
        var groups = SystemCatalogSeeds.All
            .GroupBy(s => s.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (CatalogType type in Enum.GetValues<CatalogType>())
        {
            Assert.True(groups.TryGetValue(type, out var count),
                $"No seeds found for CatalogType.{type}");
            Assert.Equal(2, count);
        }
    }
}
