using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class Vector3Tests
{
    [Fact]
    public void Create_ValidComponents_ReturnsSuccess()
    {
        var result = Vector3.Create(1.0, 2.0, 3.0);

        Assert.True(result.IsSuccess);
        Assert.Equal(1.0, result.Value.X);
        Assert.Equal(2.0, result.Value.Y);
        Assert.Equal(3.0, result.Value.Z);
    }

    [Fact]
    public void Create_NaN_ReturnsInvalid()
    {
        var result = Vector3.Create(double.NaN, 0, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_PositiveInfinity_ReturnsInvalid()
    {
        var result = Vector3.Create(0, double.PositiveInfinity, 0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_NegativeInfinity_ReturnsInvalid()
    {
        var result = Vector3.Create(0, 0, double.NegativeInfinity);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Zero_ReturnsZeroVector()
    {
        Assert.Equal(0.0, Vector3.Zero.X);
        Assert.Equal(0.0, Vector3.Zero.Y);
        Assert.Equal(0.0, Vector3.Zero.Z);
    }

    [Fact]
    public void UnitX_ReturnsCorrectVector()
    {
        Assert.Equal(1.0, Vector3.UnitX.X);
        Assert.Equal(0.0, Vector3.UnitX.Y);
        Assert.Equal(0.0, Vector3.UnitX.Z);
    }

    [Fact]
    public void UnitY_ReturnsCorrectVector()
    {
        Assert.Equal(0.0, Vector3.UnitY.X);
        Assert.Equal(1.0, Vector3.UnitY.Y);
        Assert.Equal(0.0, Vector3.UnitY.Z);
    }

    [Fact]
    public void UnitZ_ReturnsCorrectVector()
    {
        Assert.Equal(0.0, Vector3.UnitZ.X);
        Assert.Equal(0.0, Vector3.UnitZ.Y);
        Assert.Equal(1.0, Vector3.UnitZ.Z);
    }

    [Fact]
    public void IsValid_ValidVector_ReturnsTrue()
    {
        var v = Vector3.Create(1, 2, 3).Value;

        Assert.True(v.IsValid());
    }

    [Fact]
    public void Length_CalculatesCorrectly()
    {
        var v = Vector3.Create(3, 4, 0).Value;

        Assert.Equal(5.0, v.Length(), 10);
    }

    [Fact]
    public void Normalized_ReturnsUnitLength()
    {
        var v = Vector3.Create(3, 4, 0).Value;
        var result = v.Normalized();

        Assert.True(result.IsSuccess);
        Assert.Equal(1.0, result.Value.Length(), 10);
    }

    [Fact]
    public void Normalized_ZeroVector_ReturnsError()
    {
        var result = Vector3.Zero.Normalized();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Dot_CalculatesCorrectly()
    {
        var a = Vector3.Create(1, 2, 3).Value;
        var b = Vector3.Create(4, 5, 6).Value;

        Assert.Equal(32.0, a.Dot(b), 10);
    }

    [Fact]
    public void Cross_UnitXAndUnitY_ReturnsUnitZ()
    {
        var result = Vector3.UnitX.Cross(Vector3.UnitY);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.UnitZ));
    }

    [Fact]
    public void Cross_CalculatesCorrectly()
    {
        var a = Vector3.Create(1, 0, 0).Value;
        var b = Vector3.Create(0, 1, 0).Value;
        var result = a.Cross(b);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.Create(0, 0, 1).Value));
    }

    [Fact]
    public void IsApproximatelyEqualTo_WithinEpsilon_ReturnsTrue()
    {
        var a = Vector3.Create(1.0, 2.0, 3.0).Value;
        var b = Vector3.Create(1.0 + 1e-10, 2.0, 3.0).Value;

        Assert.True(a.IsApproximatelyEqualTo(b));
    }

    [Fact]
    public void IsApproximatelyEqualTo_OutsideEpsilon_ReturnsFalse()
    {
        var a = Vector3.Create(1.0, 2.0, 3.0).Value;
        var b = Vector3.Create(1.1, 2.0, 3.0).Value;

        Assert.False(a.IsApproximatelyEqualTo(b));
    }

    [Fact]
    public void OperatorAdd_ReturnsCorrectSum()
    {
        var a = Vector3.Create(1, 2, 3).Value;
        var b = Vector3.Create(4, 5, 6).Value;
        var result = a + b;

        Assert.True(result.IsApproximatelyEqualTo(Vector3.Create(5, 7, 9).Value));
    }

    [Fact]
    public void OperatorSubtract_ReturnsCorrectDifference()
    {
        var a = Vector3.Create(4, 5, 6).Value;
        var b = Vector3.Create(1, 2, 3).Value;
        var result = a - b;

        Assert.True(result.IsApproximatelyEqualTo(Vector3.Create(3, 3, 3).Value));
    }

    [Fact]
    public void OperatorMultiply_ScalesCorrectly()
    {
        var v = Vector3.Create(1, 2, 3).Value;
        var result = v * 2.0;

        Assert.True(result.IsApproximatelyEqualTo(Vector3.Create(2, 4, 6).Value));
    }

    [Fact]
    public void OperatorMultiply_Commutative_ScalesCorrectly()
    {
        var v = Vector3.Create(1, 2, 3).Value;
        var result = 3.0 * v;

        Assert.True(result.IsApproximatelyEqualTo(Vector3.Create(3, 6, 9).Value));
    }
}
