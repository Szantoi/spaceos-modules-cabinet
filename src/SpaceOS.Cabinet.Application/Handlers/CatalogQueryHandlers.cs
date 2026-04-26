namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Catalog;

/// <summary>Handles <see cref="GetCatalogEntryQuery"/>.</summary>
public sealed class GetCatalogEntryQueryHandler
    : IRequestHandler<GetCatalogEntryQuery, Result<CatalogEntry>>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public GetCatalogEntryQueryHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<CatalogEntry>> Handle(GetCatalogEntryQuery request, CancellationToken ct)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, ct).ConfigureAwait(false);
        return entry is not null
            ? Result<CatalogEntry>.Success(entry)
            : Result<CatalogEntry>.Error($"CatalogEntry {request.EntryId} not found.");
    }
}

/// <summary>Handles <see cref="ListCatalogEntriesQuery"/>.</summary>
public sealed class ListCatalogEntriesQueryHandler
    : IRequestHandler<ListCatalogEntriesQuery, Result<IReadOnlyList<CatalogEntry>>>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public ListCatalogEntriesQueryHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<CatalogEntry>>> Handle(
        ListCatalogEntriesQuery request, CancellationToken ct)
    {
        var entries = await _repo.ListAsync(request.TenantId, request.Type, request.State, ct)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<CatalogEntry>>.Success(entries);
    }
}
