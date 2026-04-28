namespace SpaceOS.Cabinet.Catalog;

/// <summary>
/// Denormalized rating rollup for a <see cref="CatalogEntry"/>.
/// Maintained by the aggregate-method primary path (BE-06) with DB trigger as defense-in-depth.
/// </summary>
public sealed record RatingAggregate(
    int Count,
    decimal AverageStars,
    DateTimeOffset? LastRatedAt)
{
    /// <summary>An empty rating aggregate with zero count and zero average.</summary>
    public static readonly RatingAggregate Empty = new(0, 0m, null);
}
