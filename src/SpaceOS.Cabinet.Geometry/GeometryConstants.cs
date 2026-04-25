namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// Shared epsilon constants used across geometry operations.
/// </summary>
public static class GeometryConstants
{
    /// <summary>General-purpose floating-point comparison epsilon (1e-9).</summary>
    public const double DefaultEpsilon = 1e-9;

    /// <summary>Epsilon for angular comparisons in radians (1e-7).</summary>
    public const double AngularEpsilon = 1e-7;

    /// <summary>Epsilon for physical dimension comparisons in mm (1e-3).</summary>
    public const double DimensionEpsilon = 1e-3;
}
