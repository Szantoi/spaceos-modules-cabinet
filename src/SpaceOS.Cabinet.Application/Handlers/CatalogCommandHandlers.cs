namespace SpaceOS.Cabinet.Application.Handlers;

using Ardalis.Result;
using MediatR;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Catalog;

/// <summary>
/// Handles <see cref="CreateCatalogEntryCommand"/>: creates a Draft catalog entry and persists it.
/// </summary>
public sealed class CreateCatalogEntryCommandHandler
    : IRequestHandler<CreateCatalogEntryCommand, Result<Guid>>
{
    private readonly ICatalogEntryRepository _repo;
    private readonly ICatalogPayloadValidator _validator;

    /// <summary>Initializes the handler with a repository and payload validator.</summary>
    public CreateCatalogEntryCommandHandler(
        ICatalogEntryRepository repo,
        ICatalogPayloadValidator validator)
    {
        _repo = repo;
        _validator = validator;
    }

    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(
        CreateCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var result = CatalogEntry.CreateDraft(
            request.TenantId,
            request.ActorUserId,
            request.Type,
            request.Name,
            request.Description,
            request.Visibility,
            request.PayloadJson,
            request.PayloadSchemaVersion,
            _validator);

        if (!result.IsSuccess)
            return Result<Guid>.Error(string.Join("; ", result.Errors));

        await _repo.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Success(result.Value.Id);
    }
}

/// <summary>
/// Handles <see cref="SubmitCatalogEntryCommand"/>: transitions a Draft entry to Submitted.
/// </summary>
public sealed class SubmitCatalogEntryCommandHandler
    : IRequestHandler<SubmitCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;
    private readonly ICatalogPayloadValidator _validator;

    /// <summary>Initializes the handler with a repository and payload validator.</summary>
    public SubmitCatalogEntryCommandHandler(
        ICatalogEntryRepository repo,
        ICatalogPayloadValidator validator)
    {
        _repo = repo;
        _validator = validator;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        SubmitCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var result = entry.Submit(request.ActorUserId, _validator);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="ApproveCatalogEntryCommand"/>: transitions a Submitted entry to Approved (staff action).
/// </summary>
public sealed class ApproveCatalogEntryCommandHandler
    : IRequestHandler<ApproveCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;
    private readonly IStaffAuditLogger _auditLogger;

    /// <summary>Initializes the handler with a repository and audit logger.</summary>
    public ApproveCatalogEntryCommandHandler(
        ICatalogEntryRepository repo,
        IStaffAuditLogger auditLogger)
    {
        _repo = repo;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        ApproveCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var result = entry.Approve(request.StaffUserId);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _auditLogger.LogAsync(request.StaffUserId, "Approve", request.EntryId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="RejectCatalogEntryCommand"/>: transitions a Submitted entry to Rejected (staff action).
/// </summary>
public sealed class RejectCatalogEntryCommandHandler
    : IRequestHandler<RejectCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;
    private readonly IStaffAuditLogger _auditLogger;

    /// <summary>Initializes the handler with a repository and audit logger.</summary>
    public RejectCatalogEntryCommandHandler(
        ICatalogEntryRepository repo,
        IStaffAuditLogger auditLogger)
    {
        _repo = repo;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        RejectCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var result = entry.Reject(request.StaffUserId, request.Reason);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _auditLogger.LogAsync(request.StaffUserId, "Reject", request.EntryId, request.Reason,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="PublishCatalogEntryCommand"/>: transitions an Approved entry to Published (staff action).
/// </summary>
public sealed class PublishCatalogEntryCommandHandler
    : IRequestHandler<PublishCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;
    private readonly IStaffAuditLogger _auditLogger;

    /// <summary>Initializes the handler with a repository and audit logger.</summary>
    public PublishCatalogEntryCommandHandler(
        ICatalogEntryRepository repo,
        IStaffAuditLogger auditLogger)
    {
        _repo = repo;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        PublishCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var result = entry.Publish(request.StaffUserId);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _auditLogger.LogAsync(request.StaffUserId, "Publish", request.EntryId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}

/// <summary>
/// Handles <see cref="DeprecateCatalogEntryCommand"/>: transitions a Published entry to Deprecated (staff action).
/// </summary>
public sealed class DeprecateCatalogEntryCommandHandler
    : IRequestHandler<DeprecateCatalogEntryCommand, Result>
{
    private readonly ICatalogEntryRepository _repo;
    private readonly IStaffAuditLogger _auditLogger;

    /// <summary>Initializes the handler with a repository and audit logger.</summary>
    public DeprecateCatalogEntryCommandHandler(
        ICatalogEntryRepository repo,
        IStaffAuditLogger auditLogger)
    {
        _repo = repo;
        _auditLogger = auditLogger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        DeprecateCatalogEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repo.GetByIdAsync(request.EntryId, cancellationToken).ConfigureAwait(false);
        if (entry is null)
            return Result.Error($"CatalogEntry {request.EntryId} not found.");

        var result = entry.Deprecate(request.StaffUserId);
        if (!result.IsSuccess)
            return result;

        await _repo.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await _auditLogger.LogAsync(request.StaffUserId, "Deprecate", request.EntryId,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
