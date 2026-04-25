using Ardalis.Result;

namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// Immutable physical dimensions (in mm) of a single cabinet part: length, width, thickness.
/// All values are validated against physical limits (SEC-CAB-3).
/// </summary>
public readonly record struct PartDimension
{
    /// <summary>Maximum allowed length in mm.</summary>
    public const double MaxLength = 6000;

    /// <summary>Maximum allowed width in mm.</summary>
    public const double MaxWidth = 3000;

    /// <summary>Maximum allowed thickness in mm.</summary>
    public const double MaxThickness = 100;

    /// <summary>Minimum allowed dimension in mm for any axis.</summary>
    public const double MinDimension = 0.1;

    /// <summary>Length of the part in mm.</summary>
    public double Length { get; }

    /// <summary>Width of the part in mm.</summary>
    public double Width { get; }

    /// <summary>Thickness of the part in mm.</summary>
    public double Thickness { get; }

    private PartDimension(double length, double width, double thickness)
    {
        Length = length;
        Width = width;
        Thickness = thickness;
    }

    /// <summary>
    /// Creates a <see cref="PartDimension"/> from the given mm values.
    /// Validates finiteness and physical limits (SEC-CAB-1, SEC-CAB-3).
    /// </summary>
    /// <param name="length">Length in mm. Must be in [MinDimension, MaxLength].</param>
    /// <param name="width">Width in mm. Must be in [MinDimension, MaxWidth].</param>
    /// <param name="thickness">Thickness in mm. Must be in [MinDimension, MaxThickness].</param>
    public static Result<PartDimension> Create(double length, double width, double thickness)
    {
        if (!IsFinite(length) || !IsFinite(width) || !IsFinite(thickness))
            return Result<PartDimension>.Invalid(new ValidationError("Dimensions must be finite (no NaN or Infinity)."));

        if (length < MinDimension)
            return Result<PartDimension>.Invalid(new ValidationError($"Length must be >= {MinDimension} mm."));
        if (length > MaxLength)
            return Result<PartDimension>.Invalid(new ValidationError($"Length must be <= {MaxLength} mm."));

        if (width < MinDimension)
            return Result<PartDimension>.Invalid(new ValidationError($"Width must be >= {MinDimension} mm."));
        if (width > MaxWidth)
            return Result<PartDimension>.Invalid(new ValidationError($"Width must be <= {MaxWidth} mm."));

        if (thickness < MinDimension)
            return Result<PartDimension>.Invalid(new ValidationError($"Thickness must be >= {MinDimension} mm."));
        if (thickness > MaxThickness)
            return Result<PartDimension>.Invalid(new ValidationError($"Thickness must be <= {MaxThickness} mm."));

        return Result<PartDimension>.Success(new PartDimension(length, width, thickness));
    }

    /// <summary>
    /// Returns the dimensions as a <see cref="Vector3"/> (Length, Width, Thickness).
    /// </summary>
    public Result<Vector3> ToVector() => Vector3.Create(Length, Width, Thickness);

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    /// <inheritdoc/>
    public override string ToString() => $"PartDimension(L={Length}, W={Width}, T={Thickness})";
}
