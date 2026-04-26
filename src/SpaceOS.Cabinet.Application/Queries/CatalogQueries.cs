namespace SpaceOS.Cabinet.Application.Queries;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;

/// <summary>Returns a single <see cref="CatalogEntry"/> by ID.</summary>
public sealed record GetCatalogEntryQuery(Guid EntryId) : IRequest<Result<CatalogEntry>>;

/// <summary>Lists catalog entries for a tenant, optionally filtered by type and/or state.</summary>
public sealed record ListCatalogEntriesQuery(
    Guid TenantId,
    CatalogType? Type = null,
    CatalogLifecycleState? State = null) : IRequest<Result<IReadOnlyList<CatalogEntry>>>;

/// <summary>Returns ordered assembly steps for a skeleton.</summary>
public sealed record GetAssemblyDocumentationQuery(Guid SkeletonId)
    : IRequest<Result<IReadOnlyList<AssemblyStepDto>>>;

/// <summary>Returns an exploded-view representation of a skeleton.</summary>
public sealed record GetExplodedViewQuery(Guid SkeletonId) : IRequest<Result<ExplodedViewDto>>;
