namespace SpaceOS.Cabinet.Geometry;

/// <summary>
/// Provides the canonical gravity direction used throughout cabinet geometry calculations.
/// </summary>
public static class GravityVector
{
    /// <summary>
    /// Gravity direction in world space: (0, 0, -1) — pointing in the negative Z direction.
    /// </summary>
    public static readonly Vector3 Default = Vector3.Create(0, 0, -1).Value;
}
