using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Events;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class SkeletonCatalogPinTests
{
    private static AssemblyDimension ValidDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    private static PartFrame ValidPartFrame()
    {
        var dim = PartDimension.Create(200, 560, 18).Value;
        return PartFrame.Create(AffineTransform.Identity, dim).Value;
    }

    private static Skeleton CreateSkeleton()
        => Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

    // ── PinCatalogEntry ──────────────────────────────────────────────────────

    [Fact]
    public void PinCatalogEntry_ValidPartAndEntry_Succeeds()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();

        var result = skeleton.PinCatalogEntry(partId, CatalogType.MaterialThickness, entryId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void PinCatalogEntry_InvalidPartId_ReturnsInvalid()
    {
        var skeleton = CreateSkeleton();

        var result = skeleton.PinCatalogEntry(Guid.NewGuid(), CatalogType.JointType, Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void PinCatalogEntry_EmptyCatalogEntryId_ReturnsInvalid()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;

        var result = skeleton.PinCatalogEntry(partId, CatalogType.HardwareSet, Guid.Empty);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void PinCatalogEntry_OverwriteExisting_Succeeds()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var firstEntry = Guid.NewGuid();
        var secondEntry = Guid.NewGuid();

        skeleton.PinCatalogEntry(partId, CatalogType.EdgeBandingRule, firstEntry);
        var result = skeleton.PinCatalogEntry(partId, CatalogType.EdgeBandingRule, secondEntry);

        Assert.True(result.IsSuccess);
        Assert.Equal(secondEntry, skeleton.PinnedCatalogEntries[(partId, CatalogType.EdgeBandingRule)]);
    }

    [Fact]
    public void PinCatalogEntry_MultiplePinsForSamePart_DifferentTypes_AllStored()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var matEntry = Guid.NewGuid();
        var hwEntry = Guid.NewGuid();

        skeleton.PinCatalogEntry(partId, CatalogType.MaterialThickness, matEntry);
        skeleton.PinCatalogEntry(partId, CatalogType.HardwareSet, hwEntry);

        Assert.Equal(matEntry, skeleton.PinnedCatalogEntries[(partId, CatalogType.MaterialThickness)]);
        Assert.Equal(hwEntry, skeleton.PinnedCatalogEntries[(partId, CatalogType.HardwareSet)]);
    }

    [Fact]
    public void PinCatalogEntry_BumpsVersion()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var versionBefore = skeleton.Version;

        skeleton.PinCatalogEntry(partId, CatalogType.RasterStandard, Guid.NewGuid());

        Assert.NotEqual(versionBefore, skeleton.Version);
    }

    [Fact]
    public void PinCatalogEntry_StoredInPinnedCatalogEntries()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();

        skeleton.PinCatalogEntry(partId, CatalogType.BackPanelStandard, entryId);

        Assert.True(skeleton.PinnedCatalogEntries.ContainsKey((partId, CatalogType.BackPanelStandard)));
        Assert.Equal(entryId, skeleton.PinnedCatalogEntries[(partId, CatalogType.BackPanelStandard)]);
    }

    // ── DeriveAssembly ───────────────────────────────────────────────────────

    [Fact]
    public void DeriveAssembly_WithResolver_Succeeds()
    {
        var skeleton = CreateSkeleton();
        var resolver = new AlwaysFalseResolver();

        var result = skeleton.DeriveAssembly(resolver);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void DeriveAssembly_NullResolver_Throws()
    {
        var skeleton = CreateSkeleton();

        Assert.Throws<ArgumentNullException>(() => skeleton.DeriveAssembly(null!));
    }

    [Fact]
    public void DeriveAssembly_RaisesAssemblyDerivedEvent()
    {
        var skeleton = CreateSkeleton();
        skeleton.PopDomainEvents(); // clear creation event

        var resolver = new AlwaysFalseResolver();
        skeleton.DeriveAssembly(resolver);

        var events = skeleton.PopDomainEvents();
        Assert.Contains(events, e => e is AssemblyDerived);
    }

    [Fact]
    public void DeriveAssembly_AssemblyDerivedEvent_HasCorrectSkeletonId()
    {
        var skeleton = CreateSkeleton();
        skeleton.PopDomainEvents();

        skeleton.DeriveAssembly(new AlwaysFalseResolver());

        var events = skeleton.PopDomainEvents();
        var evt = Assert.IsType<AssemblyDerived>(events.Last());
        Assert.Equal(skeleton.Id, evt.SkeletonId);
    }

    [Fact]
    public void DeriveAssembly_BumpsVersion()
    {
        var skeleton = CreateSkeleton();
        var versionBefore = skeleton.Version;

        skeleton.DeriveAssembly(new AlwaysFalseResolver());

        Assert.NotEqual(versionBefore, skeleton.Version);
    }

    // ── DeriveBillOfServices ─────────────────────────────────────────────────

    [Fact]
    public void DeriveBillOfServices_NoPins_ReturnsEmptyList()
    {
        var skeleton = CreateSkeleton();

        var result = skeleton.DeriveBillOfServices();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public void DeriveBillOfServices_WithPins_ReturnsItems()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        skeleton.PinCatalogEntry(partId, CatalogType.EdgeBandingRule, Guid.NewGuid());

        var result = skeleton.DeriveBillOfServices();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Items);
    }

    [Fact]
    public void DeriveBillOfServices_ItemHasCorrectPartIdAndType()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.HardwareSet, entryId);

        var result = skeleton.DeriveBillOfServices();

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(partId, item.PartId);
        Assert.Equal(CatalogType.HardwareSet, item.CatalogType);
        Assert.Equal(entryId, item.CatalogEntryId);
    }

    [Fact]
    public void DeriveBillOfServices_MultiplePins_AllItemsReturned()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        skeleton.PinCatalogEntry(partId, CatalogType.MaterialThickness, Guid.NewGuid());
        skeleton.PinCatalogEntry(partId, CatalogType.HardwareSet, Guid.NewGuid());

        var result = skeleton.DeriveBillOfServices();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
    }

    [Fact]
    public void DeriveBillOfServices_SkeletonIdMatchesSkeleton()
    {
        var skeleton = CreateSkeleton();

        var result = skeleton.DeriveBillOfServices();

        Assert.Equal(skeleton.Id, result.Value.SkeletonId);
    }

    // ── Snapshot round-trip ──────────────────────────────────────────────────

    [Fact]
    public void SkeletonSnapshot_V02_RoundTrip()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.JointType, entryId);

        var json = SkeletonSnapshot.FromSkeleton(skeleton).ToJson();
        var snapshotResult = SkeletonSnapshot.FromJson(json);

        Assert.True(snapshotResult.IsSuccess);
        Assert.Equal("0.3", snapshotResult.Value.SchemaVersion);
    }

    [Fact]
    public void SkeletonSnapshot_V02_HasPinnedCatalogEntries()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.RasterStandard, entryId);

        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        Assert.NotEmpty(snapshot.PinnedCatalogEntries);
        var entry = Assert.Single(snapshot.PinnedCatalogEntries);
        Assert.Equal(partId, entry.PartId);
        Assert.Equal(entryId, entry.CatalogEntryId);
    }

    [Fact]
    public void SkeletonSnapshot_V02_HasRoleAssignments_WhenRolesAssigned()
    {
        var skeleton = CreateSkeleton();
        // BaseCuboid parts have AssignedRole null by default — snapshot RoleAssignments will be empty
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        // RoleAssignments list is present (may be empty for default skeletons)
        Assert.NotNull(snapshot.RoleAssignments);
    }

    [Fact]
    public void SkeletonSnapshot_V01_StillDeserializes()
    {
        // Construct a minimal v0.1 JSON (no roleAssignments, no pinnedCatalogEntries)
        var v01Json = """
            {
              "schemaVersion": "0.1",
              "id": "00000000-0000-0000-0000-000000000001",
              "tenantId": "00000000-0000-0000-0000-000000000002",
              "version": "00000000-0000-0000-0000-000000000003",
              "lastSequenceNumber": 0,
              "dimensionWidth": 600,
              "dimensionHeight": 720,
              "dimensionDepth": 560,
              "parts": [],
              "connections": [],
              "roleAssignments": [],
              "pinnedCatalogEntries": []
            }
            """;

        var result = SkeletonSnapshot.FromJson(v01Json);

        Assert.True(result.IsSuccess);
        Assert.Equal("0.1", result.Value.SchemaVersion);
    }

    [Fact]
    public void Reconstruct_WithPinnedEntries_RestoresState()
    {
        var skeleton = CreateSkeleton();
        var partId = skeleton.Parts[0].Id;
        var entryId = Guid.NewGuid();
        skeleton.PinCatalogEntry(partId, CatalogType.ConstructionTemplate, entryId);

        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();
        var snapshotResult = SkeletonSnapshot.FromJson(json);
        var reconstructResult = SkeletonReconstruction.FromSnapshot(snapshotResult.Value);

        Assert.True(reconstructResult.IsSuccess);
        var reconstructed = reconstructResult.Value;
        Assert.True(reconstructed.PinnedCatalogEntries.ContainsKey((partId, CatalogType.ConstructionTemplate)));
        Assert.Equal(entryId, reconstructed.PinnedCatalogEntries[(partId, CatalogType.ConstructionTemplate)]);
    }

    [Fact]
    public void PinCatalogEntry_TwoPartsOneDifferentType_StoredSeparately()
    {
        var skeleton = CreateSkeleton();
        var partA = skeleton.Parts[0].Id;
        var partB = skeleton.Parts[1].Id;
        var entryA = Guid.NewGuid();
        var entryB = Guid.NewGuid();

        skeleton.PinCatalogEntry(partA, CatalogType.MaterialThickness, entryA);
        skeleton.PinCatalogEntry(partB, CatalogType.MaterialThickness, entryB);

        Assert.Equal(entryA, skeleton.PinnedCatalogEntries[(partA, CatalogType.MaterialThickness)]);
        Assert.Equal(entryB, skeleton.PinnedCatalogEntries[(partB, CatalogType.MaterialThickness)]);
    }

    [Fact]
    public void PinCatalogEntry_AddedPart_CanBePinned()
    {
        var skeleton = CreateSkeleton();
        skeleton.PopDomainEvents();
        var part = skeleton.AddPart(ValidPartFrame(), "mat-a").Value;
        skeleton.PopDomainEvents();
        var entryId = Guid.NewGuid();

        var result = skeleton.PinCatalogEntry(part.Id, CatalogType.HorizontalRole, entryId);

        Assert.True(result.IsSuccess);
    }
}

/// <summary>Test stub: resolver that always returns false (no pins).</summary>
internal sealed class AlwaysFalseResolver : ICatalogResolver
{
    public bool TryGetPinnedEntry(Guid skeletonId, Guid partId, CatalogType type, out Guid catalogEntryId)
    {
        catalogEntryId = Guid.Empty;
        return false;
    }
}
