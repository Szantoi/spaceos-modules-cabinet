#pragma warning disable CS0618 // ApplyAll is obsolete — tests preserved for backward-compat verification
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Construction;

public class ConstructionRuleEngineTests
{
    private static Skeleton DefaultSkeleton()
        => Skeleton.Create(Guid.NewGuid(), AssemblyDimension.Create(600, 720, 560).Value).Value;

    private static IConstructionContext DefaultContext()
        => new TestConstructionContext();

    // ── Stub rules ───────────────────────────────────────────────────────────

    private sealed class EmptyRule : IConstructionRule
    {
        public string RuleId => "R-Empty";
        public string Description => "Returns nothing.";

        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => ConstructionRuleResult.Empty;
    }

    private sealed class SingleMachiningRule : IConstructionRule
    {
        public string RuleId => "R-Single";
        public string Description => "Returns one machining feature.";

        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var feature = MachiningFeature.Create(
                new PlaneSubject(s.BaseCuboid.LeftSide.Id, PartFace.Left),
                MachiningOperation.Drill,
                new MachiningParameters(depth: 10, diameter: 5)).Value;

            return new ConstructionRuleResult([feature], Array.Empty<DesignAdvisory>());
        }
    }

    private sealed class SingleAdvisoryRule : IConstructionRule
    {
        public string RuleId => "R-Advisory";
        public string Description => "Returns one advisory.";

        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var advisory = new DesignAdvisory("R-Advisory", AdvisorySeverity.Info, "Skeleton", "Test advisory.", null);
            return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), [advisory]);
        }
    }

    private sealed class ThrowingRule : IConstructionRule
    {
        public string RuleId => "R-Throwing";
        public string Description => "Throws an exception.";

        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => throw new InvalidOperationException("Simulated rule failure.");
    }

    private sealed class NullReturnRule : IConstructionRule
    {
        public string RuleId => "R-NullReturn";
        public string Description => "Returns null (invalid).";

#pragma warning disable CS8603 // Intentional null return for test
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => null!;
#pragma warning restore CS8603
    }

    private sealed class ExcessiveMachiningRule : IConstructionRule
    {
        public string RuleId => "R-Excessive";
        public string Description => "Generates more features than the cap.";

        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var features = Enumerable
                .Range(0, ConstructionRuleEngine.MaxMachiningsPerRule + 1)
                .Select(_ => MachiningFeature.Create(
                    new PlaneSubject(s.BaseCuboid.LeftSide.Id, PartFace.Left),
                    MachiningOperation.Drill,
                    new MachiningParameters(depth: 10, diameter: 5)).Value)
                .ToList();

            return new ConstructionRuleResult(features.AsReadOnly(), Array.Empty<DesignAdvisory>());
        }
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyAll_NoRules_ReturnsEmptyResult()
    {
        var engine = new ConstructionRuleEngine([]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AllGeneratedMachinings);
        Assert.Empty(result.Value.AllAdvisories);
    }

    [Fact]
    public void ApplyAll_SingleRule_ReturnsRuleResult()
    {
        var engine = new ConstructionRuleEngine([new SingleMachiningRule()]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public void ApplyAll_MultipleRules_AggregatesResults()
    {
        var engine = new ConstructionRuleEngine(
        [
            new SingleMachiningRule(),
            new SingleAdvisoryRule()
        ]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
        Assert.Single(result.Value.AllAdvisories);
    }

    [Fact]
    public void ApplyAll_RuleThrowsException_CapturedAsAdvisory()
    {
        var engine = new ConstructionRuleEngine([new ThrowingRule()]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AllGeneratedMachinings);
        Assert.Single(result.Value.AllAdvisories);
        Assert.Equal(AdvisorySeverity.Critical, result.Value.AllAdvisories[0].Severity);
    }

    [Fact]
    public void ApplyAll_RuleReturnsNull_CapturedAsAdvisory()
    {
        var engine = new ConstructionRuleEngine([new NullReturnRule()]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllAdvisories);
        Assert.Equal(AdvisorySeverity.Critical, result.Value.AllAdvisories[0].Severity);
    }

    [Fact]
    public void ApplyAll_RuleExceedsMaxMachinings_Truncated()
    {
        var engine = new ConstructionRuleEngine([new ExcessiveMachiningRule()]);

        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(ConstructionRuleEngine.MaxMachiningsPerRule, result.Value.AllGeneratedMachinings.Count);
        Assert.Single(result.Value.AllAdvisories);
        Assert.Equal(AdvisorySeverity.Critical, result.Value.AllAdvisories[0].Severity);
    }

    [Fact]
    public void ApplyAll_CancellationRequested_StopsGracefully()
    {
        // A rule that loops and respects cancellation.
        var engine = new ConstructionRuleEngine([new EmptyRule()]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Even with an already-cancelled token the engine should not throw.
        var result = engine.ApplyAll(DefaultSkeleton(), DefaultContext(), cts.Token);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ApplyAll_NeverThrows_AlwaysReturnsResult()
    {
        var engine = new ConstructionRuleEngine(
        [
            new ThrowingRule(),
            new NullReturnRule(),
            new SingleMachiningRule()
        ]);

        // Must not throw under any circumstance (A11).
        var exception = Record.Exception(
            () => engine.ApplyAll(DefaultSkeleton(), DefaultContext()));

        Assert.Null(exception);
    }
}
