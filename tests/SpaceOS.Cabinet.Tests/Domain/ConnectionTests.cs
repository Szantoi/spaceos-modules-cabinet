using System.Reflection;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class ConnectionTests
{
    private static AssemblyDimension ValidDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    private static PartFrame ValidFrame()
    {
        var dim = PartDimension.Create(200, 560, 18).Value;
        return PartFrame.Create(AffineTransform.Identity, dim).Value;
    }

    // ── SEC-CAB-2: internal constructor enforcement ───────────────────────────

    [Fact]
    public void Connection_InternalCtor_IsNotPubliclyAccessible()
    {
        var type = typeof(Connection);
        var publicCtors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Empty(publicCtors);
    }

    // ── Default joint type ────────────────────────────────────────────────────

    [Fact]
    public void Connection_DefaultJointType_IsFaceEdgeButt()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;
        var geo = new ConnectionGeometry(PartFace.Top, PartEdge.BottomFront, 0);

        var connection = skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, part.Id, geo).Value;

        Assert.Equal(JointType.FaceEdgeButt, connection.JointType);
    }

    // ── Internal mutation ─────────────────────────────────────────────────────

    [Fact]
    public void Connection_SetJointType_ChangesType()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;
        var geo = new ConnectionGeometry(PartFace.Top, PartEdge.BottomFront, 0);
        var connection = skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, part.Id, geo).Value;

        var method = typeof(Connection).GetMethod("SetJointType", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(connection, new object[] { JointType.Dado });

        Assert.Equal(JointType.Dado, connection.JointType);
    }

    // ── SkeletonId binding ────────────────────────────────────────────────────

    [Fact]
    public void Connection_HasCorrectSkeletonId()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;
        var geo = new ConnectionGeometry(PartFace.Top, PartEdge.BottomFront, 0);

        var connection = skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, part.Id, geo).Value;

        Assert.Equal(skeleton.Id, connection.SkeletonId);
    }

    // ── Geometry stored correctly ──────────────────────────────────────────────

    [Fact]
    public void Connection_Geometry_IsStoredCorrectly()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;
        var geo = new ConnectionGeometry(PartFace.Back, PartEdge.BackLeft, 32.5);

        var connection = skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, part.Id, geo).Value;

        Assert.Equal(PartFace.Back, connection.Geometry.ParentFace);
        Assert.Equal(PartEdge.BackLeft, connection.Geometry.ChildEdge);
        Assert.Equal(32.5, connection.Geometry.EdgeOffset);
    }
}
