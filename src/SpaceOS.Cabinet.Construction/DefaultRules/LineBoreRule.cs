using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Generates 32 mm system line-bore drilling on the left and right side panels.
/// Controlled by <see cref="ITenantStandardProvider.LineBoreEnabled"/>.
/// </summary>
public sealed class LineBoreRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-32mm-LineBore";

    /// <inheritdoc/>
    public string Description => "Generates 32mm system line bore drilling on vertical panels.";

    /// <summary>Standard 32 mm system hole depth in mm.</summary>
    private const double StandardDepth = 12.0;

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.TenantStandard.LineBoreEnabled)
            return ConstructionRuleResult.Empty;

        var machinings = new List<MachiningFeature>();

        double firstOffset = context.TenantStandard.LineBoreFirstHoleOffset;
        double spacing = context.TenantStandard.LineBoreSpacing;
        double diameter = context.TenantStandard.LineBoreDiameter;

        // Apply to left and right side panels.
        var sideParts = new[] { skeleton.BaseCuboid.LeftSide, skeleton.BaseCuboid.RightSide };

        foreach (var part in sideParts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // For a vertical side panel, Length is the panel's height.
            double partHeight = part.Frame.Dimension.Length;
            double currentPos = firstOffset;

            while (currentPos < partHeight - firstOffset)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parameters = new MachiningParameters(depth: StandardDepth, diameter: diameter);
                var feature = MachiningFeature.Create(
                    new PlaneSubject(part.Id, PartFace.Left),
                    MachiningOperation.Drill,
                    parameters);

                if (feature.IsSuccess)
                    machinings.Add(feature.Value);

                currentPos += spacing;
            }
        }

        return new ConstructionRuleResult(machinings.AsReadOnly(), Array.Empty<DesignAdvisory>());
    }
}
