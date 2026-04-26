namespace SpaceOS.Cabinet.Domain.Skeleton;

using SpaceOS.Cabinet.Abstractions;

/// <summary>A single line in a bill of services, linking a part to a resolved catalog entry.</summary>
/// <param name="PartId">The part this entry applies to.</param>
/// <param name="CatalogType">The catalog type slot that was pinned.</param>
/// <param name="CatalogEntryId">The pinned catalog entry identifier.</param>
public sealed record BillOfServicesItem(Guid PartId, CatalogType CatalogType, Guid CatalogEntryId);

/// <summary>
/// A derived list of catalog services required to manufacture a skeleton (A13 extension point).
/// Derived from pinned catalog entries on the skeleton.
/// </summary>
/// <param name="SkeletonId">The skeleton this bill belongs to.</param>
/// <param name="Items">All service line items derived from pinned catalog entries.</param>
public sealed record BillOfServices(Guid SkeletonId, IReadOnlyList<BillOfServicesItem> Items);
