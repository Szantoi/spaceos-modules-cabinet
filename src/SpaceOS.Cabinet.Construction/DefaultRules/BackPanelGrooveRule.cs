using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Generates groove machining on the inner back edges of the four main carcass panels
/// when the tenant standard specifies <see cref="BackPanelAttachmentDefault.Groove"/>.
/// </summary>
public sealed class BackPanelGrooveRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-BackPanel-Hidden";

    /// <inheritdoc/>
    public string Description => "Generates groove for hidden back panel attachment.";

    /// <summary>Groove depth in mm.</summary>
    private const double GrooveDepth = 8.0;

    /// <summary>Clearance added to panel thickness for groove width in mm.</summary>
    private const double GrooveClearance = 0.2;

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        if (context.TenantStandard.BackPanelAttachment != BackPanelAttachmentDefault.Groove)
            return ConstructionRuleResult.Empty;

        var machinings = new List<MachiningFeature>();
        double grooveWidth = context.TenantStandard.DefaultBackPanelThickness + GrooveClearance;

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

            var parameters = new MachiningParameters(depth: GrooveDepth, width: grooveWidth);
            var feature = MachiningFeature.Create(
                new EdgeSubject(part.Id, PartEdge.BackLeft),
                MachiningOperation.Groove,
                parameters);

            if (feature.IsSuccess)
                machinings.Add(feature.Value);
        }

        return new ConstructionRuleResult(machinings.AsReadOnly(), Array.Empty<DesignAdvisory>());
    }
}
