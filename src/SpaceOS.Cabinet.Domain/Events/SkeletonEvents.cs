using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Domain.Events;

/// <summary>Raised when a new Skeleton aggregate is created.</summary>
public sealed record SkeletonCreated(
    Guid SkeletonId,
    Guid TenantId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when a Part is added to the Skeleton.</summary>
public sealed record PartAdded(
    Guid SkeletonId,
    Guid PartId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when a Part is removed from the Skeleton.</summary>
public sealed record PartRemoved(
    Guid SkeletonId,
    Guid PartId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when a Connection between two Parts is added.</summary>
public sealed record ConnectionAdded(
    Guid SkeletonId,
    Guid ConnectionId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when a Connection is removed from the Skeleton.</summary>
public sealed record ConnectionRemoved(
    Guid SkeletonId,
    Guid ConnectionId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when the assembly dimensions are changed.</summary>
public sealed record SkeletonResized(
    Guid SkeletonId,
    AssemblyDimension OldDim,
    AssemblyDimension NewDim,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;

/// <summary>Raised when an assembly ordering is derived from catalog pins (A14).</summary>
public sealed record AssemblyDerived(
    Guid SkeletonId,
    DateTime OccurredAt,
    long SequenceNumber) : IDomainEvent;
