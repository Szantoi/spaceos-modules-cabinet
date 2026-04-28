#pragma warning disable CS0618 // ApplyAll is obsolete — tests preserved for backward-compat verification
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Construction.DefaultRules;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using SpaceOS.Cabinet.Semantics;
using SpaceOS.Cabinet.Tests.Construction;
using Xunit;

namespace SpaceOS.Cabinet.Tests.CrossCutting;

/// <summary>
/// End-to-end smoke tests that exercise the full pipeline across all six packages:
/// Geometry → Domain → Machining → Construction → Semantics → Snapshot.
/// </summary>
public class SmokeTests
{
    private static AssemblyDimension DefaultDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    private static IConstructionContext DefaultContext()
        => new TestConstructionContext();

    // ── Full pipeline ─────────────────────────────────────────────────────────

    [Fact]
    public void FullFlow_CreateConnectConstructInfer_Succeeds()
    {
        // Arrange — Skeleton
        var skeleton = Skeleton.Create(Guid.NewGuid(), DefaultDimension()).Value;

        // Arrange — add a shelf part at mid-height (interior horizontal → Shelf)
        var shelfZ = 360.0;
        var shelfDim = PartDimension.Create(564, 560, 18).Value; // inner width = 600 - 2×18
        var shelfOffset = Vector3.Create(18, 0, shelfZ).Value;
        var shelfTransform = AffineTransform.Translation(shelfOffset).Value;
        var shelfFrame = PartFrame.Create(shelfTransform, shelfDim).Value;
        var shelfPart = skeleton.AddPart(shelfFrame, "lamiboard-18mm-white").Value;

        // Arrange — connect shelf to bottom
        var geo = new ConnectionGeometry(PartFace.Top, PartEdge.BottomFront, 0);
        var connectionResult = skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, shelfPart.Id, geo);
        Assert.True(connectionResult.IsSuccess);

        // Act — construction rule engine (at least one rule)
        var engine = new ConstructionRuleEngine([new MaterialDefaultRule()]);
        var engineResult = engine.ApplyAll(skeleton, DefaultContext());
        Assert.True(engineResult.IsSuccess);

        // Act — semantic inference
        var svc = new SemanticInferenceService();
        var roles = svc.InferAll(skeleton);

        // Assert — inference
        Assert.Equal(PartRole.LeftSide, roles[skeleton.BaseCuboid.LeftSide.Id]);
        Assert.Equal(PartRole.RightSide, roles[skeleton.BaseCuboid.RightSide.Id]);
        Assert.Equal(PartRole.Bottom, roles[skeleton.BaseCuboid.Bottom.Id]);
        Assert.Equal(PartRole.Top, roles[skeleton.BaseCuboid.Top.Id]);
        Assert.Equal(PartRole.Shelf, roles[shelfPart.Id]);

        // Act — snapshot roundtrip
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();
        var restored = SkeletonSnapshot.FromJson(json);
        Assert.True(restored.IsSuccess);
        Assert.Equal(snapshot.Id, restored.Value.Id);
    }

    // ── Snapshot roundtrip ───────────────────────────────────────────────────

    [Fact]
    public void SnapshotRoundTrip_PreservesIdentity()
    {
        var tenantId = Guid.NewGuid();
        var skeleton = Skeleton.Create(tenantId, DefaultDimension()).Value;

        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();
        var restoredResult = SkeletonSnapshot.FromJson(json);

        Assert.True(restoredResult.IsSuccess);
        var restored = restoredResult.Value;
        Assert.Equal(snapshot.Id, restored.Id);
        Assert.Equal(snapshot.TenantId, restored.TenantId);
        Assert.Equal(snapshot.Version, restored.Version);
        Assert.Equal(snapshot.SchemaVersion, restored.SchemaVersion);
    }

    [Fact]
    public void SnapshotToJson_CalledTwice_ProducesIdenticalOutput()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), DefaultDimension()).Value;
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        var json1 = snapshot.ToJson();
        var json2 = snapshot.ToJson();

        Assert.Equal(json1, json2);
    }

    // ── Meta-package reachability ─────────────────────────────────────────────

    [Fact]
    public void MetaPackage_TypesFromAllPackages_AreAccessible()
    {
        // Geometry
        var vec = Vector3.Create(1, 2, 3).Value;
        Assert.True(vec.IsValid());

        // Abstractions
        var role = PartRole.Shelf;
        Assert.Equal(PartRole.Shelf, role);

        // Domain
        var skeleton = Skeleton.Create(Guid.NewGuid(), DefaultDimension()).Value;
        Assert.NotEqual(Guid.Empty, skeleton.Id);

        // Machining — create a machining feature
        var subject = new PlaneSubject(skeleton.BaseCuboid.LeftSide.Id, PartFace.Left);
        var feature = MachiningFeature.Create(
            subject,
            MachiningOperation.Drill,
            new MachiningParameters(depth: 10, diameter: 5)).Value;
        Assert.NotEqual(Guid.Empty, feature.Id);

        // Construction
        var engine = new ConstructionRuleEngine(Array.Empty<IConstructionRule>());
        var engineResult = engine.ApplyAll(skeleton, DefaultContext());
        Assert.True(engineResult.IsSuccess);

        // Semantics
        var svc = new SemanticInferenceService();
        var inferred = svc.InferRole(skeleton.BaseCuboid.Bottom, skeleton);
        Assert.Equal(PartRole.Bottom, inferred);
    }

    // ── Snapshot preserves part count ────────────────────────────────────────

    [Fact]
    public void SnapshotRoundTrip_PreservesPartAndConnectionCount()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), DefaultDimension()).Value;

        var partDim = PartDimension.Create(564, 560, 18).Value;
        var partFrame = PartFrame.Create(
            AffineTransform.Translation(Vector3.Create(18, 0, 360).Value).Value,
            partDim).Value;
        skeleton.AddPart(partFrame, "mat");

        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();
        var restored = SkeletonSnapshot.FromJson(json).Value;

        Assert.Equal(skeleton.Parts.Count, restored.Parts.Count);
        Assert.Equal(skeleton.Connections.Count, restored.Connections.Count);
    }
}
