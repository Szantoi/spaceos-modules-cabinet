using SpaceOS.Cabinet.Abstractions;

namespace SpaceOS.Cabinet.Machining;

/// <summary>
/// Discriminated union describing what geometry a machining operation targets.
/// </summary>
public abstract record MachiningSubject;

/// <summary>A specific face of a part.</summary>
/// <param name="PartId">ID of the part being machined.</param>
/// <param name="Face">Which face the operation applies to.</param>
public sealed record PlaneSubject(Guid PartId, PartFace Face) : MachiningSubject;

/// <summary>A specific edge of a part.</summary>
/// <param name="PartId">ID of the part being machined.</param>
/// <param name="Edge">Which edge the operation applies to.</param>
public sealed record EdgeSubject(Guid PartId, PartEdge Edge) : MachiningSubject;

/// <summary>A joinery connection between two parts.</summary>
/// <param name="ConnectionId">ID of the connection being machined.</param>
public sealed record ConnectionSubject(Guid ConnectionId) : MachiningSubject;
