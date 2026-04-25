using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Emits an Info advisory for non-base parts whose length exceeds the tenant's long-shelf threshold,
/// indicating potential sag risk under load.
/// </summary>
public sealed class ShelfSagRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-Shelf-Sag";

    /// <inheritdoc/>
    public string Description => "Informs about potential shelf sag for long shelves with dado joints.";

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        double threshold = context.TenantStandard.LongShelfThreshold;
        var advisories = new List<DesignAdvisory>();

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

        foreach (var part in skeleton.Parts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip base cuboid panels — only evaluate added shelves/dividers.
            if (baseCuboidPartIds.Contains(part.Id))
                continue;

            if (part.Frame.Dimension.Length > threshold)
            {
                advisories.Add(new DesignAdvisory(
                    RuleId,
                    AdvisorySeverity.Info,
                    $"Part:{part.Id}",
                    "Long shelf may experience sag under load — consider reinforcement or thicker material.",
                    "Use thicker board or add centre support."));
            }
        }

        return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), advisories.AsReadOnly());
    }
}
