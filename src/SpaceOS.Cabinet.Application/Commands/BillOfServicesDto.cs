namespace SpaceOS.Cabinet.Application.Commands;

using SpaceOS.Cabinet.Abstractions;

/// <summary>A single line item in a bill of services DTO.</summary>
/// <param name="PartId">Part this service applies to.</param>
/// <param name="CatalogType">Catalog type slot that was pinned.</param>
/// <param name="CatalogEntryId">The pinned catalog entry identifier.</param>
public sealed record BillOfServicesItemDto(Guid PartId, CatalogType CatalogType, Guid CatalogEntryId);

/// <summary>DTO representing a derived bill of services for a skeleton.</summary>
/// <param name="SkeletonId">The skeleton this bill belongs to.</param>
/// <param name="Items">All service line items derived from pinned catalog entries.</param>
public sealed record BillOfServicesDto(Guid SkeletonId, IReadOnlyList<BillOfServicesItemDto> Items);
