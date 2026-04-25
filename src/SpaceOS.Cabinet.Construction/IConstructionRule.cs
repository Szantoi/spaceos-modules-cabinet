using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction;

/// <summary>
/// A single construction rule that inspects a <see cref="Skeleton"/> and produces
/// machining features and/or design advisories.
/// Rules must be stateless — all state is derived from the skeleton and context arguments.
/// </summary>
public interface IConstructionRule
{
    /// <summary>Stable, unique identifier used to reference this rule in overrides and advisories.</summary>
    string RuleId { get; }

    /// <summary>Human-readable description of what this rule enforces or generates.</summary>
    string Description { get; }

    /// <summary>
    /// Applies this rule to the given <paramref name="skeleton"/>.
    /// SEC-CAB-4: implementations must respect <paramref name="cancellationToken"/> by calling
    /// <see cref="CancellationToken.ThrowIfCancellationRequested"/> in any loops.
    /// A11: rules should never throw for normal design scenarios — emit advisories instead.
    /// </summary>
    /// <param name="skeleton">The skeleton to inspect.</param>
    /// <param name="context">Tenant context and assembly dimensions.</param>
    /// <param name="cancellationToken">Cancellation token for per-rule timeout enforcement.</param>
    /// <returns>The machinings and advisories produced by this rule.</returns>
    ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// The output of a single <see cref="IConstructionRule.Apply"/> call.
/// </summary>
/// <param name="GeneratedMachinings">Machining features produced by the rule.</param>
/// <param name="Advisories">Design advisories emitted by the rule.</param>
public sealed record ConstructionRuleResult(
    IReadOnlyList<MachiningFeature> GeneratedMachinings,
    IReadOnlyList<DesignAdvisory> Advisories)
{
    /// <summary>A result with no machinings and no advisories.</summary>
    public static ConstructionRuleResult Empty => new(
        Array.Empty<MachiningFeature>(),
        Array.Empty<DesignAdvisory>());
}
