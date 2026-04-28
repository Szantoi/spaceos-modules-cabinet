using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Application;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Domain;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.CrossCutting;

/// <summary>
/// Full-pipeline smoke tests for Cabinet 0.3: Skeleton, TenantStandard, Construction, Federation, Snapshot.
/// </summary>
public class SmokePipelineTests
{
    private static readonly Guid TenantId = new("a1b2c3d4-0001-0001-0001-000000000001");
    private static readonly Guid ActorId = Guid.NewGuid();
    private const string ValidPayload = """{"role":"Shelf","priority":1}""";
    private const string ValidSchema = "horizontal_role/v1";

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static Skeleton CreateSkeleton()
        => Skeleton.Create(TenantId, AssemblyDimension.Create(600, 720, 560).Value).Value;

    private static TenantStandard CreateTenantStandard()
    {
        var materials = new MaterialDefaults("lamiboard-18mm", 18.0, "hdf-5mm", 5.0);
        var lineBore = new LineBoreSettings(true, 38.0, 32.0, 5.0);
        var thresholds = new RuleThresholds(2000.0, 800.0);
        return TenantStandard.Create(
            TenantId, materials, BackPanelAttachmentDefault.Groove,
            TopType.FullTop, lineBore, thresholds, ActorId).Value;
    }

    private static CatalogEntry CreateDraftEntry()
        => CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole, "Shelf Standard", "Desc",
            CatalogVisibility.Private, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;

    private sealed class NoOpRule : IConstructionRule
    {
        public string RuleId => "Smoke-NoOp";
        public string Description => "No-op rule for smoke tests.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => ConstructionRuleResult.Empty;
    }

    private sealed class SingleFeatureRule : IConstructionRule
    {
        public string RuleId => "Smoke-Single";
        public string Description => "Emits one machining feature.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var feature = MachiningFeature.Create(
                new PlaneSubject(s.BaseCuboid.LeftSide.Id, PartFace.Left),
                MachiningOperation.Drill,
                new MachiningParameters(depth: 10, diameter: 5)).Value;
            return new ConstructionRuleResult([feature], Array.Empty<DesignAdvisory>());
        }
    }

    private sealed class SeverityRule(string ruleId, AdvisorySeverity severity) : IConstructionRule
    {
        public string RuleId => ruleId;
        public string Description => $"Emits {severity} advisory.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var adv = new DesignAdvisory(ruleId, severity, "Skeleton", "Smoke advisory.", null);
            return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), [adv]);
        }
    }

    private sealed class TestConstructionCtx(ITenantStandardProvider provider) : IConstructionContext
    {
        public ITenantStandardProvider TenantStandard => provider;
        public AssemblyDimension AssemblyDimension => AssemblyDimension.Create(600, 720, 560).Value;
    }

    private sealed class TestTenantProvider : ITenantStandardProvider
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public string DefaultCarcassMaterial { get; init; } = "lamiboard-18mm";
        public double DefaultCarcassThickness { get; init; } = 18.0;
        public string DefaultBackPanelMaterial { get; init; } = "hdf-5mm";
        public double DefaultBackPanelThickness { get; init; } = 5.0;
        public BackPanelAttachmentDefault BackPanelAttachment { get; init; } = BackPanelAttachmentDefault.Groove;
        public TopType TopType { get; init; } = TopType.FullTop;
        public bool LineBoreEnabled { get; init; } = true;
        public double LineBoreFirstHoleOffset { get; init; } = 38.0;
        public double LineBoreSpacing { get; init; } = 32.0;
        public double LineBoreDiameter { get; init; } = 5.0;
        public double TallCabinetHeightThreshold { get; init; } = 2000.0;
        public double LongShelfThreshold { get; init; } = 800.0;
        public IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides { get; init; }
            = new Dictionary<string, AdvisorySeverity>();
    }

    // ── Tests ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullPipeline_CreateSkeletonAndApplyRules_Succeeds()
    {
        var skeleton = CreateSkeleton();
        var engine = new ConstructionRuleEngine([new SingleFeatureRule()]);

        var result = await engine.ApplyAllAsync(skeleton, new TestConstructionCtx(new TestTenantProvider()));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public async Task FullPipeline_SnapshotRoundTrip_0_3_Schema()
    {
        var skeleton = CreateSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);
        var json = snapshot.ToJson();

        var restored = SkeletonSnapshot.FromJson(json);

        Assert.True(restored.IsSuccess);
        Assert.Equal(skeleton.Id, restored.Value.Id);
        Assert.Equal(skeleton.TenantId, restored.Value.TenantId);
    }

    [Fact]
    public async Task FullPipeline_SnapshotSchemaIs_0_3()
    {
        var skeleton = CreateSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        Assert.Equal("0.3", snapshot.SchemaVersion);
    }

    [Fact]
    public void FullPipeline_Migrate_0_2_To_0_3_AddsAppliedTenantStandard()
    {
        // Build a v0.2 snapshot by migrating from v0.1
        var migrator_01_02 = new SnapshotMigrator_0_1_to_0_2();
        var migrator_02_03 = new SnapshotMigrator_0_2_to_0_3();

        var skeleton = CreateSkeleton();
        // Start at v0.1 by creating a v0.3 snapshot and downgrading the version field
        var json03 = SkeletonSnapshot.FromSkeleton(skeleton).ToJson();
        // Simulate v0.1 JSON by replacing the schemaVersion
        var json01 = json03
            .Replace("\"schemaVersion\":\"0.3\"", "\"schemaVersion\":\"0.1\"")
            .Replace(",\"appliedTenantStandard\":null", "")
            .Replace(",\"roleAssignments\":[]", "")
            .Replace(",\"pinnedCatalogEntries\":[]", "");

        var step1 = migrator_01_02.Migrate(json01, "0.1", "0.2");
        Assert.True(step1.IsSuccess);

        var step2 = migrator_02_03.Migrate(step1.Value, "0.2", "0.3");
        Assert.True(step2.IsSuccess);
        Assert.Contains("appliedTenantStandard", step2.Value);
    }

    [Fact]
    public void FullPipeline_CatalogEntry_RateAndFlag_Succeeds()
    {
        var ownerTenantId = Guid.NewGuid();
        var raterTenantId = Guid.NewGuid();
        var entry = CatalogEntry.CreateDraft(
            ownerTenantId, ActorId, CatalogType.HorizontalRole, "Rateable Entry", "Desc",
            CatalogVisibility.Private, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry.Approve(ActorId);
        entry.Publish(ActorId);

        var rating = CatalogEntryRating.Create(entry.Id, raterTenantId, ActorId, 4, "Good", ownerTenantId).Value;
        var ingestResult = entry.IngestRating(rating);

        Assert.True(ingestResult.IsSuccess);
        Assert.Equal(4, rating.Stars);
        Assert.Equal(entry.Id, rating.CatalogEntryId);
    }

    [Fact]
    public void FullPipeline_CatalogEntry_IsAutoHidden_WhenFlagCount3()
    {
        var ownerTenantId = Guid.NewGuid();
        var entry = CatalogEntry.CreateDraft(
            ownerTenantId, ActorId, CatalogType.HorizontalRole, "Flaggable", "Desc",
            CatalogVisibility.Private, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry.Approve(ActorId);
        entry.Publish(ActorId);

        // Create and ingest 3 flags from different reporter tenants
        var flag1 = CatalogEntryFlag.Create(entry.Id, Guid.NewGuid(), Guid.NewGuid(), FlagReason.Spam, null, ownerTenantId).Value;
        var flag2 = CatalogEntryFlag.Create(entry.Id, Guid.NewGuid(), Guid.NewGuid(), FlagReason.Inappropriate, null, ownerTenantId).Value;
        var flag3 = CatalogEntryFlag.Create(entry.Id, Guid.NewGuid(), Guid.NewGuid(), FlagReason.DangerousConstruction, null, ownerTenantId).Value;
        entry.IngestFlag(flag1);
        entry.IngestFlag(flag2);
        entry.IngestFlag(flag3);

        Assert.True(entry.IsAutoHidden);
    }

    [Fact]
    public async Task FullPipeline_TenantStandard_Create_And_UseInRuleEngine()
    {
        var ts = CreateTenantStandard();
        var engine = new ConstructionRuleEngine([new NoOpRule()]);

        var result = await engine.ApplyAllAsync(CreateSkeleton(), new TestConstructionCtx(new TestTenantProvider()), ts);

        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, ts.TenantId);
    }

    [Fact]
    public void FullPipeline_Determinism_SameInputSameHash()
    {
        var payload = ValidPayload;

        var entry1 = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole, "Entry A", "Desc",
            CatalogVisibility.Private, payload, ValidSchema, NullCatalogPayloadValidator.Instance).Value;

        var entry2 = CatalogEntry.CreateDraft(
            TenantId, ActorId, CatalogType.HorizontalRole, "Entry B", "Desc",
            CatalogVisibility.Private, payload, ValidSchema, NullCatalogPayloadValidator.Instance).Value;

        Assert.Equal(entry1.ContentHash, entry2.ContentHash);
    }

    [Fact]
    public void FullPipeline_SnapshotMigrator_Chain_0_1_To_0_2_To_0_3()
    {
        var migrator01 = new SnapshotMigrator_0_1_to_0_2();
        var migrator02 = new SnapshotMigrator_0_2_to_0_3();

        Assert.True(migrator01.CanMigrate("0.1", "0.2"));
        Assert.True(migrator02.CanMigrate("0.2", "0.3"));
        Assert.False(migrator01.CanMigrate("0.2", "0.3"));
        Assert.False(migrator02.CanMigrate("0.1", "0.2"));
    }

    [Fact]
    public async Task FullPipeline_ApplyAllAsync_WithTenantStandardContext_Succeeds()
    {
        var ts = CreateTenantStandard();
        ts.OverrideRuleSeverity("Smoke-Severity", AdvisorySeverity.Info, ActorId, 1);

        var engine = new ConstructionRuleEngine([new SeverityRule("Smoke-Severity", AdvisorySeverity.Warning)]);

        var result = await engine.ApplyAllAsync(CreateSkeleton(), new TestConstructionCtx(new TestTenantProvider()), ts);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.Severity == AdvisorySeverity.Info);
    }

    [Fact]
    public void FullPipeline_CatalogEntryCluster_CreateAndAddMember()
    {
        var entryId1 = Guid.NewGuid();
        var entryId2 = Guid.NewGuid();
        var cluster = CatalogEntryCluster.CreateForEntry("type:vendor:code:v1", CatalogType.HorizontalRole, entryId1).Value;

        var addResult = cluster.AddMember(entryId2);

        Assert.True(addResult.IsSuccess);
        Assert.Equal(2, cluster.MemberEntryIds.Count);
    }

    [Fact]
    public void FullPipeline_RatingAggregate_AccumulatesCorrectly()
    {
        var ownerTenantId = Guid.NewGuid();
        var raterTenantId = Guid.NewGuid();
        var entry = CatalogEntry.CreateDraft(
            ownerTenantId, ActorId, CatalogType.HorizontalRole, "Rated Entry", "Desc",
            CatalogVisibility.Private, ValidPayload, ValidSchema,
            NullCatalogPayloadValidator.Instance).Value;
        entry.Submit(ActorId, NullCatalogPayloadValidator.Instance);
        entry.Approve(ActorId);
        entry.Publish(ActorId);

        var rating = CatalogEntryRating.Create(entry.Id, raterTenantId, ActorId, 5, "Excellent", ownerTenantId).Value;
        entry.IngestRating(rating);

        Assert.Equal(5, rating.Stars);
        Assert.False(string.IsNullOrWhiteSpace(rating.Comment));
        Assert.Equal(1, entry.Ratings.Count);
        Assert.Equal(5m, entry.Ratings.AverageStars);
    }

    [Fact]
    public async Task FullPipeline_TenantStandard_SeverityOverride_AffectsEngineOutput()
    {
        var ts = CreateTenantStandard();
        ts.OverrideRuleSeverity("Smoke-Sev2", AdvisorySeverity.Critical, ActorId, 1);

        var engine = new ConstructionRuleEngine([new SeverityRule("Smoke-Sev2", AdvisorySeverity.Info)]);

        var result = await engine.ApplyAllAsync(CreateSkeleton(), new TestConstructionCtx(new TestTenantProvider()), ts);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "Smoke-Sev2" && a.Severity == AdvisorySeverity.Critical);
    }

    [Fact]
    public void FullPipeline_Snapshot_0_3_AppliedTenantStandard_Preserved()
    {
        var skeleton = CreateSkeleton();
        var snapshot = SkeletonSnapshot.FromSkeleton(skeleton);

        // AppliedTenantStandard defaults to null from FromSkeleton
        Assert.Null(snapshot.AppliedTenantStandard);
    }

    [Fact]
    public void FullPipeline_SnapshotMigrator_0_2_To_0_3_Forward_Only()
    {
        var migrator = new SnapshotMigrator_0_2_to_0_3();

        Assert.True(migrator.CanMigrate("0.2", "0.3"));
        Assert.False(migrator.CanMigrate("0.3", "0.2"));
        Assert.False(migrator.CanMigrate("0.1", "0.3"));
    }
}
