using System.Reflection;
using SpaceOS.Cabinet.Abstractions;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Abstractions;

public class AbstractionsTests
{
    // ── Interface visibility ─────────────────────────────────────────────────

    [Fact]
    public void ITenantStandardProvider_IsPublicInterface()
    {
        var type = typeof(ITenantStandardProvider);

        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);
    }

    [Fact]
    public void ISnapshotMigrator_IsPublicInterface()
    {
        var type = typeof(ISnapshotMigrator);

        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);
    }

    [Fact]
    public void IGeometryProjector_IsPublicInterface()
    {
        var type = typeof(IGeometryProjector);

        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);
    }

    [Fact]
    public void IPartCatalog_IsPublicInterface()
    {
        var type = typeof(IPartCatalog);

        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);
    }

    // ── ITenantStandardProvider members ─────────────────────────────────────

    [Theory]
    [InlineData(nameof(ITenantStandardProvider.TenantId))]
    [InlineData(nameof(ITenantStandardProvider.DefaultCarcassMaterial))]
    [InlineData(nameof(ITenantStandardProvider.DefaultCarcassThickness))]
    [InlineData(nameof(ITenantStandardProvider.DefaultBackPanelMaterial))]
    [InlineData(nameof(ITenantStandardProvider.DefaultBackPanelThickness))]
    [InlineData(nameof(ITenantStandardProvider.BackPanelAttachment))]
    [InlineData(nameof(ITenantStandardProvider.TopType))]
    [InlineData(nameof(ITenantStandardProvider.LineBoreEnabled))]
    [InlineData(nameof(ITenantStandardProvider.LineBoreFirstHoleOffset))]
    [InlineData(nameof(ITenantStandardProvider.LineBoreSpacing))]
    [InlineData(nameof(ITenantStandardProvider.LineBoreDiameter))]
    [InlineData(nameof(ITenantStandardProvider.TallCabinetHeightThreshold))]
    [InlineData(nameof(ITenantStandardProvider.LongShelfThreshold))]
    [InlineData(nameof(ITenantStandardProvider.RuleSeverityOverrides))]
    public void ITenantStandardProvider_HasExpectedMember(string memberName)
    {
        var type = typeof(ITenantStandardProvider);
        var member = type.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(member);
    }

    // ── ISnapshotMigrator members ────────────────────────────────────────────

    [Theory]
    [InlineData(nameof(ISnapshotMigrator.CanMigrate))]
    [InlineData(nameof(ISnapshotMigrator.Migrate))]
    public void ISnapshotMigrator_HasExpectedMember(string memberName)
    {
        var type = typeof(ISnapshotMigrator);
        var member = type.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(member);
    }

    // ── Enum values ──────────────────────────────────────────────────────────

    [Fact]
    public void BackPanelAttachmentDefault_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(BackPanelAttachmentDefault), BackPanelAttachmentDefault.Stumpf));
        Assert.True(Enum.IsDefined(typeof(BackPanelAttachmentDefault), BackPanelAttachmentDefault.Rabbet));
        Assert.True(Enum.IsDefined(typeof(BackPanelAttachmentDefault), BackPanelAttachmentDefault.Groove));
    }

    [Fact]
    public void TopType_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(TopType), TopType.FullTop));
        Assert.True(Enum.IsDefined(typeof(TopType), TopType.CrossRailPair));
    }

    [Fact]
    public void AdvisorySeverity_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(AdvisorySeverity), AdvisorySeverity.Info));
        Assert.True(Enum.IsDefined(typeof(AdvisorySeverity), AdvisorySeverity.Warning));
        Assert.True(Enum.IsDefined(typeof(AdvisorySeverity), AdvisorySeverity.Error));
        Assert.True(Enum.IsDefined(typeof(AdvisorySeverity), AdvisorySeverity.Critical));
    }

    [Fact]
    public void PartFace_HasSixValues()
    {
        var values = Enum.GetValues<PartFace>();
        Assert.Equal(6, values.Length);
    }

    [Fact]
    public void PartEdge_HasTwelveValues()
    {
        var values = Enum.GetValues<PartEdge>();
        Assert.Equal(12, values.Length);
    }

    [Fact]
    public void PartRole_HasUnknownValue()
    {
        Assert.True(Enum.IsDefined(typeof(PartRole), PartRole.Unknown));
    }

    // ── ISnapshotMigrator contract via test implementation ───────────────────

    [Fact]
    public void ISnapshotMigrator_CanMigrate_ReturnsFalseForUnsupportedVersions()
    {
        ISnapshotMigrator migrator = new StubSnapshotMigrator();

        Assert.False(migrator.CanMigrate("0.1", "0.3"));
    }

    [Fact]
    public void ISnapshotMigrator_CanMigrate_ReturnsTrueForSupportedVersions()
    {
        ISnapshotMigrator migrator = new StubSnapshotMigrator();

        Assert.True(migrator.CanMigrate("0.1", "0.2"));
    }

    [Fact]
    public void ISnapshotMigrator_Migrate_ReturnsSuccessForSupportedMigration()
    {
        ISnapshotMigrator migrator = new StubSnapshotMigrator();

        var result = migrator.Migrate("{}", "0.1", "0.2");

        Assert.True(result.IsSuccess);
    }

    // ── Test double ──────────────────────────────────────────────────────────

    private sealed class StubSnapshotMigrator : ISnapshotMigrator
    {
        public bool CanMigrate(string fromVersion, string toVersion)
            => fromVersion == "0.1" && toVersion == "0.2";

        public Ardalis.Result.Result<string> Migrate(string snapshotJson, string fromVersion, string toVersion)
            => CanMigrate(fromVersion, toVersion)
                ? Ardalis.Result.Result<string>.Success(snapshotJson)
                : Ardalis.Result.Result<string>.Error("Unsupported migration path.");
    }
}
