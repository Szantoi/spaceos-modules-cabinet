#pragma warning disable CS0618 // ApplyAll is obsolete — tests preserved for backward-compat verification
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Construction.DefaultRules;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Construction;

public class EngineResultTests
{
    private static Skeleton DefaultSkeleton()
        => Skeleton.Create(Guid.NewGuid(), AssemblyDimension.Create(600, 720, 560).Value).Value;

    private static IConstructionContext DefaultContext()
        => new TestConstructionContext();

    [Fact]
    public void EngineResult_AllGeneratedMachinings_IsReadOnly()
    {
        var engine = new ConstructionRuleEngine([]);
        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.IsAssignableFrom<IReadOnlyList<MachiningFeature>>(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public void EngineResult_AllAdvisories_IsReadOnly()
    {
        var engine = new ConstructionRuleEngine([]);
        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.IsAssignableFrom<IReadOnlyList<DesignAdvisory>>(result.Value.AllAdvisories);
    }

    [Fact]
    public void ConstructionRuleResult_Empty_HasNoMachinings()
    {
        Assert.Empty(ConstructionRuleResult.Empty.GeneratedMachinings);
    }

    [Fact]
    public void ConstructionRuleResult_Empty_HasNoAdvisories()
    {
        Assert.Empty(ConstructionRuleResult.Empty.Advisories);
    }

    [Fact]
    public void ApplyAll_WithDefaultRuleSet_ProducesResult()
    {
        // Smoke test: wires up all 10 default rules together.
        var engine = new ConstructionRuleEngine(
        [
            new LineBoreRule(),
            new DefaultJointRule(),
            new BackPanelGrooveRule(),
            new BackPanelRabbetRule(),
            new EdgeBandFrontRule(),
            new EdgeBandHiddenRule(),
            new SetbackRule(),
            new MaterialDefaultRule(),
            new StiffenerTallRule(),
            new ShelfSagRule()
        ]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ApplyAll_WithGrooveAttachment_EngineAggregatesGrooveAndLineBore()
    {
        var engine = new ConstructionRuleEngine(
        [
            new LineBoreRule(),
            new BackPanelGrooveRule()
        ]);
        var context = new TestConstructionContext(
            new TestTenantStandardProvider
            {
                BackPanelAttachment = BackPanelAttachmentDefault.Groove,
                LineBoreEnabled = true
            });

        var result = engine.ApplyAll(DefaultSkeleton(), context);

        Assert.True(result.IsSuccess);
        // Groove rule produces 4, LineBore produces holes on 2 side panels.
        Assert.True(result.Value.AllGeneratedMachinings.Count >= 4);
    }

    [Fact]
    public void ApplyAll_MaterialDefaultRule_AdvisoryCountMatchesPartCount()
    {
        var engine = new ConstructionRuleEngine([new MaterialDefaultRule()]);
        var skeleton = DefaultSkeleton();

        var result = engine.ApplyAll(skeleton, DefaultContext());

        // Every base cuboid part has "default-carcass" material → one advisory per part.
        Assert.Equal(skeleton.Parts.Count, result.Value.AllAdvisories.Count);
    }
}
