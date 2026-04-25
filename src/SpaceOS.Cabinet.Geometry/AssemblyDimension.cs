using Ardalis.Result;

namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// Immutable outer dimensions (in mm) of a cabinet assembly: width, height, depth.
/// All values are validated against physical limits (SEC-CAB-3).
/// </summary>
public readonly record struct AssemblyDimension
{
    /// <summary>Maximum allowed width in mm.</summary>
    public const double MaxWidth = 6000;

    /// <summary>Maximum allowed height in mm.</summary>
    public const double MaxHeight = 6000;

    /// <summary>Maximum allowed depth in mm.</summary>
    public const double MaxDepth = 1500;

    /// <summary>Minimum allowed dimension in mm for any axis.</summary>
    public const double MinDimension = 50;

    /// <summary>Width of the assembly in mm.</summary>
    public double Width { get; }

    /// <summary>Height of the assembly in mm.</summary>
    public double Height { get; }

    /// <summary>Depth of the assembly in mm.</summary>
    public double Depth { get; }

    private AssemblyDimension(double width, double height, double depth)
    {
        Width = width;
        Height = height;
        Depth = depth;
    }

    /// <summary>
    /// Creates an <see cref="AssemblyDimension"/> from the given mm values.
    /// Validates finiteness and physical limits (SEC-CAB-1, SEC-CAB-3).
    /// </summary>
    /// <param name="width">Width in mm. Must be in [MinDimension, MaxWidth].</param>
    /// <param name="height">Height in mm. Must be in [MinDimension, MaxHeight].</param>
    /// <param name="depth">Depth in mm. Must be in [MinDimension, MaxDepth].</param>
    public static Result<AssemblyDimension> Create(double width, double height, double depth)
    {
        if (!IsFinite(width) || !IsFinite(height) || !IsFinite(depth))
            return Result<AssemblyDimension>.Invalid(new ValidationError("Dimensions must be finite (no NaN or Infinity)."));

        if (width < MinDimension)
            return Result<AssemblyDimension>.Invalid(new ValidationError($"Width must be >= {MinDimension} mm."));
        if (width > MaxWidth)
            return Result<AssemblyDimension>.Invalid(new ValidationError($"Width must be <= {MaxWidth} mm."));

        if (height < MinDimension)
            return Result<AssemblyDimension>.Invalid(new ValidationError($"Height must be >= {MinDimension} mm."));
        if (height > MaxHeight)
            return Result<AssemblyDimension>.Invalid(new ValidationError($"Height must be <= {MaxHeight} mm."));

        if (depth < MinDimension)
            return Result<AssemblyDimension>.Invalid(new ValidationError($"Depth must be >= {MinDimension} mm."));
        if (depth > MaxDepth)
            return Result<AssemblyDimension>.Invalid(new ValidationError($"Depth must be <= {MaxDepth} mm."));

        return Result<AssemblyDimension>.Success(new AssemblyDimension(width, height, depth));
    }

    /// <summary>
    /// Returns the dimensions as a <see cref="Vector3"/> (Width, Depth, Height).
    /// </summary>
    public Result<Vector3> ToVector() => Vector3.Create(Width, Depth, Height);

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    /// <inheritdoc/>
    public override string ToString() => $"AssemblyDimension(W={Width}, H={Height}, D={Depth})";
}
