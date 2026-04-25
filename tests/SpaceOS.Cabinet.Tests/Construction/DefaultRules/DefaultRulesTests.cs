using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Construction.DefaultRules;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Construction.DefaultRules;

public class DefaultRulesTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Skeleton DefaultSkeleton()
        => Skeleton.Create(Guid.NewGuid(), AssemblyDimension.Create(600, 720, 560).Value).Value;

    private static Skeleton TallSkeleton()
        => Skeleton.Create(Guid.NewGuid(), AssemblyDimension.Create(600, 2400, 560).Value).Value;

    private static IConstructionContext DefaultContext()
        => new TestConstructionContext();

    private static IConstructionContext ContextWith(ITenantStandardProvider provider)
        => new TestConstructionContext(provider);

    private static IConstructionContext ContextWithDimension(AssemblyDimension dimension)
        => new TestConstructionContext(assemblyDimension: dimension);

    // Adds a shelf-sized part to a skeleton to act as a stiffener or long shelf.
    private static Part AddShelfPart(Skeleton skeleton, double length)
    {
        var dim = PartDimension.Create(length, 400, 18).Value;
        var frame = PartFrame.Create(AffineTransform.Identity, dim).Value;
        return skeleton.AddPart(frame, "shelf-material").Value;
    }

    // ── LineBoreRule ─────────────────────────────────────────────────────────

    [Fact]
    public void LineBoreRule_Enabled_GeneratesDrills()
    {
        var rule = new LineBoreRule();
        var skeleton = DefaultSkeleton();
        var context = DefaultContext();

        var result = rule.Apply(skeleton, context, CancellationToken.None);

        Assert.NotEmpty(result.GeneratedMachinings);
        Assert.All(result.GeneratedMachinings,
            f => Assert.Equal(SpaceOS.Cabinet.Machining.MachiningOperation.Drill, f.Operation));
    }

    [Fact]
    public void LineBoreRule_Disabled_ReturnsEmpty()
    {
        var rule = new LineBoreRule();
        var skeleton = DefaultSkeleton();
        var context = ContextWith(new TestTenantStandardProvider { LineBoreEnabled = false });

        var result = rule.Apply(skeleton, context, CancellationToken.None);

        Assert.Empty(result.GeneratedMachinings);
    }

    // ── DefaultJointRule ─────────────────────────────────────────────────────

    [Fact]
    public void DefaultJointRule_ReturnsEmpty()
    {
        var rule = new DefaultJointRule();

        var result = rule.Apply(DefaultSkeleton(), DefaultContext(), CancellationToken.None);

        Assert.Same(ConstructionRuleResult.Empty.GeneratedMachinings, result.GeneratedMachinings);
        Assert.Same(ConstructionRuleResult.Empty.Advisories, result.Advisories);
    }

    // ── BackPanelGrooveRule ───────────────────────────────────────────────────

    [Fact]
    public void BackPanelGrooveRule_GrooveAttachment_GeneratesGrooves()
    {
        var rule = new BackPanelGrooveRule();
        var context = ContextWith(new TestTenantStandardProvider
        {
            BackPanelAttachment = BackPanelAttachmentDefault.Groove
        });

        var result = rule.Apply(DefaultSkeleton(), context, CancellationToken.None);

        Assert.Equal(4, result.GeneratedMachinings.Count);
        Assert.All(result.GeneratedMachinings,
            f => Assert.Equal(SpaceOS.Cabinet.Machining.MachiningOperation.Groove, f.Operation));
    }

    [Fact]
    public void BackPanelGrooveRule_NonGrooveAttachment_ReturnsEmpty()
    {
        var rule = new BackPanelGrooveRule();
        var context = ContextWith(new TestTenantStandardProvider
        {
            BackPanelAttachment = BackPanelAttachmentDefault.Rabbet
        });

        var result = rule.Apply(DefaultSkeleton(), context, CancellationToken.None);

        Assert.Empty(result.GeneratedMachinings);
    }

    // ── BackPanelRabbetRule ───────────────────────────────────────────────────

    [Fact]
    public void BackPanelRabbetRule_RabbetAttachment_GeneratesRabbets()
    {
        var rule = new BackPanelRabbetRule();
        var context = ContextWith(new TestTenantStandardProvider
        {
            BackPanelAttachment = BackPanelAttachmentDefault.Rabbet
        });

        var result = rule.Apply(DefaultSkeleton(), context, CancellationToken.None);

        Assert.Equal(4, result.GeneratedMachinings.Count);
        Assert.All(result.GeneratedMachinings,
            f => Assert.Equal(SpaceOS.Cabinet.Machining.MachiningOperation.Rabbet, f.Operation));
    }

    [Fact]
    public void BackPanelRabbetRule_NonRabbetAttachment_ReturnsEmpty()
    {
        var rule = new BackPanelRabbetRule();
        var context = ContextWith(new TestTenantStandardProvider
        {
            BackPanelAttachment = BackPanelAttachmentDefault.Groove
        });

        var result = rule.Apply(DefaultSkeleton(), context, CancellationToken.None);

        Assert.Empty(result.GeneratedMachinings);
    }

    // ── EdgeBandFrontRule ─────────────────────────────────────────────────────

    [Fact]
    public void EdgeBandFrontRule_GeneratesEdgeBands()
    {
        var rule = new EdgeBandFrontRule();
        var skeleton = DefaultSkeleton();

        var result = rule.Apply(skeleton, DefaultContext(), CancellationToken.None);

        // There are 4 base parts → 4 edge bands.
        Assert.Equal(skeleton.Parts.Count, result.GeneratedMachinings.Count);
        Assert.All(result.GeneratedMachinings,
            f => Assert.Equal(SpaceOS.Cabinet.Machining.MachiningOperation.EdgeBand, f.Operation));
    }

    // ── EdgeBandHiddenRule ────────────────────────────────────────────────────

    [Fact]
    public void EdgeBandHiddenRule_ReturnsEmpty()
    {
        var rule = new EdgeBandHiddenRule();

        var result = rule.Apply(DefaultSkeleton(), DefaultContext(), CancellationToken.None);

        Assert.Empty(result.GeneratedMachinings);
        Assert.Empty(result.Advisories);
    }

    // ── SetbackRule ───────────────────────────────────────────────────────────

    [Fact]
    public void SetbackRule_ReturnsEmpty()
    {
        var rule = new SetbackRule();

        var result = rule.Apply(DefaultSkeleton(), DefaultContext(), CancellationToken.None);

        Assert.Empty(result.GeneratedMachinings);
        Assert.Empty(result.Advisories);
    }

    // ── MaterialDefaultRule ───────────────────────────────────────────────────

    [Fact]
    public void MaterialDefaultRule_DefaultMaterial_GeneratesAdvisory()
    {
        var rule = new MaterialDefaultRule();
        var skeleton = DefaultSkeleton();

        // Default skeleton has parts with "default-carcass" material.
        var result = rule.Apply(skeleton, DefaultContext(), CancellationToken.None);

        Assert.NotEmpty(result.Advisories);
        Assert.All(result.Advisories, a => Assert.Equal(AdvisorySeverity.Info, a.Severity));
    }

    // ── StiffenerTallRule ─────────────────────────────────────────────────────

    [Fact]
    public void StiffenerTallRule_TallCabinet_GeneratesWarning()
    {
        var rule = new StiffenerTallRule();
        // Height 2400 mm > threshold 2000 mm, no stiffener added.
        var skeleton = TallSkeleton();
        var context = ContextWithDimension(AssemblyDimension.Create(600, 2400, 560).Value);

        var result = rule.Apply(skeleton, context, CancellationToken.None);

        Assert.Single(result.Advisories);
        Assert.Equal(AdvisorySeverity.Warning, result.Advisories[0].Severity);
    }

    [Fact]
    public void StiffenerTallRule_ShortCabinet_ReturnsEmpty()
    {
        var rule = new StiffenerTallRule();
        var skeleton = DefaultSkeleton(); // height 720 mm < threshold 2000 mm
        var context = ContextWithDimension(AssemblyDimension.Create(600, 720, 560).Value);

        var result = rule.Apply(skeleton, context, CancellationToken.None);

        Assert.Empty(result.Advisories);
    }

    // ── ShelfSagRule ──────────────────────────────────────────────────────────

    [Fact]
    public void ShelfSagRule_LongShelf_GeneratesInfo()
    {
        var rule = new ShelfSagRule();
        var skeleton = DefaultSkeleton();
        // Add a part with length > 800 mm threshold.
        AddShelfPart(skeleton, length: 900);

        var result = rule.Apply(skeleton, DefaultContext(), CancellationToken.None);

        Assert.Single(result.Advisories);
        Assert.Equal(AdvisorySeverity.Info, result.Advisories[0].Severity);
    }

    [Fact]
    public void ShelfSagRule_ShortShelf_ReturnsEmpty()
    {
        var rule = new ShelfSagRule();
        var skeleton = DefaultSkeleton();
        // Add a part with length < 800 mm threshold.
        AddShelfPart(skeleton, length: 600);

        var result = rule.Apply(skeleton, DefaultContext(), CancellationToken.None);

        Assert.Empty(result.Advisories);
    }
}
