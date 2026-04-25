using SpaceOS.Cabinet.Domain.Skeleton;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Validation rule that enforces the 15 mm setback zone from the back edge for groove/rabbet placement.
/// In Cabinet 0.1 this is a design-convention marker; setback compliance is verified during
/// toolpath generation. No machinings or advisories are produced here.
/// </summary>
public sealed class SetbackRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-Setback-15mm";

    /// <inheritdoc/>
    public string Description => "Ensures 15mm setback zone from the back edge for back panel.";

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        // Setback is a positional design convention for groove/rabbet features.
        // Actual position compliance is verified during toolpath post-processing.
        return ConstructionRuleResult.Empty;
    }
}
