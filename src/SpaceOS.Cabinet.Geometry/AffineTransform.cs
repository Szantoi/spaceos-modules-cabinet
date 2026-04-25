using Ardalis.Result;

namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// An immutable 4x4 affine transformation matrix stored in row-major order.
/// The last row is always [0, 0, 0, 1].
/// </summary>
/// <remarks>
/// Layout (row-major):
/// <code>
/// | m0  m1  m2  m3  |   BasisX.X  BasisY.X  BasisZ.X  Origin.X
/// | m4  m5  m6  m7  |   BasisX.Y  BasisY.Y  BasisZ.Y  Origin.Y
/// | m8  m9  m10 m11 |   BasisX.Z  BasisY.Z  BasisZ.Z  Origin.Z
/// | m12 m13 m14 m15 |   0         0         0         1
/// </code>
/// </remarks>
public readonly record struct AffineTransform
{
    // 16 elements, row-major. Stored as a fixed-size array on the heap.
    private readonly double[] _m;

    private AffineTransform(double[] m)
    {
        _m = m;
    }

    // ── Static instances ────────────────────────────────────────────────────

    /// <summary>The 4x4 identity transform.</summary>
    public static readonly AffineTransform Identity = new(new double[]
    {
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    });

    // ── Factories ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a pure translation transform.
    /// </summary>
    /// <param name="offset">Translation vector; must be finite.</param>
    public static Result<AffineTransform> Translation(Vector3 offset)
    {
        if (!offset.IsValid())
            return Result<AffineTransform>.Invalid(new ValidationError("Translation offset must be finite."));

        return Result<AffineTransform>.Success(new AffineTransform(new double[]
        {
            1, 0, 0, offset.X,
            0, 1, 0, offset.Y,
            0, 0, 1, offset.Z,
            0, 0, 0, 1
        }));
    }

    /// <summary>
    /// Creates a rotation around <paramref name="axis"/> by <paramref name="radians"/> using Rodrigues' formula.
    /// </summary>
    /// <param name="axis">Rotation axis (need not be unit length, but must be non-zero).</param>
    /// <param name="radians">Angle in radians. Must be finite.</param>
    public static Result<AffineTransform> Rotation(Vector3 axis, double radians)
    {
        if (!axis.IsValid())
            return Result<AffineTransform>.Invalid(new ValidationError("Rotation axis must be finite."));

        if (!IsFinite(radians))
            return Result<AffineTransform>.Invalid(new ValidationError("Rotation angle must be finite."));

        var normalised = axis.Normalized();
        if (!normalised.IsSuccess)
            return Result<AffineTransform>.Invalid(new ValidationError("Rotation axis must not be a zero vector."));

        Vector3 u = normalised.Value;
        double c = Math.Cos(radians);
        double s = Math.Sin(radians);
        double t = 1.0 - c;

        double ux = u.X, uy = u.Y, uz = u.Z;

        var m = new double[]
        {
            t*ux*ux + c,      t*ux*uy - s*uz,  t*ux*uz + s*uy,  0,
            t*ux*uy + s*uz,   t*uy*uy + c,     t*uy*uz - s*ux,  0,
            t*ux*uz - s*uy,   t*uy*uz + s*ux,  t*uz*uz + c,     0,
            0,                0,               0,               1
        };

        if (!AllFinite(m))
            return Result<AffineTransform>.Invalid(new ValidationError("Rotation produced non-finite matrix elements."));

        return Result<AffineTransform>.Success(new AffineTransform(m));
    }

    /// <summary>
    /// Creates a non-uniform scaling transform.
    /// </summary>
    /// <param name="sx">Scale along X.</param>
    /// <param name="sy">Scale along Y.</param>
    /// <param name="sz">Scale along Z.</param>
    public static Result<AffineTransform> Scaling(double sx, double sy, double sz)
    {
        if (!IsFinite(sx) || !IsFinite(sy) || !IsFinite(sz))
            return Result<AffineTransform>.Invalid(new ValidationError("Scale factors must be finite."));

        return Result<AffineTransform>.Success(new AffineTransform(new double[]
        {
            sx, 0,  0,  0,
            0,  sy, 0,  0,
            0,  0,  sz, 0,
            0,  0,  0,  1
        }));
    }

    /// <summary>
    /// Composes two transforms: applies <paramref name="a"/> first, then <paramref name="b"/> (i.e. b * a).
    /// </summary>
    public static Result<AffineTransform> Compose(AffineTransform a, AffineTransform b)
    {
        if (!a.IsValid() || !b.IsValid())
            return Result<AffineTransform>.Invalid(new ValidationError("Cannot compose invalid transforms."));

        double[] am = a._m;
        double[] bm = b._m;
        var result = new double[16];

        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                double sum = 0;
                for (int k = 0; k < 4; k++)
                    sum += bm[row * 4 + k] * am[k * 4 + col];
                result[row * 4 + col] = sum;
            }
        }

        if (!AllFinite(result))
            return Result<AffineTransform>.Invalid(new ValidationError("Composition produced non-finite matrix elements."));

        return Result<AffineTransform>.Success(new AffineTransform(result));
    }

    // ── Validity ─────────────────────────────────────────────────────────────

    /// <summary>Returns <c>true</c> if all matrix elements are finite.</summary>
    public bool IsValid() => _m is not null && AllFinite(_m);

    // ── Application ──────────────────────────────────────────────────────────

    /// <summary>
    /// Transforms <paramref name="point"/> as a position (includes translation).
    /// </summary>
    public Result<Vector3> ApplyTo(Vector3 point)
    {
        if (!IsValid())
            return Result<Vector3>.Invalid(new ValidationError("Transform is not valid."));

        if (!point.IsValid())
            return Result<Vector3>.Invalid(new ValidationError("Point must be finite."));

        double[] m = _m;
        double x = m[0] * point.X + m[1] * point.Y + m[2]  * point.Z + m[3];
        double y = m[4] * point.X + m[5] * point.Y + m[6]  * point.Z + m[7];
        double z = m[8] * point.X + m[9] * point.Y + m[10] * point.Z + m[11];

        return Vector3.Create(x, y, z);
    }

    /// <summary>
    /// Transforms <paramref name="direction"/> as a vector (translation-free).
    /// </summary>
    public Result<Vector3> ApplyToDirection(Vector3 direction)
    {
        if (!IsValid())
            return Result<Vector3>.Invalid(new ValidationError("Transform is not valid."));

        if (!direction.IsValid())
            return Result<Vector3>.Invalid(new ValidationError("Direction must be finite."));

        double[] m = _m;
        double x = m[0] * direction.X + m[1] * direction.Y + m[2]  * direction.Z;
        double y = m[4] * direction.X + m[5] * direction.Y + m[6]  * direction.Z;
        double z = m[8] * direction.X + m[9] * direction.Y + m[10] * direction.Z;

        return Vector3.Create(x, y, z);
    }

    /// <summary>
    /// Computes the inverse of this transform. Returns Invalid if the matrix is singular.
    /// </summary>
    public Result<AffineTransform> Inverse()
    {
        if (!IsValid())
            return Result<AffineTransform>.Invalid(new ValidationError("Cannot invert an invalid transform."));

        // For an affine transform with last row [0,0,0,1]:
        // R is the upper-left 3x3, t is the translation column.
        // Inverse = [R^T | -R^T * t; 0 0 0 1]
        // This only works if the 3x3 sub-matrix is orthogonal / invertible.
        // We use general 4x4 cofactor / adjugate for correctness with non-orthogonal (scaling) matrices.
        double[] m = _m;
        double det = Det4x4(m);

        if (Math.Abs(det) < 1e-12)
            return Result<AffineTransform>.Invalid(new ValidationError("Transform is singular and cannot be inverted."));

        double[] inv = Adjugate4x4(m);
        double invDet = 1.0 / det;
        for (int i = 0; i < 16; i++)
            inv[i] *= invDet;

        if (!AllFinite(inv))
            return Result<AffineTransform>.Invalid(new ValidationError("Inversion produced non-finite elements."));

        return Result<AffineTransform>.Success(new AffineTransform(inv));
    }

    // ── Basis extractors ─────────────────────────────────────────────────────

    /// <summary>Extracts the X basis vector (first column of the rotation block).</summary>
    public Vector3 BasisX() => new Vector3Builder(_m[0], _m[4], _m[8]).Build();

    /// <summary>Extracts the Y basis vector (second column of the rotation block).</summary>
    public Vector3 BasisY() => new Vector3Builder(_m[1], _m[5], _m[9]).Build();

    /// <summary>Extracts the Z basis vector (third column of the rotation block).</summary>
    public Vector3 BasisZ() => new Vector3Builder(_m[2], _m[6], _m[10]).Build();

    /// <summary>Extracts the translation (origin) vector (fourth column).</summary>
    public Vector3 Origin() => new Vector3Builder(_m[3], _m[7], _m[11]).Build();

    // ── Approximate equality ─────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> if all corresponding matrix elements are within <paramref name="epsilon"/>.
    /// </summary>
    public bool IsApproximatelyEqualTo(AffineTransform other, double epsilon = 1e-9)
    {
        if (_m is null || other._m is null) return false;
        for (int i = 0; i < 16; i++)
        {
            if (Math.Abs(_m[i] - other._m[i]) > epsilon)
                return false;
        }
        return true;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    private static bool AllFinite(double[] arr)
    {
        foreach (double v in arr)
            if (!IsFinite(v)) return false;
        return true;
    }

    private static double Det3x3(double a, double b, double c,
                                  double d, double e, double f,
                                  double g, double h, double i)
        => a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);

    private static double Det4x4(double[] m)
    {
        // Cofactor expansion along first row
        double c0 = Det3x3(m[5], m[6], m[7], m[9], m[10], m[11], m[13], m[14], m[15]);
        double c1 = Det3x3(m[4], m[6], m[7], m[8], m[10], m[11], m[12], m[14], m[15]);
        double c2 = Det3x3(m[4], m[5], m[7], m[8], m[9],  m[11], m[12], m[13], m[15]);
        double c3 = Det3x3(m[4], m[5], m[6], m[8], m[9],  m[10], m[12], m[13], m[14]);

        return m[0] * c0 - m[1] * c1 + m[2] * c2 - m[3] * c3;
    }

    private static double[] Adjugate4x4(double[] m)
    {
        // Compute transpose of cofactor matrix
        var adj = new double[16];

        adj[0]  =  Det3x3(m[5], m[6], m[7],  m[9], m[10], m[11], m[13], m[14], m[15]);
        adj[4]  = -Det3x3(m[4], m[6], m[7],  m[8], m[10], m[11], m[12], m[14], m[15]);
        adj[8]  =  Det3x3(m[4], m[5], m[7],  m[8], m[9],  m[11], m[12], m[13], m[15]);
        adj[12] = -Det3x3(m[4], m[5], m[6],  m[8], m[9],  m[10], m[12], m[13], m[14]);

        adj[1]  = -Det3x3(m[1], m[2], m[3],  m[9], m[10], m[11], m[13], m[14], m[15]);
        adj[5]  =  Det3x3(m[0], m[2], m[3],  m[8], m[10], m[11], m[12], m[14], m[15]);
        adj[9]  = -Det3x3(m[0], m[1], m[3],  m[8], m[9],  m[11], m[12], m[13], m[15]);
        adj[13] =  Det3x3(m[0], m[1], m[2],  m[8], m[9],  m[10], m[12], m[13], m[14]);

        adj[2]  =  Det3x3(m[1], m[2], m[3],  m[5], m[6],  m[7],  m[13], m[14], m[15]);
        adj[6]  = -Det3x3(m[0], m[2], m[3],  m[4], m[6],  m[7],  m[12], m[14], m[15]);
        adj[10] =  Det3x3(m[0], m[1], m[3],  m[4], m[5],  m[7],  m[12], m[13], m[15]);
        adj[14] = -Det3x3(m[0], m[1], m[2],  m[4], m[5],  m[6],  m[12], m[13], m[14]);

        adj[3]  = -Det3x3(m[1], m[2], m[3],  m[5], m[6],  m[7],  m[9],  m[10], m[11]);
        adj[7]  =  Det3x3(m[0], m[2], m[3],  m[4], m[6],  m[7],  m[8],  m[10], m[11]);
        adj[11] = -Det3x3(m[0], m[1], m[3],  m[4], m[5],  m[7],  m[8],  m[9],  m[11]);
        adj[15] =  Det3x3(m[0], m[1], m[2],  m[4], m[5],  m[6],  m[8],  m[9],  m[10]);

        return adj;
    }

    /// <summary>Builds a raw Vector3 bypassing validation — only for known-valid extracted components.</summary>
    private readonly struct Vector3Builder(double x, double y, double z)
    {
        public Vector3 Build() => Vector3.Create(x, y, z).Value;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        _m is null ? "AffineTransform(null)" :
        $"[{_m[0]:G4},{_m[1]:G4},{_m[2]:G4},{_m[3]:G4} | {_m[4]:G4},{_m[5]:G4},{_m[6]:G4},{_m[7]:G4} | {_m[8]:G4},{_m[9]:G4},{_m[10]:G4},{_m[11]:G4}]";
}
