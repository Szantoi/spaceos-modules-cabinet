using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class BillOfServicesTests
{
    private static AssemblyDimension ValidDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    private static Skeleton CreateSkeleton()
        => Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

    [Fact]
    public void BillOfServices_HasSkeletonId()
    {
        var skeleton = CreateSkeleton();

        var result = skeleton.DeriveBillOfServices();

        Assert.Equal(skeleton.Id, result.Value.SkeletonId);
    }

    [Fact]
    public void BillOfServices_EmptyItems_WhenNoPins()
    {
        var skeleton = CreateSkeleton();

        var result = skeleton.DeriveBillOfServices();

        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public void BillOfServices_Items_ContainCorrectData()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.HardwareSet, entryId);

        var result = skeleton.DeriveBillOfServices();

        var item = Assert.Single(result.Value.Items);
        Assert.Equal(partId, item.PartId);
        Assert.Equal(CatalogType.HardwareSet, item.CatalogType);
        Assert.Equal(entryId, item.CatalogEntryId);
    }

    [Fact]
    public void BillOfServicesItem_HasAllFields()
    {
        var partId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var item = new BillOfServicesItem(partId, CatalogType.JointType, entryId);

        Assert.Equal(partId, item.PartId);
        Assert.Equal(CatalogType.JointType, item.CatalogType);
        Assert.Equal(entryId, item.CatalogEntryId);
    }

    [Fact]
    public void BillOfServices_SkeletonIdMatchesSkeleton()
    {
        var skeleton = CreateSkeleton();
        skeleton.PinCatalogEntry(skeleton.Parts[0].Id, CatalogType.RasterStandard, Guid.NewGuid());

        var bill = skeleton.DeriveBillOfServices().Value;

        Assert.Equal(skeleton.Id, bill.SkeletonId);
    }
}
