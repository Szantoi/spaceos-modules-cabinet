namespace SpaceOS.Cabinet.Application.Queries;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;

/// <summary>Returns the <see cref="TenantStandardSnapshot"/> for the given tenant, or <c>null</c> if not configured.</summary>
public sealed record GetTenantStandardQuery(Guid TenantId) : IRequest<Result<TenantStandardSnapshot?>>;

/// <summary>Returns all <see cref="TenantStandardSnapshot"/> records for the given tenant.</summary>
public sealed record ListTenantStandardsQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<TenantStandardSnapshot>>>;

/// <summary>
/// Lists community <see cref="CatalogEntry"/> instances visible to the requesting tenant.
/// Filters by <see cref="CatalogVisibility.Shared"/> or <see cref="CatalogVisibility.Community"/>.
/// </summary>
public sealed record ListCommunityEntriesQuery(
    Guid TenantId,
    CatalogType? Type,
    Guid? ClusterId,
    bool OnlyVisible) : IRequest<Result<IReadOnlyList<CatalogEntry>>>;

/// <summary>
/// Returns catalog entries that have at least one active flag, ordered by flag count descending.
/// Used by moderation staff to process the review queue.
/// </summary>
public sealed record GetModerationQueueQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<CatalogEntry>>>;

/// <summary>
/// Returns a <see cref="CatalogEntry"/> together with its current rating rollup.
/// </summary>
public sealed record GetCatalogEntryWithRatingsQuery(Guid EntryId)
    : IRequest<Result<CatalogEntryWithRatingsDto>>;
