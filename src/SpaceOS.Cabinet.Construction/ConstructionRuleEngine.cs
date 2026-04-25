using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction;

/// <summary>
/// Executes all registered <see cref="IConstructionRule"/>s against a <see cref="Skeleton"/>
/// and aggregates their outputs into an <see cref="EngineResult"/>.
///
/// A11: never propagates exceptions — rule failures are captured as Critical advisories.
/// SEC-CAB-4: enforces per-rule timeout and maximum output cap.
/// </summary>
public sealed class ConstructionRuleEngine
{
    /// <summary>Maximum <see cref="MachiningFeature"/>s a single rule may generate (SEC-CAB-4).</summary>
    public const int MaxMachiningsPerRule = 1000;

    /// <summary>Per-rule wall-clock timeout in seconds (SEC-CAB-4).</summary>
    public const int DefaultPerRuleTimeoutSeconds = 5;

    /// <summary>Engine-level wall-clock timeout in seconds (SEC-CAB-4).</summary>
    public const int DefaultEngineTimeoutSeconds = 30;

    private readonly IReadOnlyList<IConstructionRule> _rules;

    /// <summary>
    /// Initialises the engine with the given rules.
    /// </summary>
    /// <param name="rules">Ordered list of rules to execute.</param>
    public ConstructionRuleEngine(IEnumerable<IConstructionRule> rules)
    {
        _rules = rules.ToList().AsReadOnly();
    }

    /// <summary>
    /// Applies all registered rules to <paramref name="skeleton"/> and returns the aggregated result.
    /// </summary>
    /// <param name="skeleton">The cabinet skeleton to process.</param>
    /// <param name="context">Tenant context and assembly dimensions.</param>
    /// <param name="cancellationToken">External cancellation token (e.g., from HTTP request).</param>
    /// <returns>Always returns Success — exceptions become Critical advisories (A11).</returns>
    public Result<EngineResult> ApplyAll(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken = default)
    {
        var allMachinings = new List<MachiningFeature>();
        var allAdvisories = new List<DesignAdvisory>();
        var disabledRules = new HashSet<string>();

        using var engineCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        engineCts.CancelAfter(TimeSpan.FromSeconds(DefaultEngineTimeoutSeconds));

        foreach (var rule in _rules)
        {
            if (disabledRules.Contains(rule.RuleId))
                continue;

            // Engine-level timeout check before each rule.
            if (engineCts.Token.IsCancellationRequested)
            {
                allAdvisories.Add(new DesignAdvisory(
                    "Engine",
                    AdvisorySeverity.Critical,
                    "Skeleton",
                    "Engine timeout exceeded — remaining rules skipped.",
                    "Reduce skeleton complexity or rule count."));
                break;
            }

            try
            {
                // Per-rule timeout linked to the engine token (SEC-CAB-4).
                using var ruleCts = CancellationTokenSource.CreateLinkedTokenSource(engineCts.Token);
                ruleCts.CancelAfter(TimeSpan.FromSeconds(DefaultPerRuleTimeoutSeconds));

                var result = rule.Apply(skeleton, context, ruleCts.Token);

                // SEC-CAB-8: null guard — a null return is treated as a rule failure.
                if (result is null)
                {
                    allAdvisories.Add(new DesignAdvisory(
                        rule.RuleId,
                        AdvisorySeverity.Critical,
                        "Skeleton",
                        $"Rule '{rule.RuleId}' returned null — disabled.",
                        "Fix or remove this rule."));
                    disabledRules.Add(rule.RuleId);
                    continue;
                }

                // SEC-CAB-4: cap output to prevent runaway rules.
                if (result.GeneratedMachinings.Count > MaxMachiningsPerRule)
                {
                    allAdvisories.Add(new DesignAdvisory(
                        rule.RuleId,
                        AdvisorySeverity.Critical,
                        "Skeleton",
                        $"Rule '{rule.RuleId}' exceeded max machinings per rule limit — output truncated.",
                        "Review rule logic."));
                    allMachinings.AddRange(result.GeneratedMachinings.Take(MaxMachiningsPerRule));
                    disabledRules.Add(rule.RuleId);
                }
                else
                {
                    allMachinings.AddRange(result.GeneratedMachinings);
                }

                allAdvisories.AddRange(result.Advisories);
            }
            catch (OperationCanceledException)
            {
                allAdvisories.Add(new DesignAdvisory(
                    rule.RuleId,
                    AdvisorySeverity.Critical,
                    "Skeleton",
                    $"Rule '{rule.RuleId}' timed out — disabled.",
                    "Optimise rule or increase timeout."));
                disabledRules.Add(rule.RuleId);
            }
            catch (Exception ex)
            {
                // A11: never propagate rule exceptions — wrap as Critical advisory.
                allAdvisories.Add(new DesignAdvisory(
                    rule.RuleId,
                    AdvisorySeverity.Critical,
                    "Skeleton",
                    $"Rule '{rule.RuleId}' threw exception — disabled.",
                    $"Fix rule: {ex.GetType().Name}"));
                disabledRules.Add(rule.RuleId);
            }
        }

        return Result<EngineResult>.Success(new EngineResult(
            allMachinings.AsReadOnly(),
            allAdvisories.AsReadOnly()));
    }
}

/// <summary>
/// The aggregated output of a full <see cref="ConstructionRuleEngine.ApplyAll"/> pass.
/// </summary>
/// <param name="AllGeneratedMachinings">All machining features produced by all rules.</param>
/// <param name="AllAdvisories">All design advisories emitted by all rules (including engine-level ones).</param>
public sealed record EngineResult(
    IReadOnlyList<MachiningFeature> AllGeneratedMachinings,
    IReadOnlyList<DesignAdvisory> AllAdvisories);
