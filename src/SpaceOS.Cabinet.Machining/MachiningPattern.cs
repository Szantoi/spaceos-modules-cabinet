using Ardalis.Result;

namespace SpaceOS.Cabinet.Machining;

/// <summary>
/// A single machining template within a pattern — defines operation and parameters.
/// </summary>
/// <param name="Operation">The machining operation to apply.</param>
/// <param name="Parameters">Numeric and geometric parameters for the operation.</param>
public sealed record MachiningTemplate(MachiningOperation Operation, MachiningParameters Parameters);

/// <summary>
/// A named template that describes the complete set of machining operations required
/// to install a hardware item (e.g. a hinge, a dowel set, a shelf pin pair).
/// Used to generate multiple <see cref="MachiningFeature"/>s from a single hardware placement.
/// </summary>
public sealed class MachiningPattern
{
    /// <summary>Unique identifier for this pattern (e.g. "blum-clip-top-hinge").</summary>
    public string PatternId { get; }

    /// <summary>Human-readable description of what this pattern represents.</summary>
    public string Description { get; }

    private readonly List<MachiningTemplate> _templates;

    /// <summary>The ordered list of machining templates in this pattern.</summary>
    public IReadOnlyList<MachiningTemplate> Templates => _templates.AsReadOnly();

    /// <summary>
    /// Initialises a new <see cref="MachiningPattern"/>.
    /// </summary>
    /// <param name="patternId">Unique pattern identifier.</param>
    /// <param name="description">Human-readable description.</param>
    /// <param name="templates">Ordered list of machining templates.</param>
    public MachiningPattern(string patternId, string description, IEnumerable<MachiningTemplate> templates)
    {
        PatternId = patternId;
        Description = description;
        _templates = templates.ToList();
    }

    /// <summary>
    /// Generates <see cref="MachiningFeature"/>s from this pattern for the given subject and hardware.
    /// All templates are applied to the same <paramref name="subject"/>.
    /// </summary>
    /// <param name="subject">The geometry target for all generated features (required).</param>
    /// <param name="hardware">The hardware catalog reference to attach to each feature.</param>
    /// <returns>
    /// Success with the generated feature list, or Invalid/Error if generation fails.
    /// </returns>
    public Result<IReadOnlyList<MachiningFeature>> GenerateFeatures(
        MachiningSubject subject,
        HardwareReference hardware)
    {
        if (subject is null)
            return Result<IReadOnlyList<MachiningFeature>>.Invalid(
                new ValidationError("Subject is required."));

        var features = new List<MachiningFeature>();
        foreach (var template in _templates)
        {
            var result = MachiningFeature.Create(subject, template.Operation, template.Parameters, hardware);
            if (!result.IsSuccess)
                return Result<IReadOnlyList<MachiningFeature>>.Error(
                    $"Failed to create feature from template: {string.Join(", ", result.Errors)}");

            features.Add(result.Value);
        }

        return Result<IReadOnlyList<MachiningFeature>>.Success(features.AsReadOnly());
    }
}
