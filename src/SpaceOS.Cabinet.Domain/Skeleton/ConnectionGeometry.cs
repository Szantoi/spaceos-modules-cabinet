using SpaceOS.Cabinet.Abstractions;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Describes the geometric relationship between parent and child parts in a connection.
/// Specifies which face of the parent and which edge of the child are joined,
/// plus an optional offset along the joint.
/// </summary>
/// <param name="ParentFace">The face on the parent part where the child is attached.</param>
/// <param name="ChildEdge">The edge on the child part that contacts the parent face.</param>
/// <param name="EdgeOffset">Offset in mm along the parent face from the datum edge to the child's position.</param>
public sealed record ConnectionGeometry(PartFace ParentFace, PartEdge ChildEdge, double EdgeOffset);
