namespace SpaceOS.Cabinet.Application.Commands;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Catalog;

/// <summary>Rates a <see cref="CatalogEntry"/> owned by another tenant.</summary>
public sealed record RateCatalogEntryCommand(
    Guid EntryId,
    Guid EntryOwnerTenantId,
    Guid RaterTenantId,
    Guid RaterUserId,
    int Stars,
    string? Comment) : IRequest<Result>;

/// <summary>Flags a <see cref="CatalogEntry"/> owned by another tenant for moderation review.</summary>
public sealed record FlagCatalogEntryCommand(
    Guid EntryId,
    Guid EntryOwnerTenantId,
    Guid ReporterTenantId,
    Guid ReporterUserId,
    FlagReason Reason,
    string? Note) : IRequest<Result>;

/// <summary>Admin time-bounded acknowledgment: suppresses <c>IsAutoHidden</c> for the specified window.</summary>
public sealed record ClearFlagsByAdminCommand(
    Guid EntryId,
    Guid AdminUserId,
    int? AckDays) : IRequest<Result>;

/// <summary>
/// Server-side assignment of the pre-computed similarity fingerprint and optional cluster reference.
/// Must never be triggered from a client-supplied payload (SEC-02).
/// </summary>
public sealed record AssignFingerprintCommand(
    Guid EntryId,
    Guid ActorUserId,
    string? Fingerprint,
    Guid? ClusterId) : IRequest<Result>;

/// <summary>
/// Triggers re-computation of cluster canonical members for all entries of the given tenant.
/// Currently a no-op stub — cluster recomputation is performed asynchronously by a background service.
/// </summary>
public sealed record RecomputeClustersCommand(
    Guid TenantId,
    Guid ActorUserId) : IRequest<Result>;
