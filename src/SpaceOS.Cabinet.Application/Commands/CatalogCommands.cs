namespace SpaceOS.Cabinet.Application.Commands;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;

/// <summary>Creates a new <see cref="CatalogEntry"/> in Draft state.</summary>
public sealed record CreateCatalogEntryCommand(
    Guid TenantId,
    CatalogType Type,
    string Name,
    string Description,
    CatalogVisibility Visibility,
    string PayloadJson,
    string PayloadSchemaVersion,
    Guid ActorUserId) : IRequest<Result<Guid>>;

/// <summary>Submits a Draft catalog entry for review.</summary>
public sealed record SubmitCatalogEntryCommand(
    Guid EntryId,
    Guid ActorUserId) : IRequest<Result>;

/// <summary>Approves a Submitted catalog entry (staff action).</summary>
public sealed record ApproveCatalogEntryCommand(
    Guid EntryId,
    Guid StaffUserId) : IRequest<Result>;

/// <summary>Rejects a Submitted catalog entry (staff action).</summary>
public sealed record RejectCatalogEntryCommand(
    Guid EntryId,
    Guid StaffUserId,
    string Reason) : IRequest<Result>;

/// <summary>Publishes an Approved catalog entry (staff action).</summary>
public sealed record PublishCatalogEntryCommand(
    Guid EntryId,
    Guid StaffUserId) : IRequest<Result>;

/// <summary>Deprecates a Published catalog entry (staff action).</summary>
public sealed record DeprecateCatalogEntryCommand(
    Guid EntryId,
    Guid StaffUserId) : IRequest<Result>;

/// <summary>Pins a catalog entry to a specific part + type slot in a skeleton.</summary>
public sealed record PinCatalogEntryCommand(
    Guid SkeletonId,
    Guid PartId,
    CatalogType CatalogType,
    Guid CatalogEntryId,
    Guid ActorUserId) : IRequest<Result>;

/// <summary>Derives assembly ordering for all parts in a skeleton.</summary>
public sealed record DeriveAssemblyCommand(
    Guid SkeletonId,
    Guid ActorUserId) : IRequest<Result>;

/// <summary>Derives a bill of services from pinned catalog entries in a skeleton.</summary>
public sealed record DeriveBillOfServicesCommand(
    Guid SkeletonId,
    Guid ActorUserId) : IRequest<Result<BillOfServicesDto>>;
