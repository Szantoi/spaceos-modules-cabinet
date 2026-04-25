using Ardalis.Result;

namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// An immutable 3D vector value object. All components must be finite (SEC-CAB-1).
/// Use <see cref="Create"/> to construct; direct instantiation is not possible.
/// </summary>
public readonly record struct Vector3
{
    /// <summary>X component in millimetres (or unitless for direction vectors).</summary>
    public double X { get; }

    /// <summary>Y component.</summary>
    public double Y { get; }

    /// <summary>Z component.</summary>
    public double Z { get; }

    private Vector3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    // ── Static instances ────────────────────────────────────────────────────

    /// <summary>The zero vector (0, 0, 0).</summary>
    public static readonly Vector3 Zero = new(0, 0, 0);

    /// <summary>Unit vector along X axis.</summary>
    public static readonly Vector3 UnitX = new(1, 0, 0);

    /// <summary>Unit vector along Y axis.</summary>
    public static readonly Vector3 UnitY = new(0, 1, 0);

    /// <summary>Unit vector along Z axis.</summary>
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    // ── Factory ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="Vector3"/> from components. Rejects NaN and Infinity (SEC-CAB-1).
    /// </summary>
    /// <param name="x">X component.</param>
    /// <param name="y">Y component.</param>
    /// <param name="z">Z component.</param>
    /// <returns>Success with the vector, or Invalid if any component is non-finite.</returns>
    public static Result<Vector3> Create(double x, double y, double z)
    {
        if (!IsFinite(x) || !IsFinite(y) || !IsFinite(z))
            return Result<Vector3>.Invalid(new ValidationError("Vector3 components must be finite (no NaN or Infinity)."));

        return Result<Vector3>.Success(new Vector3(x, y, z));
    }

    // ── Validity ─────────────────────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if all components are finite.</summary>
    public bool IsValid() => IsFinite(X) && IsFinite(Y) && IsFinite(Z);

    // ── Geometry ─────────────────────────────────────────────────────────────

    /// <summary>Euclidean length of this vector.</summary>
    public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a unit vector in the same direction.
    /// Returns an invalid result if this is a zero vector.
    /// </summary>
    public Result<Vector3> Normalized()
    {
        double len = Length();
        if (len < GeometryConstants.DefaultEpsilon)
            return Result<Vector3>.Invalid(new ValidationError("Cannot normalise a zero-length vector."));

        return Result<Vector3>.Success(new Vector3(X / len, Y / len, Z / len));
    }

    /// <summary>Dot product with <paramref name="other"/>.</summary>
    public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    /// <summary>
    /// Cross product with <paramref name="other"/>. Returns Invalid if the result would be non-finite.
    /// </summary>
    public Result<Vector3> Cross(Vector3 other)
    {
        double cx = Y * other.Z - Z * other.Y;
        double cy = Z * other.X - X * other.Z;
        double cz = X * other.Y - Y * other.X;

        if (!IsFinite(cx) || !IsFinite(cy) || !IsFinite(cz))
            return Result<Vector3>.Invalid(new ValidationError("Cross product produced non-finite components."));

        return Result<Vector3>.Success(new Vector3(cx, cy, cz));
    }

    /// <summary>
    /// Returns <c>true</c> if all components are within <paramref name="epsilon"/> of <paramref name="other"/>.
    /// </summary>
    public bool IsApproximatelyEqualTo(Vector3 other, double epsilon = GeometryConstants.DefaultEpsilon) =>
        Math.Abs(X - other.X) <= epsilon &&
        Math.Abs(Y - other.Y) <= epsilon &&
        Math.Abs(Z - other.Z) <= epsilon;

    // ── Operators ────────────────────────────────────────────────────────────

    /// <summary>Component-wise addition.</summary>
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Component-wise subtraction.</summary>
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Scalar multiplication.</summary>
    public static Vector3 operator *(Vector3 v, double scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    /// <summary>Scalar multiplication (commutative).</summary>
    public static Vector3 operator *(double scalar, Vector3 v) => v * scalar;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    /// <inheritdoc/>
    public override string ToString() => $"({X:G6}, {Y:G6}, {Z:G6})";
}
