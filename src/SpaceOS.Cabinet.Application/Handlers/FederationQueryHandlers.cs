namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Catalog;

/// <summary>
/// Handles <see cref="GetTenantStandardQuery"/>: returns the read-side snapshot for the given tenant.
/// </summary>
public sealed class GetTenantStandardQueryHandler
    : IRequestHandler<GetTenantStandardQuery, Result<TenantStandardSnapshot?>>
{
    private readonly ITenantStandardRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public GetTenantStandardQueryHandler(ITenantStandardRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<TenantStandardSnapshot?>> Handle(
        GetTenantStandardQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await _repo.GetByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return Result<TenantStandardSnapshot?>.Success(snapshot);
    }
}

/// <summary>
/// Handles <see cref="ListTenantStandardsQuery"/>: returns all read-side snapshots for the given tenant.
/// </summary>
public sealed class ListTenantStandardsQueryHandler
    : IRequestHandler<ListTenantStandardsQuery, Result<IReadOnlyList<TenantStandardSnapshot>>>
{
    private readonly ITenantStandardRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public ListTenantStandardsQueryHandler(ITenantStandardRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<TenantStandardSnapshot>>> Handle(
        ListTenantStandardsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.ListByTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<TenantStandardSnapshot>>.Success(list);
    }
}

/// <summary>
/// Handles <see cref="ListCommunityEntriesQuery"/>: returns Shared/Community entries visible to the caller.
/// </summary>
public sealed class ListCommunityEntriesQueryHandler
    : IRequestHandler<ListCommunityEntriesQuery, Result<IReadOnlyList<CatalogEntry>>>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public ListCommunityEntriesQueryHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CatalogEntry>>> Handle(
        ListCommunityEntriesQuery request, CancellationToken cancellationToken)
    {
        // Load all entries for the tenant; the repository handles tenantId + optional type filter
        var all = await _repo.ListAsync(request.TenantId, request.Type, null, cancellationToken)
            .ConfigureAwait(false);

        var query = all.Where(e =>
            e.Visibility == CatalogVisibility.Shared || e.Visibility == CatalogVisibility.Community);

        if (request.ClusterId.HasValue)
            query = query.Where(e => e.ClusterId == request.ClusterId.Value);

        if (request.OnlyVisible)
            query = query.Where(e => !e.IsAutoHidden);

        return Result<IReadOnlyList<CatalogEntry>>.Success(query.ToList());
    }
}

/// <summary>
/// Handles <see cref="GetModerationQueueQuery"/>: returns entries with at least one active flag.
/// </summary>
public sealed class GetModerationQueueQueryHandler
    : IRequestHandler<GetModerationQueueQuery, Result<IReadOnlyList<CatalogEntry>>>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public GetModerationQueueQueryHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CatalogEntry>>> Handle(
        GetModerationQueueQuery request, CancellationToken cancellationToken)
    {
        var all = await _repo.ListAsync(request.TenantId, null, null, cancellationToken)
            .ConfigureAwait(false);

        var flagged = all
            .Where(e => e.ActiveFlagCount > 0)
            .OrderByDescending(e => e.ActiveFlagCount)
            .ToList();

        return Result<IReadOnlyList<CatalogEntry>>.Success(flagged);
    }
}
