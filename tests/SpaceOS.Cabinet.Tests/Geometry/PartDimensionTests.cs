using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class PartDimensionTests
{
    [Fact]
    public void Create_ValidDimensions_ReturnsSuccess()
    {
        var result = PartDimension.Create(600, 300, 18);

        Assert.True(result.IsSuccess);
        Assert.Equal(600, result.Value.Length);
        Assert.Equal(300, result.Value.Width);
        Assert.Equal(18, result.Value.Thickness);
    }

    [Fact]
    public void Create_NaN_ReturnsInvalid()
    {
        var result = PartDimension.Create(double.NaN, 300, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_InfiniteLength_ReturnsInvalid()
    {
        var result = PartDimension.Create(double.PositiveInfinity, 300, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxLength_ReturnsInvalid()
    {
        var result = PartDimension.Create(PartDimension.MaxLength + 1, 300, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxWidth_ReturnsInvalid()
    {
        var result = PartDimension.Create(600, PartDimension.MaxWidth + 1, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ExceedsMaxThickness_ReturnsInvalid()
    {
        var result = PartDimension.Create(600, 300, PartDimension.MaxThickness + 1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_BelowMinDimension_ReturnsInvalid()
    {
        var result = PartDimension.Create(PartDimension.MinDimension - 0.01, 300, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_ZeroDimension_ReturnsInvalid()
    {
        var result = PartDimension.Create(0, 300, 18);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ToVector_ReturnsCorrectVector()
    {
        var dim = PartDimension.Create(600, 300, 18).Value;
        var v = dim.ToVector();

        Assert.True(v.IsSuccess);
        Assert.Equal(600, v.Value.X);
        Assert.Equal(300, v.Value.Y);
        Assert.Equal(18, v.Value.Z);
    }

    [Fact]
    public void Create_AtMaxLength_ReturnsSuccess()
    {
        var result = PartDimension.Create(PartDimension.MaxLength, 300, 18);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_AtMinDimension_ReturnsSuccess()
    {
        var result = PartDimension.Create(PartDimension.MinDimension, PartDimension.MinDimension, PartDimension.MinDimension);

        Assert.True(result.IsSuccess);
    }
}
