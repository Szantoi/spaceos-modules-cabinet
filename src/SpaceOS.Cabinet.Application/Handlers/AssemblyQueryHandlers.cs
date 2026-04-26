namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Application.Queries;
using SpaceOS.Cabinet.Assembly;

/// <summary>Handles <see cref="GetAssemblyDocumentationQuery"/>.</summary>
public sealed class GetAssemblyDocumentationQueryHandler
    : IRequestHandler<GetAssemblyDocumentationQuery, Result<IReadOnlyList<AssemblyStepDto>>>
{
    private readonly ISkeletonRepository _skeletonRepo;
    private readonly AssemblyDocumentationService _docService;

    /// <summary>Initializes the handler with a skeleton repository and assembly documentation service.</summary>
    public GetAssemblyDocumentationQueryHandler(
        ISkeletonRepository skeletonRepo,
        AssemblyDocumentationService docService)
    {
        _skeletonRepo = skeletonRepo;
        _docService = docService;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<AssemblyStepDto>>> Handle(
        GetAssemblyDocumentationQuery request, CancellationToken ct)
    {
        var skeleton = await _skeletonRepo.GetByIdAsync(request.SkeletonId, ct).ConfigureAwait(false);
        if (skeleton is null)
            return Result<IReadOnlyList<AssemblyStepDto>>.Error($"Skeleton {request.SkeletonId} not found.");

        var stepsResult = _docService.GenerateAssemblySteps(skeleton);
        if (!stepsResult.IsSuccess)
            return Result<IReadOnlyList<AssemblyStepDto>>.Error(string.Join("; ", stepsResult.Errors));

        var dtos = stepsResult.Value
            .Select(s => new AssemblyStepDto(
                s.Order,
                s.Title,
                s.SanitizedInstruction,
                s.PrimaryPartId,
                s.RequiredConnectionIds,
                s.RequiredTools,
                s.RequiredSkillLevel))
            .ToList();

        return Result<IReadOnlyList<AssemblyStepDto>>.Success(dtos);
    }
}

/// <summary>Handles <see cref="GetExplodedViewQuery"/>.</summary>
public sealed class GetExplodedViewQueryHandler
    : IRequestHandler<GetExplodedViewQuery, Result<ExplodedViewDto>>
{
    private readonly ISkeletonRepository _skeletonRepo;
    private readonly AssemblyDocumentationService _docService;

    /// <summary>Initializes the handler with a skeleton repository and assembly documentation service.</summary>
    public GetExplodedViewQueryHandler(
        ISkeletonRepository skeletonRepo,
        AssemblyDocumentationService docService)
    {
        _skeletonRepo = skeletonRepo;
        _docService = docService;
    }

    /// <inheritdoc/>
    public async Task<Result<ExplodedViewDto>> Handle(GetExplodedViewQuery request, CancellationToken ct)
    {
        var skeleton = await _skeletonRepo.GetByIdAsync(request.SkeletonId, ct).ConfigureAwait(false);
        if (skeleton is null)
            return Result<ExplodedViewDto>.Error($"Skeleton {request.SkeletonId} not found.");

        var exploded = _docService.GenerateExplodedView(skeleton);
        var dto = new ExplodedViewDto(
            exploded.Layers
                .Select(l => new ExplodedViewLayerDto(l.LayerIndex, l.PartIds))
                .ToList());

        return Result<ExplodedViewDto>.Success(dto);
    }
}
