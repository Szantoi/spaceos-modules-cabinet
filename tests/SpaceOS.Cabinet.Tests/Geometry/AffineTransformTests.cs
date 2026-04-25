using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class AffineTransformTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void Identity_IsValid()
    {
        Assert.True(AffineTransform.Identity.IsValid());
    }

    [Fact]
    public void Identity_BasisX_ReturnsUnitX()
    {
        var bx = AffineTransform.Identity.BasisX();

        Assert.True(bx.IsApproximatelyEqualTo(Vector3.UnitX, Tolerance));
    }

    [Fact]
    public void Identity_BasisY_ReturnsUnitY()
    {
        var by = AffineTransform.Identity.BasisY();

        Assert.True(by.IsApproximatelyEqualTo(Vector3.UnitY, Tolerance));
    }

    [Fact]
    public void Identity_BasisZ_ReturnsUnitZ()
    {
        var bz = AffineTransform.Identity.BasisZ();

        Assert.True(bz.IsApproximatelyEqualTo(Vector3.UnitZ, Tolerance));
    }

    [Fact]
    public void Identity_Origin_ReturnsZero()
    {
        var origin = AffineTransform.Identity.Origin();

        Assert.True(origin.IsApproximatelyEqualTo(Vector3.Zero, Tolerance));
    }

    [Fact]
    public void Translation_ApplyToPoint_TranslatesCorrectly()
    {
        var offset = Vector3.Create(10, 20, 30).Value;
        var t = AffineTransform.Translation(offset).Value;
        var point = Vector3.Create(1, 2, 3).Value;

        var result = t.ApplyTo(point);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.Create(11, 22, 33).Value, Tolerance));
    }

    [Fact]
    public void Translation_ApplyToDirection_IgnoresTranslation()
    {
        var offset = Vector3.Create(100, 200, 300).Value;
        var t = AffineTransform.Translation(offset).Value;
        var dir = Vector3.UnitX;

        var result = t.ApplyToDirection(dir);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.UnitX, Tolerance));
    }

    [Fact]
    public void Rotation_90DegreesAroundZ_RotatesUnitXToUnitY()
    {
        var axis = Vector3.UnitZ;
        var t = AffineTransform.Rotation(axis, Math.PI / 2.0).Value;
        var point = Vector3.UnitX;

        var result = t.ApplyToDirection(point);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.UnitY, 1e-10));
    }

    [Fact]
    public void Scaling_ApplyToPoint_ScalesCorrectly()
    {
        var t = AffineTransform.Scaling(2, 3, 4).Value;
        var point = Vector3.Create(1, 1, 1).Value;

        var result = t.ApplyTo(point);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.Create(2, 3, 4).Value, Tolerance));
    }

    [Fact]
    public void Compose_TwoTranslations_AddsThem()
    {
        var t1 = AffineTransform.Translation(Vector3.Create(10, 0, 0).Value).Value;
        var t2 = AffineTransform.Translation(Vector3.Create(5, 0, 0).Value).Value;
        var composed = AffineTransform.Compose(t1, t2).Value;

        var result = composed.ApplyTo(Vector3.Zero);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.Create(15, 0, 0).Value, Tolerance));
    }

    [Fact]
    public void Inverse_OfTranslation_IsNegativeTranslation()
    {
        var t = AffineTransform.Translation(Vector3.Create(5, 0, 0).Value).Value;
        var inv = t.Inverse().Value;
        var point = Vector3.Create(5, 0, 0).Value;

        var result = inv.ApplyTo(point);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.Zero, 1e-10));
    }

    [Fact]
    public void Inverse_ThenForward_GivesIdentity()
    {
        var t = AffineTransform.Translation(Vector3.Create(3, 7, -2).Value).Value;
        var inv = t.Inverse().Value;
        var composed = AffineTransform.Compose(t, inv).Value;

        Assert.True(composed.IsApproximatelyEqualTo(AffineTransform.Identity, 1e-10));
    }

    [Fact]
    public void IsApproximatelyEqualTo_SameMatrix_ReturnsTrue()
    {
        Assert.True(AffineTransform.Identity.IsApproximatelyEqualTo(AffineTransform.Identity));
    }

    [Fact]
    public void IsApproximatelyEqualTo_DifferentMatrix_ReturnsFalse()
    {
        var t = AffineTransform.Translation(Vector3.Create(1, 0, 0).Value).Value;

        Assert.False(AffineTransform.Identity.IsApproximatelyEqualTo(t));
    }

    [Fact]
    public void Rotation_NaN_ReturnsError()
    {
        var result = AffineTransform.Rotation(Vector3.UnitZ, double.NaN);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Rotation_ZeroAxis_ReturnsError()
    {
        var result = AffineTransform.Rotation(Vector3.Zero, Math.PI / 2.0);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Translation_NaNOffset_ReturnsError()
    {
        var nanVec = Vector3.Create(double.NaN, 0, 0);

        Assert.False(nanVec.IsSuccess);
        // Translation itself cannot be created because Vector3.Create rejects NaN
    }

    [Fact]
    public void Scaling_NaN_ReturnsError()
    {
        var result = AffineTransform.Scaling(double.NaN, 1, 1);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Rotation_180DegreesAroundY_NegatesX()
    {
        var t = AffineTransform.Rotation(Vector3.UnitY, Math.PI).Value;
        var result = t.ApplyToDirection(Vector3.UnitX);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsApproximatelyEqualTo(Vector3.Create(-1, 0, 0).Value, 1e-10));
    }
}
