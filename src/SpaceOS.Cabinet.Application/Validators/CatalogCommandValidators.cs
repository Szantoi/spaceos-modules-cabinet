namespace SpaceOS.Cabinet.Application.Validators;

using System.Text;
using FluentValidation;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Catalog;

/// <summary>Validates <see cref="CreateCatalogEntryCommand"/> fields (BE-CAB02-7).</summary>
public sealed class CreateCatalogEntryCommandValidator : AbstractValidator<CreateCatalogEntryCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateCatalogEntryCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CatalogEntry.MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(CatalogEntry.MaxDescriptionLength);
        RuleFor(x => x.PayloadJson)
            .NotEmpty()
            .Must(json => Encoding.UTF8.GetByteCount(json) <= CatalogEntry.MaxPayloadSizeBytes)
            .WithMessage($"PayloadJson must not exceed {CatalogEntry.MaxPayloadSizeBytes} bytes.");
        RuleFor(x => x.PayloadSchemaVersion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

/// <summary>Validates <see cref="SubmitCatalogEntryCommand"/> fields.</summary>
public sealed class SubmitCatalogEntryCommandValidator : AbstractValidator<SubmitCatalogEntryCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public SubmitCatalogEntryCommandValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
