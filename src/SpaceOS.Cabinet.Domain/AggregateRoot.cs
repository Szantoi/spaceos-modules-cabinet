namespace SpaceOS.Cabinet.Domain;

using SpaceOS.Cabinet.Domain.Events;

/// <summary>
/// Base class for aggregate roots that collect and buffer domain events.
/// Subclasses call <see cref="RaiseEvent"/> to append events and <see cref="NextSeq"/>
/// to obtain a monotonically increasing sequence number within the aggregate lifetime.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private long _nextSequence;

    /// <summary>Pending domain events (not yet flushed via <see cref="PopDomainEvents"/>).</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Returns all buffered events ordered by sequence number and clears the internal buffer.
    /// Call this after persisting the aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var result = _domainEvents.OrderBy(e => e.SequenceNumber).ToList().AsReadOnly();
        _domainEvents.Clear();
        return result;
    }

    /// <summary>Appends a domain event to the internal buffer.</summary>
    /// <param name="evt">The event to buffer.</param>
    protected void RaiseEvent(IDomainEvent evt) => _domainEvents.Add(evt);

    /// <summary>Returns the next monotonically increasing sequence number for this aggregate.</summary>
    protected long NextSeq() => ++_nextSequence;
}
