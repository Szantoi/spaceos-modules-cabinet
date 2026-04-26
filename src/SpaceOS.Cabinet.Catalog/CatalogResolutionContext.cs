namespace SpaceOS.Cabinet.Catalog;

/// <summary>
/// Optional context provided to <see cref="ICatalogResolutionProvider.Resolve"/>
/// to enable higher-priority resolution levels (skeleton-pin, template-default).
/// </summary>
/// <param name="SkeletonId">ID of the skeleton being designed (enables skeleton-pin resolution).</param>
/// <param name="TemplateId">ID of the active template (enables template-default resolution).</param>
/// <param name="ScopedToPartId">Narrows resolution to a specific part.</param>
public sealed record CatalogResolutionContext(
    Guid? SkeletonId = null,
    Guid? TemplateId = null,
    Guid? ScopedToPartId = null);
