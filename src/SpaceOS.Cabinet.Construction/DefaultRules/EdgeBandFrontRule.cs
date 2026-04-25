using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Applies edge-banding operations to the front-facing edge of every part in the skeleton.
/// </summary>
public sealed class EdgeBandFrontRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-EdgeBand-FrontVisible";

    /// <inheritdoc/>
    public string Description => "Applies edge banding to front-facing edges.";

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        var machinings = new List<MachiningFeature>();

        foreach (var part in skeleton.Parts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var feature = MachiningFeature.Create(
                new EdgeSubject(part.Id, PartEdge.FrontLeft),
                MachiningOperation.EdgeBand,
                new MachiningParameters());

            if (feature.IsSuccess)
                machinings.Add(feature.Value);
        }

        return new ConstructionRuleResult(machinings.AsReadOnly(), Array.Empty<DesignAdvisory>());
    }
}
