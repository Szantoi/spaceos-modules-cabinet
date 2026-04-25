using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class PartFrameTests
{
    private static PartDimension ValidDimension() =>
        PartDimension.Create(600, 300, 18).Value;

    [Fact]
    public void Create_ValidInputs_ReturnsSuccess()
    {
        var result = PartFrame.Create(AffineTransform.Identity, ValidDimension());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GrainDirectionInAssembly_Identity_ReturnsUnitX()
    {
        var frame = PartFrame.Create(AffineTransform.Identity, ValidDimension()).Value;
        var grain = frame.GrainDirectionInAssembly();

        Assert.True(grain.IsSuccess);
        Assert.True(grain.Value.IsApproximatelyEqualTo(Vector3.UnitX, 1e-10));
    }

    [Fact]
    public void NormalInAssembly_Identity_ReturnsUnitZ()
    {
        var frame = PartFrame.Create(AffineTransform.Identity, ValidDimension()).Value;
        var normal = frame.NormalInAssembly();

        Assert.True(normal.IsSuccess);
        Assert.True(normal.Value.IsApproximatelyEqualTo(Vector3.UnitZ, 1e-10));
    }

    [Fact]
    public void DatumInAssembly_Identity_ReturnsZero()
    {
        var frame = PartFrame.Create(AffineTransform.Identity, ValidDimension()).Value;
        var datum = frame.DatumInAssembly();

        Assert.True(datum.IsSuccess);
        Assert.True(datum.Value.IsApproximatelyEqualTo(Vector3.Zero, 1e-10));
    }

    [Fact]
    public void DatumInAssembly_WithTranslation_ReturnsTranslatedOrigin()
    {
        var t = AffineTransform.Translation(Vector3.Create(100, 200, 300).Value).Value;
        var frame = PartFrame.Create(t, ValidDimension()).Value;
        var datum = frame.DatumInAssembly();

        Assert.True(datum.IsSuccess);
        Assert.True(datum.Value.IsApproximatelyEqualTo(Vector3.Create(100, 200, 300).Value, 1e-10));
    }

    [Fact]
    public void GrainDirectionInAssembly_WithRotation_CorrectlyTransformsDirection()
    {
        // 90 degrees around Z: UnitX becomes UnitY
        var t = AffineTransform.Rotation(Vector3.UnitZ, Math.PI / 2.0).Value;
        var frame = PartFrame.Create(t, ValidDimension()).Value;
        var grain = frame.GrainDirectionInAssembly();

        Assert.True(grain.IsSuccess);
        Assert.True(grain.Value.IsApproximatelyEqualTo(Vector3.UnitY, 1e-10));
    }
}
