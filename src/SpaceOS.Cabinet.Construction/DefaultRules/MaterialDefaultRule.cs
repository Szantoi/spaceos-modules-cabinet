using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Construction.DefaultRules;

/// <summary>
/// Emits informational advisories for parts that still carry the default carcass material
/// rather than an explicit tenant material selection.
/// </summary>
public sealed class MaterialDefaultRule : IConstructionRule
{
    /// <inheritdoc/>
    public string RuleId => "R-Material-Default";

    /// <inheritdoc/>
    public string Description => "Applies default material from TenantStandard to parts without explicit material.";

    /// <summary>Material reference string used for unassigned carcass parts.</summary>
    private const string DefaultCarcassReference = "default-carcass";

    /// <inheritdoc/>
    public ConstructionRuleResult Apply(
        Skeleton skeleton,
        IConstructionContext context,
        CancellationToken cancellationToken)
    {
        var advisories = new List<DesignAdvisory>();

        foreach (var part in skeleton.Parts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(part.MaterialReference) ||
                part.MaterialReference == DefaultCarcassReference)
            {
                advisories.Add(new DesignAdvisory(
                    RuleId,
                    AdvisorySeverity.Info,
                    $"Part:{part.Id}",
                    "Part uses default material from tenant standard.",
                    null));
            }
        }

        return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), advisories.AsReadOnly());
    }
}
