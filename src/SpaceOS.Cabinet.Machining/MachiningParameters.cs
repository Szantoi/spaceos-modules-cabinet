using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Machining;

/// <summary>
/// Holds the numeric and geometric parameters for a machining operation.
/// All fields are optional because different operations require different parameters
/// (e.g., a drill needs Diameter + Depth; a groove needs Width + Depth).
/// </summary>
public sealed record MachiningParameters
{
    /// <summary>Cutting depth in mm (positive value into the material).</summary>
    public double? Depth { get; init; }

    /// <summary>Cut width in mm (for grooves, rabbets, pockets).</summary>
    public double? Width { get; init; }

    /// <summary>Drill diameter in mm.</summary>
    public double? Diameter { get; init; }

    /// <summary>Cut length in mm (for grooves, profiles).</summary>
    public double? Length { get; init; }

    /// <summary>Optional tool approach direction in part-local space.</summary>
    public Vector3? Direction { get; init; }

    /// <summary>Optional explicit placement transform of the feature origin in part-local space.</summary>
    public AffineTransform? Placement { get; init; }

    /// <summary>Creates an empty <see cref="MachiningParameters"/> instance.</summary>
    public MachiningParameters() { }

    /// <summary>
    /// Creates a <see cref="MachiningParameters"/> with the specified values.
    /// All parameters are optional; pass only what the operation requires.
    /// </summary>
    public MachiningParameters(
        double? depth = null,
        double? width = null,
        double? diameter = null,
        double? length = null,
        Vector3? direction = null,
        AffineTransform? placement = null)
    {
        Depth = depth;
        Width = width;
        Diameter = diameter;
        Length = length;
        Direction = direction;
        Placement = placement;
    }
}
