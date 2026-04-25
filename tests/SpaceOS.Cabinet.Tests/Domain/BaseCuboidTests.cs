using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class BaseCuboidTests
{
    private static AssemblyDimension ValidDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    [Fact]
    public void CreateDefault_ProducesFourParts()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

        // The BaseCuboid should have exactly 4 structural parts
        var parts = skeleton.BaseCuboid.GetType()
            .GetMethod("GetAllParts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(skeleton.BaseCuboid, null) as System.Collections.Generic.IEnumerable<object>;

        // Verify through the public API: skeleton.Parts includes all BaseCuboid parts
        Assert.Equal(4, skeleton.Parts.Count);
    }

    [Fact]
    public void CreateDefault_LeftSide_IsNotNull()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

        Assert.NotNull(skeleton.BaseCuboid.LeftSide);
    }

    [Fact]
    public void CreateDefault_RightSide_IsNotNull()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

        Assert.NotNull(skeleton.BaseCuboid.RightSide);
    }

    [Fact]
    public void CreateDefault_Bottom_IsNotNull()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

        Assert.NotNull(skeleton.BaseCuboid.Bottom);
    }

    [Fact]
    public void CreateDefault_Top_IsNotNull()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

        Assert.NotNull(skeleton.BaseCuboid.Top);
    }

    [Fact]
    public void CreateDefault_BackPanel_IsNullByDefault()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

        Assert.Null(skeleton.BaseCuboid.BackPanel);
    }

    [Fact]
    public void CreateDefault_AllPartsHaveCorrectSkeletonId()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var bc = skeleton.BaseCuboid;

        Assert.All(
            new[] { bc.LeftSide, bc.RightSide, bc.Bottom, bc.Top },
            p => Assert.Equal(skeleton.Id, p.SkeletonId));
    }

    [Fact]
    public void CreateDefault_AllPartsHaveUniqueIds()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var bc = skeleton.BaseCuboid;

        var ids = new[] { bc.LeftSide.Id, bc.RightSide.Id, bc.Bottom.Id, bc.Top.Id };
        Assert.Equal(4, ids.Distinct().Count());
    }
}
