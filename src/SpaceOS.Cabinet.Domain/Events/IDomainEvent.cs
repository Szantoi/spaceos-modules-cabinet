namespace SpaceOS.Cabinet.Domain.Events;

/// <summary>
/// Marker interface for all Cabinet domain events.
/// Events are immutable records emitted by the <c>Skeleton</c> aggregate root.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTime OccurredAt { get; }

    /// <summary>Monotonically increasing sequence number within the owning aggregate.</summary>
    long SequenceNumber { get; }
}
