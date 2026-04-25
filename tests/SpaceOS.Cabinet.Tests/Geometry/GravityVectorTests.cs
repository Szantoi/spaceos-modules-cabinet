using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Geometry;

public class GravityVectorTests
{
    [Fact]
    public void Default_IsNegativeZ()
    {
        var gravity = GravityVector.Default;

        Assert.Equal(0.0, gravity.X);
        Assert.Equal(0.0, gravity.Y);
        Assert.Equal(-1.0, gravity.Z);
    }

    [Fact]
    public void Default_IsValid()
    {
        Assert.True(GravityVector.Default.IsValid());
    }

    [Fact]
    public void Default_HasUnitLength()
    {
        Assert.Equal(1.0, GravityVector.Default.Length(), 10);
    }

    [Fact]
    public void Default_MatchesAssemblyFrameGravityDirection()
    {
        Assert.True(GravityVector.Default.IsApproximatelyEqualTo(AssemblyFrame.GravityDirection));
    }
}
