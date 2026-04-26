namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Handles <see cref="PinCatalogEntryCommand"/>: pins a catalog entry to a part in a skeleton.
/// </summary>
public sealed class PinCatalogEntryCommandHandler
    : IRequestHandler<PinCatalogEntryCommand, Result>
{
    private readonly ISkeletonRepository _repo;

    /// <summary>Initializes the handler with a skeleton repository.</summary>
    public PinCatalogEntryCommandHandler(ISkeletonRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(PinCatalogEntryCommand request, CancellationToken ct)
    {
        var skeleton = await _repo.GetByIdAsync(request.SkeletonId, ct).ConfigureAwait(false);
        if (skeleton is null)
            return Result.Error($"Skeleton {request.SkeletonId} not found.");

        var result = skeleton.PinCatalogEntry(request.PartId, request.CatalogType, request.CatalogEntryId);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(skeleton, ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="DeriveAssemblyCommand"/>: derives assembly ordering for a skeleton.
/// </summary>
public sealed class DeriveAssemblyCommandHandler
    : IRequestHandler<DeriveAssemblyCommand, Result>
{
    private readonly ISkeletonRepository _repo;

    /// <summary>Initializes the handler with a skeleton repository.</summary>
    public DeriveAssemblyCommandHandler(ISkeletonRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(DeriveAssemblyCommand request, CancellationToken ct)
    {
        var skeleton = await _repo.GetByIdAsync(request.SkeletonId, ct).ConfigureAwait(false);
        if (skeleton is null)
            return Result.Error($"Skeleton {request.SkeletonId} not found.");

        var result = skeleton.DeriveAssembly(NullCatalogResolver.Instance);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(skeleton, ct).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="DeriveBillOfServicesCommand"/>: derives a bill of services from pinned catalog entries.
/// </summary>
public sealed class DeriveBillOfServicesCommandHandler
    : IRequestHandler<DeriveBillOfServicesCommand, Result<BillOfServicesDto>>
{
    private readonly ISkeletonRepository _repo;

    /// <summary>Initializes the handler with a skeleton repository.</summary>
    public DeriveBillOfServicesCommandHandler(ISkeletonRepository repo)
    {
        _repo = repo;
    }

    /// <inheritdoc/>
    public async Task<Result<BillOfServicesDto>> Handle(
        DeriveBillOfServicesCommand request, CancellationToken ct)
    {
        var skeleton = await _repo.GetByIdAsync(request.SkeletonId, ct).ConfigureAwait(false);
        if (skeleton is null)
            return Result<BillOfServicesDto>.Error($"Skeleton {request.SkeletonId} not found.");

        var bosResult = skeleton.DeriveBillOfServices();
        if (!bosResult.IsSuccess)
            return Result<BillOfServicesDto>.Error(string.Join("; ", bosResult.Errors));

        var bos = bosResult.Value;
        var dto = new BillOfServicesDto(
            bos.SkeletonId,
            bos.Items
                .Select(i => new BillOfServicesItemDto(i.PartId, i.CatalogType, i.CatalogEntryId))
                .ToList());

        return Result<BillOfServicesDto>.Success(dto);
    }
}
