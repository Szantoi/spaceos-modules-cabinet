using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Generates rabbet machining on the back edges of the four main carcass panels
/// when the tenant standard specifies <see cref="BackPanelAttachmentDefault.Rabbet"/>.
/// </summary>
public sealed class BackPanelRabbetRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-BackPanel-Visible";

    /// <inheritdoc/>
    public string Description => "Generates rabbet for visible back panel attachment.";

    /// <summary>Rabbet width (step width) in mm.</summary>
    private const double RabbetWidth = 8.0;

    /// <summary>Clearance added to panel thickness for rabbet depth in mm.</summary>
    private const double RabbetClearance = 0.5;

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        if (context.TenantStandard.BackPanelAttachment != BackPanelAttachmentDefault.Rabbet)
            return ConstructionRuleResult.Empty;

        var machinings = new List<MachiningFeature>();
        double rabbetDepth = context.TenantStandard.DefaultBackPanelThickness + RabbetClearance;

        var parts = new[]
        {
            skeleton.BaseCuboid.LeftSide,
            skeleton.BaseCuboid.RightSide,
            skeleton.BaseCuboid.Bottom,
            skeleton.BaseCuboid.Top
        };

        foreach (var part in parts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parameters = new MachiningParameters(depth: rabbetDepth, width: RabbetWidth);
            var feature = MachiningFeature.Create(
                new EdgeSubject(part.Id, PartEdge.BackRight),
                MachiningOperation.Rabbet,
                parameters);

            if (feature.IsSuccess)
                machinings.Add(feature.Value);
        }

        return new ConstructionRuleResult(machinings.AsReadOnly(), Array.Empty<DesignAdvisory>());
    }
}
