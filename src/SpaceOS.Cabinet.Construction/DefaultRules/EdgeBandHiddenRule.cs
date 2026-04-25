using SpaceOS.Cabinet.Domain.Skeleton;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Validation rule that ensures hidden and internal edges are not edge-banded.
/// This is a convention-enforcement rule — it emits no machinings and produces no advisories
/// in the base implementation (edge detection is handled by toolpath post-processing).
/// </summary>
public sealed class EdgeBandHiddenRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-EdgeBand-Hidden";

    /// <inheritdoc/>
    public string Description => "Ensures hidden and internal edges are not edge-banded.";

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        // Convention-only rule: back and internal edges must not receive edge banding.
        // Enforcement is done at toolpath post-processing level; this rule acts as a marker.
        return ConstructionRuleResult.Empty;
    }
}
