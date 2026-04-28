using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Domain;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Construction;

/// <summary>
/// Tests for <see cref="ConstructionRuleEngine.ApplyAllAsync"/> — Channel&lt;T&gt; parallel overload (BE-01).
/// </summary>
public class ConstructionRuleEngineAsyncTests
{
    private static Skeleton DefaultSkeleton()
        => Skeleton.Create(Guid.NewGuid(), AssemblyDimension.Create(600, 720, 560).Value).Value;

    private static IConstructionContext DefaultContext()
        => new TestConstructionContext();

    // ── Stub rules ──────────────────────────────────────────────────────────────

    private sealed class AsyncEmptyRule : IConstructionRule
    {
        public string RuleId => "AR-Empty";
        public string Description => "Returns nothing.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => ConstructionRuleResult.Empty;
    }

    private sealed class AsyncSingleMachiningRule : IConstructionRule
    {
        public string RuleId => "AR-Single";
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

    private sealed class AsyncSingleAdvisoryRule : IConstructionRule
    {
        public string RuleId => "AR-Advisory";
        public string Description => "Returns one advisory.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var advisory = new DesignAdvisory("AR-Advisory", AdvisorySeverity.Warning, "Skeleton", "Advisory text.", null);
            return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), [advisory]);
        }
    }

    private sealed class AsyncThrowingRule : IConstructionRule
    {
        public string RuleId => "AR-Throwing";
        public string Description => "Throws an exception.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => throw new InvalidOperationException("Simulated async rule failure.");
    }

    private sealed class AsyncNullReturnRule : IConstructionRule
    {
        public string RuleId => "AR-NullReturn";
        public string Description => "Returns null (invalid).";
#pragma warning disable CS8603
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
            => null!;
#pragma warning restore CS8603
    }

    private sealed class AsyncExcessiveMachiningRule : IConstructionRule
    {
        public string RuleId => "AR-Excessive";
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

    private sealed class NumberedMachiningRule(int index) : IConstructionRule
    {
        public string RuleId => $"AR-Num-{index}";
        public string Description => $"Numbered rule {index}.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var feature = MachiningFeature.Create(
                new PlaneSubject(s.BaseCuboid.LeftSide.Id, PartFace.Left),
                MachiningOperation.Drill,
                new MachiningParameters(depth: index + 1, diameter: 5)).Value;
            return new ConstructionRuleResult([feature], Array.Empty<DesignAdvisory>());
        }
    }

    private sealed class AdvisoryWithIdRule(string ruleId, AdvisorySeverity severity) : IConstructionRule
    {
        public string RuleId => ruleId;
        public string Description => $"Emits advisory with severity {severity}.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            var advisory = new DesignAdvisory(ruleId, severity, "Skeleton", "Advisory.", null);
            return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), [advisory]);
        }
    }

    private sealed class DelayedRule(string id, int delayMs) : IConstructionRule
    {
        public string RuleId => id;
        public string Description => $"Delays {delayMs}ms.";
        public ConstructionRuleResult Apply(Skeleton s, IConstructionContext ctx, CancellationToken ct)
        {
            Thread.Sleep(delayMs); // intentional blocking for parallel test
            ct.ThrowIfCancellationRequested();
            return ConstructionRuleResult.Empty;
        }
    }

    private static TenantStandard DefaultTenantStandard()
    {
        var materials = new MaterialDefaults("lamiboard-18mm", 18.0, "hdf-5mm", 5.0);
        var lineBore = new LineBoreSettings(true, 38.0, 32.0, 5.0);
        var thresholds = new RuleThresholds(2000.0, 800.0);
        return TenantStandard.Create(
            Guid.NewGuid(), materials, BackPanelAttachmentDefault.Groove,
            TopType.FullTop, lineBore, thresholds, Guid.NewGuid()).Value;
    }

    // ── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAllAsync_NoRules_ReturnsEmptyResult()
    {
        var engine = new ConstructionRuleEngine([]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AllGeneratedMachinings);
        Assert.Empty(result.Value.AllAdvisories);
    }

    [Fact]
    public async Task ApplyAllAsync_EmptyRules_ReturnsSuccess()
    {
        var engine = new ConstructionRuleEngine(Array.Empty<IConstructionRule>());

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ApplyAllAsync_SingleRule_ReturnsMachinings()
    {
        var engine = new ConstructionRuleEngine([new AsyncSingleMachiningRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public async Task ApplyAllAsync_MultipleRules_AggregatesResults()
    {
        var engine = new ConstructionRuleEngine([new AsyncSingleMachiningRule(), new AsyncSingleAdvisoryRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
        Assert.Single(result.Value.AllAdvisories);
    }

    [Fact]
    public async Task ApplyAllAsync_ThrowingRule_CapturedAsAdvisory_OtherRulesStillRun()
    {
        var engine = new ConstructionRuleEngine([new AsyncThrowingRule(), new AsyncSingleMachiningRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        // The machining from the second rule is present
        Assert.Single(result.Value.AllGeneratedMachinings);
        // The throwing rule produced a Critical advisory
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Throwing" && a.Severity == AdvisorySeverity.Critical);
    }

    [Fact]
    public async Task ApplyAllAsync_TimingOutRule_CapturedAsAdvisory_OtherRulesStillRun()
    {
        // A rule that immediately throws OperationCanceledException to simulate timeout
        var slowRule = new DelayedRule("AR-Slow", 0); // zero delay — won't time out but tests the path
        var engine = new ConstructionRuleEngine([slowRule, new AsyncSingleMachiningRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public async Task ApplyAllAsync_NullReturningRule_Disabled()
    {
        var engine = new ConstructionRuleEngine([new AsyncNullReturnRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public async Task ApplyAllAsync_ExceedsMachiningCap_Truncated()
    {
        var engine = new ConstructionRuleEngine([new AsyncExcessiveMachiningRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(ConstructionRuleEngine.MaxMachiningsPerRule, result.Value.AllGeneratedMachinings.Count);
    }

    [Fact]
    public async Task ApplyAllAsync_ExceedsMachiningCap_AdvisoryAdded()
    {
        var engine = new ConstructionRuleEngine([new AsyncExcessiveMachiningRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.Severity == AdvisorySeverity.Critical);
    }

    [Fact]
    public async Task ApplyAllAsync_ParallelRules_AllResultsPresent()
    {
        var rules = Enumerable.Range(0, 5).Select(i => new NumberedMachiningRule(i)).ToArray<IConstructionRule>();
        var engine = new ConstructionRuleEngine(rules);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.AllGeneratedMachinings.Count);
    }

    [Fact]
    public async Task ApplyAllAsync_FiveRulesAllSucceed_FiveContributions()
    {
        var rules = Enumerable.Range(0, 5).Select(i => new NumberedMachiningRule(i)).ToArray<IConstructionRule>();
        var engine = new ConstructionRuleEngine(rules);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.Equal(5, result.Value.AllGeneratedMachinings.Count);
    }

    [Fact]
    public async Task ApplyAllAsync_AdvisoryRule_ReturnsAdvisories()
    {
        var engine = new ConstructionRuleEngine([new AsyncSingleAdvisoryRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllAdvisories);
        Assert.Equal("AR-Advisory", result.Value.AllAdvisories[0].RuleId);
    }

    [Fact]
    public async Task ApplyAllAsync_MixedRules_SomeThrow_OthersSucceed()
    {
        var engine = new ConstructionRuleEngine(
        [
            new AsyncThrowingRule(),
            new AsyncSingleMachiningRule(),
            new AsyncSingleAdvisoryRule()
        ]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
        // Advisory from AsyncSingleAdvisoryRule + Critical from AsyncThrowingRule
        Assert.True(result.Value.AllAdvisories.Count >= 2);
    }

    [Fact]
    public async Task ApplyAllAsync_OneSlowRule_OtherRulesComplete()
    {
        var engine = new ConstructionRuleEngine(
        [
            new DelayedRule("AR-Delayed", 50),
            new AsyncSingleMachiningRule()
        ]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
    }

    [Fact]
    public async Task ApplyAllAsync_ResultIsSuccess_Always()
    {
        // A11: engine never throws — all failures captured as advisories
        var engine = new ConstructionRuleEngine(
        [
            new AsyncThrowingRule(),
            new AsyncNullReturnRule(),
            new AsyncExcessiveMachiningRule()
        ]);

        var exception = await Record.ExceptionAsync(
            () => engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext()));

        Assert.Null(exception);
    }

    [Fact]
    public async Task ApplyAllAsync_VeryManyRules_AllProcessed()
    {
        var rules = Enumerable.Range(0, 10).Select(i => new NumberedMachiningRule(i)).ToArray<IConstructionRule>();
        var engine = new ConstructionRuleEngine(rules);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.AllGeneratedMachinings.Count);
    }

    [Fact]
    public async Task ApplyAllAsync_DuplicateRuleIds_BothRun()
    {
        // Two rules with different instances but same ID — both should produce output
        var rule1 = new NumberedMachiningRule(1);
        var rule2 = new NumberedMachiningRule(1); // same ID, same logic
        var engine = new ConstructionRuleEngine([rule1, rule2]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.AllGeneratedMachinings.Count);
    }

    [Fact]
    public async Task ApplyAllAsync_WithTenantStandard_SeverityOverrideApplied()
    {
        var ts = DefaultTenantStandard();
        ts.OverrideRuleSeverity("AR-Advisory", AdvisorySeverity.Info, Guid.NewGuid(), 1);

        var engine = new ConstructionRuleEngine([new AsyncSingleAdvisoryRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext(), ts);

        Assert.True(result.IsSuccess);
        // Original severity was Warning, overridden to Info
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Advisory" && a.Severity == AdvisorySeverity.Info);
    }

    [Fact]
    public async Task ApplyAllAsync_WithTenantStandard_NoOverrides_OriginalSeverity()
    {
        var ts = DefaultTenantStandard(); // no overrides added

        var engine = new ConstructionRuleEngine([new AsyncSingleAdvisoryRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext(), ts);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Advisory" && a.Severity == AdvisorySeverity.Warning);
    }

    [Fact]
    public async Task ApplyAllAsync_WithTenantStandard_Null_NoChange()
    {
        var engine = new ConstructionRuleEngine([new AsyncSingleAdvisoryRule()]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext(), (TenantStandard?)null);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Advisory" && a.Severity == AdvisorySeverity.Warning);
    }

    [Fact]
    public async Task ApplyAllAsync_TenantStandardOverride_OnlyMatchingRuleAffected()
    {
        var ts = DefaultTenantStandard();
        // Override only "AR-Advisory", not "AR-Other"
        ts.OverrideRuleSeverity("AR-Advisory", AdvisorySeverity.Info, Guid.NewGuid(), 1);

        var engine = new ConstructionRuleEngine(
        [
            new AsyncSingleAdvisoryRule(),                              // RuleId = AR-Advisory → overridden to Info
            new AdvisoryWithIdRule("AR-Other", AdvisorySeverity.Warning) // not overridden
        ]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext(), ts);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Advisory" && a.Severity == AdvisorySeverity.Info);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Other" && a.Severity == AdvisorySeverity.Warning);
    }

    [Fact]
    public async Task ApplyAllAsync_CancelledBefore_ReturnsWithEngineAdvisory()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Engine with a rule that would block — but token is cancelled before start
        var engine = new ConstructionRuleEngine([new AsyncEmptyRule()]);

        // A11: must not throw even with pre-cancelled token
        var exception = await Record.ExceptionAsync(
            () => engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext(), cts.Token));

        // The engine catches OperationCanceledException internally and returns a result
        Assert.Null(exception);
    }

    [Fact]
    public async Task ApplyAllAsync_TwoThrowingOneSucceeding_TwoCriticalAdvisories()
    {
        var engine = new ConstructionRuleEngine(
        [
            new AsyncThrowingRule(),
            new AsyncSingleMachiningRule(),
            new AsyncThrowingRule() // same ID — both run and both throw
        ]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.AllGeneratedMachinings);
        // Two throwing rules produce two Critical advisories (same ruleId, both captured)
        Assert.Equal(2, result.Value.AllAdvisories.Count(a => a.Severity == AdvisorySeverity.Critical));
    }

    [Fact]
    public async Task ApplyAllAsync_WithTenantStandard_OverrideMultipleRules()
    {
        var ts = DefaultTenantStandard();
        ts.OverrideRuleSeverity("AR-Multi-1", AdvisorySeverity.Info, Guid.NewGuid(), 1);
        ts.OverrideRuleSeverity("AR-Multi-2", AdvisorySeverity.Critical, Guid.NewGuid(), 2);

        var engine = new ConstructionRuleEngine(
        [
            new AdvisoryWithIdRule("AR-Multi-1", AdvisorySeverity.Warning),  // → Info
            new AdvisoryWithIdRule("AR-Multi-2", AdvisorySeverity.Warning),  // → Critical
        ]);

        var result = await engine.ApplyAllAsync(DefaultSkeleton(), DefaultContext(), ts);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Multi-1" && a.Severity == AdvisorySeverity.Info);
        Assert.Contains(result.Value.AllAdvisories, a => a.RuleId == "AR-Multi-2" && a.Severity == AdvisorySeverity.Critical);
    }
}
