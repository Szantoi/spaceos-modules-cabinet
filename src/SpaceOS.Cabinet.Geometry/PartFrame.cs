using Ardalis.Result;

namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// Immutable frame that positions and orients a <see cref="PartDimension"/> within assembly space.
/// </summary>
public sealed record PartFrame
{
    /// <summary>Transform from part-local space to assembly space.</summary>
    public AffineTransform LocalToAssembly { get; }

    /// <summary>Physical dimensions of this part.</summary>
    public PartDimension Dimension { get; }

    private PartFrame(AffineTransform localToAssembly, PartDimension dimension)
    {
        LocalToAssembly = localToAssembly;
        Dimension = dimension;
    }

    /// <summary>
    /// Creates a <see cref="PartFrame"/>.
    /// </summary>
    /// <param name="localToAssembly">A valid affine transform from part local space to assembly space.</param>
    /// <param name="dimension">Physical dimensions of the part.</param>
    public static Result<PartFrame> Create(AffineTransform localToAssembly, PartDimension dimension)
    {
        if (!localToAssembly.IsValid())
            return Result<PartFrame>.Invalid(new ValidationError("localToAssembly transform is not valid."));

        return Result<PartFrame>.Success(new PartFrame(localToAssembly, dimension));
    }

    /// <summary>
    /// Returns the grain direction (local X axis) expressed in assembly space.
    /// </summary>
    public Result<Vector3> GrainDirectionInAssembly() =>
        LocalToAssembly.ApplyToDirection(Vector3.UnitX);

    /// <summary>
    /// Returns the surface normal (local Z axis) expressed in assembly space.
    /// </summary>
    public Result<Vector3> NormalInAssembly() =>
        LocalToAssembly.ApplyToDirection(Vector3.UnitZ);

    /// <summary>
    /// Returns the datum (origin) of this part expressed in assembly space.
    /// </summary>
    public Result<Vector3> DatumInAssembly() =>
        LocalToAssembly.ApplyTo(Vector3.Zero);
}
