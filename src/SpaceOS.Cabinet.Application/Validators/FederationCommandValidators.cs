namespace SpaceOS.Cabinet.Application.Validators;

using System.Text;
using FluentValidation;
using SpaceOS.Cabinet.Application.Commands;
using SpaceOS.Cabinet.Catalog;

/// <summary>Validates <see cref="SubmitCommunityCatalogEntryCommand"/>.</summary>
public sealed class SubmitCommunityCatalogEntryCommandValidator
    : AbstractValidator<SubmitCommunityCatalogEntryCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public SubmitCommunityCatalogEntryCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(CatalogEntry.MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(CatalogEntry.MaxDescriptionLength);
        RuleFor(x => x.PayloadJson)
            .NotEmpty()
            .Must(json => Encoding.UTF8.GetByteCount(json) <= CatalogEntry.MaxPayloadSizeBytes)
            .WithMessage($"PayloadJson must not exceed {CatalogEntry.MaxPayloadSizeBytes} bytes.");
        RuleFor(x => x.PayloadSchemaVersion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Visibility)
            .Must(v => v != CatalogVisibility.Curated)
            .WithMessage("Community entries cannot have Curated visibility.");
    }
}

/// <summary>Validates <see cref="CreateTenantStandardCommand"/>.</summary>
public sealed class CreateTenantStandardCommandValidator : AbstractValidator<CreateTenantStandardCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateTenantStandardCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.CarcassMaterial).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BackPanelMaterial).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CarcassThicknessMm).GreaterThan(0);
        RuleFor(x => x.BackPanelThicknessMm).GreaterThan(0);
        RuleFor(x => x.DiameterMm).GreaterThan(0).When(x => x.LineBoreEnabled);
        RuleFor(x => x.SpacingMm).GreaterThan(0).When(x => x.LineBoreEnabled);
        RuleFor(x => x.TallCabinetHeightMm).GreaterThan(0);
        RuleFor(x => x.LongShelfMm).GreaterThan(0);
    }
}

/// <summary>Validates <see cref="RateCatalogEntryCommand"/>.</summary>
public sealed class RateCatalogEntryCommandValidator : AbstractValidator<RateCatalogEntryCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public RateCatalogEntryCommandValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.RaterTenantId).NotEmpty();
        RuleFor(x => x.EntryOwnerTenantId).NotEmpty();
        RuleFor(x => x.RaterUserId).NotEmpty();
        RuleFor(x => x.Stars).InclusiveBetween(1, 5);
        RuleFor(x => x)
            .Must(x => x.RaterTenantId != x.EntryOwnerTenantId)
            .WithName("RaterTenantId")
            .WithMessage("Self-rating is not allowed.");
    }
}

/// <summary>Validates <see cref="FlagCatalogEntryCommand"/>.</summary>
public sealed class FlagCatalogEntryCommandValidator : AbstractValidator<FlagCatalogEntryCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public FlagCatalogEntryCommandValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.ReporterTenantId).NotEmpty();
        RuleFor(x => x.EntryOwnerTenantId).NotEmpty();
        RuleFor(x => x.ReporterUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.ReporterTenantId != x.EntryOwnerTenantId)
            .WithName("ReporterTenantId")
            .WithMessage("Self-flagging is not allowed.");
    }
}
