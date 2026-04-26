namespace SpaceOS.Cabinet.Catalog.Events;

/// <summary>Marker interface for all catalog domain events.</summary>
public interface ICatalogDomainEvent
{
    /// <summary>The affected catalog entry.</summary>
    Guid CatalogEntryId { get; }

    /// <summary>The user who triggered the transition.</summary>
    Guid ActorUserId { get; }

    /// <summary>UTC instant the event occurred.</summary>
    DateTimeOffset OccurredAt { get; }
}

/// <summary>Raised when a new <see cref="CatalogEntry"/> is created in Draft state.</summary>
public sealed record CatalogEntryCreated(
    Guid CatalogEntryId,
    Guid TenantId,
    CatalogType Type,
    Guid ActorUserId,
    DateTimeOffset OccurredAt) : ICatalogDomainEvent;

/// <summary>Raised when a Draft entry is submitted for approval.</summary>
public sealed record CatalogEntrySubmitted(
    Guid CatalogEntryId,
    string ContentHash,
    Guid ActorUserId,
    DateTimeOffset OccurredAt) : ICatalogDomainEvent;

/// <summary>Raised when a Submitted entry is approved.</summary>
public sealed record CatalogEntryApproved(
    Guid CatalogEntryId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt) : ICatalogDomainEvent;

/// <summary>Raised when a Submitted entry is rejected.</summary>
public sealed record CatalogEntryRejected(
    Guid CatalogEntryId,
    string Reason,
    Guid ActorUserId,
    DateTimeOffset OccurredAt) : ICatalogDomainEvent;

/// <summary>Raised when an Approved entry is published and becomes resolvable.</summary>
public sealed record CatalogEntryPublished(
    Guid CatalogEntryId,
    string ContentHash,
    Guid ActorUserId,
    DateTimeOffset OccurredAt) : ICatalogDomainEvent;

/// <summary>Raised when a Published entry is deprecated.</summary>
public sealed record CatalogEntryDeprecated(
    Guid CatalogEntryId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt) : ICatalogDomainEvent;
