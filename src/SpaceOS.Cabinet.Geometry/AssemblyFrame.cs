using Ardalis.Result;

namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// Immutable frame that positions and orients a cabinet assembly in world space.
/// </summary>
public sealed record AssemblyFrame
{
    /// <summary>
    /// The gravity direction in world space (pointing downward along -Z by convention).
    /// </summary>
    public static readonly Vector3 GravityDirection = Vector3.Create(0, 0, -1).Value;

    /// <summary>Overall dimensions of this assembly.</summary>
    public AssemblyDimension Dimension { get; }

    /// <summary>Transform from assembly space to world space.</summary>
    public AffineTransform AssemblyToWorld { get; }

    private AssemblyFrame(AssemblyDimension dimension, AffineTransform assemblyToWorld)
    {
        Dimension = dimension;
        AssemblyToWorld = assemblyToWorld;
    }

    /// <summary>
    /// Creates an <see cref="AssemblyFrame"/>.
    /// </summary>
    /// <param name="dimension">Outer dimensions of the assembly.</param>
    /// <param name="assemblyToWorld">A valid affine transform from assembly space to world space.</param>
    public static Result<AssemblyFrame> Create(AssemblyDimension dimension, AffineTransform assemblyToWorld)
    {
        if (!assemblyToWorld.IsValid())
            return Result<AssemblyFrame>.Invalid(new ValidationError("assemblyToWorld transform is not valid."));

        return Result<AssemblyFrame>.Success(new AssemblyFrame(dimension, assemblyToWorld));
    }
}
