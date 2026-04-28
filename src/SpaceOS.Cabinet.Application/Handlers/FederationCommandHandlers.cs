namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Catalog;

/// <summary>
/// Handles <see cref="RateCatalogEntryCommand"/>: creates or updates a rating and updates the aggregate's rollup.
/// </summary>
public sealed class RateCatalogEntryCommandHandler
    : IRequestHandler<RateCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _entryRepo;
    private readonly IRatingRepository _ratingRepo;

    /// <summary>Initializes the handler.</summary>
    public RateCatalogEntryCommandHandler(
        ICatalogEntryRepository entryRepo,
        IRatingRepository ratingRepo)
    {
        _entryRepo = entryRepo;
        _ratingRepo = ratingRepo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        RateCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _entryRepo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        // Check for an existing rating (re-rating path)
        int? oldStars = null;
        var existing = _ratingRepo.GetByEntryAndTenant(request.EntryId, request.RaterTenantId);
        if (existing.IsSuccess)
            oldStars = existing.Value.Stars;

        var ratingResult = CatalogEntryRating.Create(
            request.EntryId,
            request.RaterTenantId,
            request.RaterUserId,
            request.Stars,
            request.Comment,
            request.EntryOwnerTenantId);

        if (!ratingResult.IsSuccess)
            return Result.Invalid(ratingResult.ValidationErrors.First());

        var ingestResult = entry.IngestRating(ratingResult.Value, oldStars);
        if (!ingestResult.IsSuccess)
            return ingestResult;

        await _entryRepo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="FlagCatalogEntryCommand"/>: creates a flag and increments the entry's active flag count.
/// </summary>
public sealed class FlagCatalogEntryCommandHandler
    : IRequestHandler<FlagCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public FlagCatalogEntryCommandHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        FlagCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var flagResult = CatalogEntryFlag.Create(
            request.EntryId,
            request.ReporterTenantId,
            request.ReporterUserId,
            request.Reason,
            request.Note,
            request.EntryOwnerTenantId);

        if (!flagResult.IsSuccess)
            return Result.Invalid(flagResult.ValidationErrors.First());

        var ingestResult = entry.IngestFlag(flagResult.Value);
        if (!ingestResult.IsSuccess)
            return ingestResult;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="ClearFlagsByAdminCommand"/>: applies a time-bounded admin acknowledgment window.
/// </summary>
public sealed class ClearFlagsByAdminCommandHandler
    : IRequestHandler<ClearFlagsByAdminCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public ClearFlagsByAdminCommandHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        ClearFlagsByAdminCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var ackDuration = request.AckDays.HasValue
            ? TimeSpan.FromDays(request.AckDays.Value)
            : (TimeSpan?)null;

        var result = entry.ClearFlagsByAdmin(request.AdminUserId, ackDuration);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="AssignFingerprintCommand"/>: assigns the server-computed fingerprint and cluster reference.
/// </summary>
public sealed class AssignFingerprintCommandHandler
    : IRequestHandler<AssignFingerprintCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;

    /// <summary>Initializes the handler.</summary>
    public AssignFingerprintCommandHandler(ICatalogEntryRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        AssignFingerprintCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var result = entry.AssignFingerprintAndCluster(request.Fingerprint, request.ClusterId);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="RecomputeClustersCommand"/>: stub — validates the tenant identifier and returns success.
/// Full cluster recomputation is performed asynchronously by a background service.
/// </summary>
public sealed class RecomputeClustersCommandHandler
    : IRequestHandler<RecomputeClustersCommand, Result>
{
    /// <inheritdoc/>
    public Task<Result> Handle(
        RecomputeClustersCommand request, CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty)
            return Task.FromResult(Result.Invalid(new ValidationError("TenantId required.")));

        return Task.FromResult(Result.Success());
    }
}
