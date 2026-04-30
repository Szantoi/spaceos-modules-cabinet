namespace SpaceOS.Cabinet.Application.Queries;

using SpaceOS.Cabinet.Catalog;

/// <summary>DTO for a single step in an assembly sequence.</summary>
/// <param name="Order">Zero-based step order.</param>
/// <param name="Title">Short step title.</param>
/// <param name="Instruction">Sanitized markdown instruction.</param>
/// <param name="PrimaryPartId">The part primarily assembled in this step.</param>
/// <param name="RequiredConnectionIds">Connection IDs completed in this step.</param>
/// <param name="RequiredTools">Tools needed for this step.</param>
/// <param name="RequiredSkillLevel">Optional skill level string.</param>
public sealed record AssemblyStepDto(
    int Order,
    string Title,
    string Instruction,
    Guid PrimaryPartId,
    IReadOnlyList<Guid> RequiredConnectionIds,
    IReadOnlyList<string> RequiredTools,
    string? RequiredSkillLevel);

/// <summary>DTO for a single layer in an exploded-view diagram.</summary>
/// <param name="LayerIndex">Zero-based layer depth.</param>
/// <param name="PartIds">Part IDs belonging to this layer.</param>
public sealed record ExplodedViewLayerDto(int LayerIndex, IReadOnlyList<Guid> PartIds);

/// <summary>DTO for an exploded-view representation of a skeleton.</summary>
/// <param name="Layers">All layers in assembly order, from base outward.</param>
public sealed record ExplodedViewDto(IReadOnlyList<ExplodedViewLayerDto> Layers);

/// <summary>
/// DTO combining a <see cref="CatalogEntry"/> with its current rating rollup.
/// Returned by <see cref="GetCatalogEntryWithRatingsQuery"/>.
/// </summary>
/// <param name="Entry">The catalog entry aggregate.</param>
/// <param name="RatingCount">Total number of ratings submitted.</param>
/// <param name="AverageStars">Rolling average (0m when no ratings exist).</param>
/// <param name="LastRatedAt">UTC timestamp of the most recent rating, or <c>null</c>.</param>
public sealed record CatalogEntryWithRatingsDto(
    CatalogEntry Entry,
    int RatingCount,
    decimal AverageStars,
    DateTimeOffset? LastRatedAt);
