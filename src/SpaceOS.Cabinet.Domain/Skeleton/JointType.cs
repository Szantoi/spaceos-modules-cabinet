namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Describes the joinery method used to connect two parts.
/// A5: the default for all new connections is <see cref="FaceEdgeButt"/>.
/// </summary>
public enum JointType
{
    /// <summary>A face-to-edge butt joint — the default (A5).</summary>
    FaceEdgeButt,

    /// <summary>Dado (flat-bottomed groove cut across the grain).</summary>
    Dado,

    /// <summary>Groove (flat-bottomed groove cut with the grain).</summary>
    Groove,

    /// <summary>Rabbet (stepped recess along an edge).</summary>
    Rabbet,

    /// <summary>Miter joint (45-degree cut on both parts).</summary>
    Miter,

    /// <summary>Tongue-and-groove joint.</summary>
    TongueGroove,

    /// <summary>Pocket screw joint.</summary>
    Pocket,

    /// <summary>Dowel joint.</summary>
    Dowel,

    /// <summary>Mitered joint (variant).</summary>
    Mitered,

    /// <summary>Offset joint.</summary>
    Offset
}
