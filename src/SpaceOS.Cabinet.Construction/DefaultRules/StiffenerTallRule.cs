using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Emits a Warning advisory when the cabinet height exceeds the tenant's tall-cabinet threshold
/// and no horizontal stiffener (cross-rail) has been added to the skeleton.
/// </summary>
public sealed class StiffenerTallRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-Stiffener-Tall";

    /// <inheritdoc/>
    public string Description => "Warns if tall cabinet lacks horizontal stiffener.";

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        double threshold = context.TenantStandard.TallCabinetHeightThreshold;

        if (context.AssemblyDimension.Height <= threshold)
            return ConstructionRuleResult.Empty;

        // A stiffener is any extra part beyond the four base cuboid panels.
        var baseCuboid = skeleton.BaseCuboid;
        var baseCuboidPartIds = new HashSet<Guid>
        {
            baseCuboid.LeftSide.Id,
            baseCuboid.RightSide.Id,
            baseCuboid.Bottom.Id,
            baseCuboid.Top.Id
        };
        if (baseCuboid.BackPanel is not null)
            baseCuboidPartIds.Add(baseCuboid.BackPanel.Id);

        bool hasStiffener = skeleton.Parts.Any(p => !baseCuboidPartIds.Contains(p.Id));

        if (hasStiffener)
            return ConstructionRuleResult.Empty;

        var advisory = new DesignAdvisory(
            RuleId,
            AdvisorySeverity.Warning,
            "Skeleton",
            "Tall cabinet without horizontal stiffener — structural integrity may be compromised.",
            "Add a horizontal cross rail at mid-height.");

        return new ConstructionRuleResult(
            Array.Empty<MachiningFeature>(),
            new[] { advisory });
    }
}
