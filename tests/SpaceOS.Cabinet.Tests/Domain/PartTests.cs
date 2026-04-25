using System.Reflection;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class PartTests
{
    private static PartFrame ValidFrame()
    {
        var dim = PartDimension.Create(600, 560, 18).Value;
        return PartFrame.Create(AffineTransform.Identity, dim).Value;
    }

    // ── SEC-CAB-2: internal constructor enforcement ───────────────────────────

    [Fact]
    public void Part_InternalCtor_IsNotPubliclyAccessible()
    {
        var type = typeof(Part);
        var publicCtors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Empty(publicCtors);
    }

    // ── Properties via Skeleton factory ──────────────────────────────────────

    [Fact]
    public void Part_HasCorrectSkeletonId()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;
        var frame = ValidFrame();

        var part = skeleton.AddPart(frame, "mat-x").Value;

        Assert.Equal(skeleton.Id, part.SkeletonId);
    }

    [Fact]
    public void Part_MaterialReference_IsStoredCorrectly()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;

        var part = skeleton.AddPart(ValidFrame(), "SKU-123").Value;

        Assert.Equal("SKU-123", part.MaterialReference);
    }

    [Fact]
    public void Part_InitialAssignedRole_IsNull()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;

        var part = skeleton.AddPart(ValidFrame(), "mat").Value;

        Assert.Null(part.AssignedRole);
    }

    // ── Internal mutations (tested via reflection to verify they work) ────────

    [Fact]
    public void Part_AssignRole_SetsRole()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;

        // Use reflection to call internal method from test project
        var method = typeof(Part).GetMethod("AssignRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(part, new object[] { PartRole.Shelf });

        Assert.Equal(PartRole.Shelf, part.AssignedRole);
    }

    [Fact]
    public void Part_ClearAssignedRole_RemovesRole()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;

        var assignMethod = typeof(Part).GetMethod("AssignRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
        assignMethod.Invoke(part, new object[] { PartRole.Shelf });

        var clearMethod = typeof(Part).GetMethod("ClearAssignedRole", BindingFlags.NonPublic | BindingFlags.Instance)!;
        clearMethod.Invoke(part, null);

        Assert.Null(part.AssignedRole);
    }

    [Fact]
    public void Part_UpdateFrame_ChangesFrame()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;
        var part = skeleton.AddPart(ValidFrame(), "mat").Value;

        var newDim = PartDimension.Create(300, 560, 18).Value;
        var newFrame = PartFrame.Create(AffineTransform.Identity, newDim).Value;

        var method = typeof(Part).GetMethod("UpdateFrame", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(part, new object[] { newFrame });

        Assert.Equal(newFrame, part.Frame);
    }

    // ── BaseCuboid parts have correct roles ───────────────────────────────────

    [Fact]
    public void BaseCuboidParts_HaveAssignedRoles()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;

        Assert.Equal(PartRole.LeftSide, skeleton.BaseCuboid.LeftSide.AssignedRole);
        Assert.Equal(PartRole.RightSide, skeleton.BaseCuboid.RightSide.AssignedRole);
        Assert.Equal(PartRole.Bottom, skeleton.BaseCuboid.Bottom.AssignedRole);
        Assert.Equal(PartRole.Top, skeleton.BaseCuboid.Top.AssignedRole);
    }
}
