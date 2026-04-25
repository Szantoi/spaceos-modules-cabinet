using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class AssemblyDimensionTests
{
    [Fact]
    public void Create_ValidDimensions_ReturnsSuccess()
    {
        var result = AssemblyDimension.Create(800, 2000, 600);

        Assert.True(result.IsSuccess);
        Assert.Equal(800, result.Value.Width);
        Assert.Equal(2000, result.Value.Height);
        Assert.Equal(600, result.Value.Depth);
    }

    [Fact]
    public void Create_NaN_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(double.NaN, 2000, 600);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_InfiniteHeight_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(800, double.PositiveInfinity, 600);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxWidth_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(AssemblyDimension.MaxWidth + 1, 2000, 600);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxHeight_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(800, AssemblyDimension.MaxHeight + 1, 600);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxDepth_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(800, 2000, AssemblyDimension.MaxDepth + 1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_BelowMinDimension_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(AssemblyDimension.MinDimension - 1, 2000, 600);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ZeroDimension_ReturnsInvalid()
    {
        var result = AssemblyDimension.Create(0, 2000, 600);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ToVector_ReturnsCorrectVector()
    {
        var dim = AssemblyDimension.Create(800, 2000, 600).Value;
        var v = dim.ToVector();

        Assert.True(v.IsSuccess);
        // ToVector returns (Width, Depth, Height)
        Assert.Equal(800, v.Value.X);
        Assert.Equal(600, v.Value.Y);
        Assert.Equal(2000, v.Value.Z);
    }

    [Fact]
    public void Create_AtMinDimension_ReturnsSuccess()
    {
        var result = AssemblyDimension.Create(AssemblyDimension.MinDimension, AssemblyDimension.MinDimension, AssemblyDimension.MinDimension);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_AtMaxDepth_ReturnsSuccess()
    {
        var result = AssemblyDimension.Create(800, 2000, AssemblyDimension.MaxDepth);

        Assert.True(result.IsSuccess);
    }
}
