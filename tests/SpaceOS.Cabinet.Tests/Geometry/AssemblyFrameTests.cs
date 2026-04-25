using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class AssemblyFrameTests
{
    private static AssemblyDimension ValidDimension() =>
        AssemblyDimension.Create(800, 2000, 600).Value;

    [Fact]
    public void Create_ValidInputs_ReturnsSuccess()
    {
        var result = AssemblyFrame.Create(ValidDimension(), AffineTransform.Identity);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GravityDirection_IsNegativeZ()
    {
        var gravity = AssemblyFrame.GravityDirection;

        Assert.Equal(0.0, gravity.X);
        Assert.Equal(0.0, gravity.Y);
        Assert.Equal(-1.0, gravity.Z);
    }

    [Fact]
    public void Create_StoresCorrectDimension()
    {
        var dim = ValidDimension();
        var frame = AssemblyFrame.Create(dim, AffineTransform.Identity).Value;

        Assert.Equal(dim, frame.Dimension);
    }

    [Fact]
    public void Create_StoresAssemblyToWorldTransform()
    {
        var t = AffineTransform.Translation(Vector3.Create(10, 20, 30).Value).Value;
        var frame = AssemblyFrame.Create(ValidDimension(), t).Value;

        Assert.True(frame.AssemblyToWorld.IsApproximatelyEqualTo(t));
    }
}
