using System.Security.Cryptography;
using System.Text;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Domain;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.CrossCutting;

/// <summary>
/// Verifies that the full Cabinet 0.3 pipeline is deterministic:
/// running the same input 10 times must produce SHA-256–identical output.
/// </summary>
public class DeterminismTests
{
    private static readonly Guid FixedTenantId = new("d3e4f5a6-0001-0001-0001-000000000001");
    private static readonly Guid FixedActorId  = new("b1c2d3e4-0002-0002-0002-000000000002");

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static Skeleton BuildDeterministicSkeleton()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        // Use a fixed tenant ID so the skeleton JSON is identical across runs.
        var skeleton = Skeleton.Create(FixedTenantId, dim).Value;

        var partDim = PartDimension.Create(564, 560, 18).Value;
        var transform = AffineTransform.Translation(Vector3.Create(18, 0, 360).Value).Value;
        var frame = PartFrame.Create(transform, partDim).Value;
        skeleton.AddPart(frame, "lamiboard-18mm");

        return skeleton;
    }

    private static TenantStandard BuildDeterministicTenantStandard()
    {
        var materials  = new MaterialDefaults("lamiboard-18mm", 18.0, "hdf-5mm", 5.0);
        var lineBore   = new LineBoreSettings(true, 38.0, 32.0, 5.0);
        var thresholds = new RuleThresholds(2000.0, 800.0);
        return TenantStandard.Create(
            FixedTenantId, materials, BackPanelAttachmentDefault.Groove,
            TopType.FullTop, lineBore, thresholds, FixedActorId).Value;
    }

    private static IConstructionContext BuildContext(AssemblyDimension dim)
        => new DeterministicConstructionContext(dim);

    private sealed class DeterministicRule : IConstructionRule
    {
        public string RuleId => "Det-Single";
        public string Description => "Deterministic single-feature rule.";

        public ConstructionRuleResult Apply(Skeleton skeleton, IConstructionContext ctx, CancellationToken ct)
        {
            var leftSideId = skeleton.BaseCuboid.LeftSide.Id;
            var feature = MachiningFeature.Create(
                new PlaneSubject(leftSideId, PartFace.Left),
                MachiningOperation.Drill,
                new MachiningParameters(depth: 10, diameter: 5)).Value;
            return new ConstructionRuleResult([feature], Array.Empty<DesignAdvisory>());
        }
    }

    private sealed class DeterministicConstructionContext(AssemblyDimension dim) : IConstructionContext
    {
        public ITenantStandardProvider TenantStandard { get; } = new DeterministicTenantProvider();
        public AssemblyDimension AssemblyDimension { get; } = dim;
    }

    private sealed class DeterministicTenantProvider : ITenantStandardProvider
    {
        public Guid TenantId => FixedTenantId;
        public string DefaultCarcassMaterial => "lamiboard-18mm";
        public double DefaultCarcassThickness => 18.0;
        public string DefaultBackPanelMaterial => "hdf-5mm";
        public double DefaultBackPanelThickness => 5.0;
        public BackPanelAttachmentDefault BackPanelAttachment => BackPanelAttachmentDefault.Groove;
        public TopType TopType => TopType.FullTop;
        public bool LineBoreEnabled => true;
        public double LineBoreFirstHoleOffset => 38.0;
        public double LineBoreSpacing => 32.0;
        public double LineBoreDiameter => 5.0;
        public double TallCabinetHeightThreshold => 2000.0;
        public double LongShelfThreshold => 800.0;
        public IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides { get; }
            = new Dictionary<string, AdvisorySeverity>();
    }

    private static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    // ── Tests ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Snapshot_TenRuns_ProduceIdenticalSha256Hash()
    {
        // Arrange — fixed skeleton (same ID each run via deterministic ctor)
        const int runs = 10;
        var skeletons = Enumerable.Range(0, runs)
            .Select(_ => BuildDeterministicSkeleton())
            .ToList();

        // Act
        var hashes = skeletons
            .Select(s => SkeletonSnapshot.FromSkeleton(s).ToJson())
            // The skeleton ID is random per-instance so we normalise it before hashing:
            // replace the actual GUID with a placeholder so structural equality is compared.
            .Select(json => NormaliseSkeletonJson(json))
            .Select(Sha256)
            .ToList();

        // Assert — all 10 hashes must be identical
        var first = hashes[0];
        Assert.All(hashes, h => Assert.Equal(first, h));
    }

    [Fact]
    public async Task ConstructionEngineOutput_TenRuns_ProduceIdenticalMachiningCount()
    {
        // Arrange
        const int runs = 10;
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var engine = new ConstructionRuleEngine([new DeterministicRule()]);

        // Act — run 10 times with freshly-created skeletons
        var results = new int[runs];
        for (var i = 0; i < runs; i++)
        {
            var skeleton = BuildDeterministicSkeleton();
            var ctx      = BuildContext(dim);
            var r        = await engine.ApplyAllAsync(skeleton, ctx);
            results[i]   = r.Value.AllGeneratedMachinings.Count;
        }

        // Assert — machining count must be identical for all runs
        Assert.All(results, count => Assert.Equal(results[0], count));
    }

    [Fact]
    public async Task ConstructionEngineWithTenantStandard_TenRuns_ProduceIdenticalAdvisoryCount()
    {
        // Arrange
        const int runs = 10;
        var dim     = AssemblyDimension.Create(600, 720, 560).Value;
        var ts      = BuildDeterministicTenantStandard();
        var engine  = new ConstructionRuleEngine([new DeterministicRule()]);

        // Act
        var advisoryCounts = new int[runs];
        for (var i = 0; i < runs; i++)
        {
            var skeleton = BuildDeterministicSkeleton();
            var ctx      = BuildContext(dim);
            var r        = await engine.ApplyAllAsync(skeleton, ctx, ts);
            advisoryCounts[i] = r.Value.AllAdvisories.Count;
        }

        // Assert
        Assert.All(advisoryCounts, count => Assert.Equal(advisoryCounts[0], count));
    }

    [Fact]
    public void SnapshotJson_TenRuns_NormalisedHashIsIdentical()
    {
        // Variant that normalises the schema version marker and verifies structural determinism.
        const int runs = 10;
        var dim = AssemblyDimension.Create(600, 720, 560).Value;

        var hashes = Enumerable.Range(0, runs)
            .Select(_ => Skeleton.Create(FixedTenantId, dim).Value)
            .Select(s => SkeletonSnapshot.FromSkeleton(s).ToJson())
            .Select(NormaliseSkeletonJson)
            .Select(Sha256)
            .ToList();

        var first = hashes[0];
        Assert.All(hashes, h => Assert.Equal(first, h));
    }

    [Fact]
    public void SnapshotSchemaVersion_TenRuns_AlwaysReturns_0_3()
    {
        const int runs = 10;
        var dim = AssemblyDimension.Create(600, 720, 560).Value;

        var versions = Enumerable.Range(0, runs)
            .Select(_ => Skeleton.Create(FixedTenantId, dim).Value)
            .Select(s => SkeletonSnapshot.FromSkeleton(s).SchemaVersion)
            .ToList();

        Assert.All(versions, v => Assert.Equal("0.3", v));
    }

    // ── Private utilities ────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces GUID-valued fields that are legitimately non-deterministic (skeleton id, part ids)
    /// with stable placeholders so the SHA-256 hash reflects structural identity only.
    /// </summary>
    private static string NormaliseSkeletonJson(string json)
    {
        // Replace all UUID4 values in the JSON with a single placeholder so
        // freshly-created skeletons with different random IDs still hash identically
        // when their structure is the same.
        var result = System.Text.RegularExpressions.Regex.Replace(
            json,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
            "00000000-0000-0000-0000-000000000000");

        return result;
    }
}
