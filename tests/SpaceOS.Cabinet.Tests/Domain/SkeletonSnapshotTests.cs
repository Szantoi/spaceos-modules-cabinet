using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class SkeletonSnapshotTests
{
    private static Skeleton CreateValidSkeleton()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        return Skeleton.Create(Guid.NewGuid(), dim).Value;
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    [Fact]
    public void ToJson_ProducesNonEmptyString()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        var json = snapshot.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public void ToJson_ContainsSchemaVersion()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        var json = snapshot.ToJson();

        Assert.Contains("schemaVersion", json);
    }

    [Fact]
    public void SchemaVersion_Is0Dot3()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        // Cabinet 0.3: snapshot schema version is now "0.3"
        Assert.Equal("0.3", snapshot.SchemaVersion);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public void ToJson_FromJson_RoundTrip_PreservesId()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();

        var restored = SkeletonSnapshot.FromJson(json);

        Assert.True(restored.IsSuccess);
        Assert.Equal(skeleton.Id, restored.Value.Id);
    }

    [Fact]
    public void ToJson_FromJson_RoundTrip_PreservesTenantId()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();

        var restored = SkeletonSnapshot.FromJson(json);

        Assert.True(restored.IsSuccess);
        Assert.Equal(skeleton.TenantId, restored.Value.TenantId);
    }

    [Fact]
    public void ToJson_FromJson_RoundTrip_PreservesDimensions()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();

        var restored = SkeletonSnapshot.FromJson(json);

        Assert.True(restored.IsSuccess);
        Assert.Equal(snapshot.DimensionWidth, restored.Value.DimensionWidth);
        Assert.Equal(snapshot.DimensionHeight, restored.Value.DimensionHeight);
        Assert.Equal(snapshot.DimensionDepth, restored.Value.DimensionDepth);
    }

    // ── FromJson validation ───────────────────────────────────────────────────

    [Fact]
    public void FromJson_NullString_ReturnsInvalid()
    {
        var result = SkeletonSnapshot.FromJson(null!);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void FromJson_EmptyString_ReturnsInvalid()
    {
        var result = SkeletonSnapshot.FromJson(string.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void FromJson_WhitespaceString_ReturnsInvalid()
    {
        var result = SkeletonSnapshot.FromJson("   ");

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void FromJson_MalformedJson_ReturnsInvalid()
    {
        var result = SkeletonSnapshot.FromJson("{not valid json");

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
    }

    // ── Schema version validation ─────────────────────────────────────────────

    [Fact]
    public void FromJson_InvalidSchemaVersionFormat_ReturnsInvalid()
    {
        var skeleton = CreateValidSkeleton();
        var json = SkeletonSnapshot.FromSkeleton(skeleton).ToJson()
            .Replace("\"0.3\"", "\"bad-version\"");

        var result = SkeletonSnapshot.FromJson(json);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void FromJson_FutureMinorVersion_ReturnsSuccess()
    {
        // Minor version bump (0.9) should still be readable by the 0.x reader
        var skeleton = CreateValidSkeleton();
        var json = SkeletonSnapshot.FromSkeleton(skeleton).ToJson()
            .Replace("\"0.3\"", "\"0.9\"");

        var result = SkeletonSnapshot.FromJson(json);

        // 0.9 is still major version 0 — compatible
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void FromJson_FutureMajorVersion_ReturnsError()
    {
        // Major version 1.0 is incompatible
        var skeleton = CreateValidSkeleton();
        var json = SkeletonSnapshot.FromSkeleton(skeleton).ToJson()
            .Replace("\"0.3\"", "\"1.0\"");

        var result = SkeletonSnapshot.FromJson(json);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    // ── FromSkeleton projection ───────────────────────────────────────────────

    [Fact]
    public void FromSkeleton_SnapshotContainsAllParts()
    {
        var skeleton = CreateValidSkeleton();

        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        Assert.Equal(skeleton.Parts.Count, snapshot.Parts.Count);
    }

    [Fact]
    public void FromSkeleton_SnapshotContainsConnections()
    {
        var skeleton = CreateValidSkeleton();
        var frame = PartFrame.Create(AffineTransform.Identity, PartDimension.Create(200, 560, 18).Value).Value;
        var part = skeleton.AddPart(frame, "mat").Value;
        var geo = new ConnectionGeometry(SpaceOS.Cabinet.Abstractions.PartFace.Top, SpaceOS.Cabinet.Abstractions.PartEdge.BottomFront, 0);
        skeleton.AddConnection(skeleton.BaseCuboid.Bottom.Id, part.Id, geo);

        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        Assert.Single(snapshot.Connections);
    }

    // ── SkeletonReconstruction validation ────────────────────────────────────

    [Fact]
    public void FromSnapshot_TooManyParts_ReturnsError()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        // Synthesise a snapshot with too many parts
        var tooManyParts = new List<PartSnapshot>();
        for (int i = 0; i <= Skeleton.MaxPartsPerSkeleton; i++)
            tooManyParts.Add(new PartSnapshot { Id = Guid.NewGuid(), SkeletonId = snapshot.Id });

        var badSnapshot = new SkeletonSnapshot
        {
            SchemaVersion = "0.1",
            Id = snapshot.Id,
            TenantId = snapshot.TenantId,
            Version = snapshot.Version,
            DimensionWidth = snapshot.DimensionWidth,
            DimensionHeight = snapshot.DimensionHeight,
            DimensionDepth = snapshot.DimensionDepth,
            Parts = tooManyParts
        };

        var result = SkeletonReconstruction.FromSnapshot(badSnapshot);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    [Fact]
    public void FromSnapshot_CrossTenantPart_ReturnsError()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        // Inject a part with a different SkeletonId
        var crossTenantParts = snapshot.Parts
            .Select(p => new PartSnapshot
            {
                Id = p.Id,
                SkeletonId = Guid.NewGuid(), // mismatched
                MaterialReference = p.MaterialReference,
                PartCatalogReference = p.PartCatalogReference
            }).ToList();

        var badSnapshot = new SkeletonSnapshot
        {
            SchemaVersion = "0.1",
            Id = snapshot.Id,
            TenantId = snapshot.TenantId,
            Version = snapshot.Version,
            DimensionWidth = snapshot.DimensionWidth,
            DimensionHeight = snapshot.DimensionHeight,
            DimensionDepth = snapshot.DimensionDepth,
            Parts = crossTenantParts
        };

        var result = SkeletonReconstruction.FromSnapshot(badSnapshot);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    [Fact]
    public void FromSnapshot_ConnectionReferencingNonExistentPart_ReturnsError()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        var badConnections = new List<ConnectionSnapshot>
        {
            new ConnectionSnapshot
            {
                Id = Guid.NewGuid(),
                SkeletonId = snapshot.Id,
                ParentPartId = Guid.NewGuid(), // doesn't exist
                ChildPartId = Guid.NewGuid()   // doesn't exist
            }
        };

        var badSnapshot = new SkeletonSnapshot
        {
            SchemaVersion = "0.1",
            Id = snapshot.Id,
            TenantId = snapshot.TenantId,
            Version = snapshot.Version,
            DimensionWidth = snapshot.DimensionWidth,
            DimensionHeight = snapshot.DimensionHeight,
            DimensionDepth = snapshot.DimensionDepth,
            Parts = snapshot.Parts,
            Connections = badConnections
        };

        var result = SkeletonReconstruction.FromSnapshot(badSnapshot);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    [Fact]
    public void FromSnapshot_MismatchedConnectionSkeletonId_ReturnsError()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        // Create valid part IDs but wrong SkeletonId on connection
        var badConnections = new List<ConnectionSnapshot>
        {
            new ConnectionSnapshot
            {
                Id = Guid.NewGuid(),
                SkeletonId = Guid.NewGuid(), // mismatched
                ParentPartId = snapshot.Parts[0].Id,
                ChildPartId = snapshot.Parts[1].Id
            }
        };

        var badSnapshot = new SkeletonSnapshot
        {
            SchemaVersion = "0.1",
            Id = snapshot.Id,
            TenantId = snapshot.TenantId,
            Version = snapshot.Version,
            DimensionWidth = snapshot.DimensionWidth,
            DimensionHeight = snapshot.DimensionHeight,
            DimensionDepth = snapshot.DimensionDepth,
            Parts = snapshot.Parts,
            Connections = badConnections
        };

        var result = SkeletonReconstruction.FromSnapshot(badSnapshot);

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    [Fact]
    public void FromSnapshot_ValidSnapshot_ReturnsSuccess()
    {
        var skeleton = CreateValidSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        var result = SkeletonReconstruction.FromSnapshot(snapshot);

        Assert.True(result.IsSuccess);
    }
}
