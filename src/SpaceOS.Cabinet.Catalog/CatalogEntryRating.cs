namespace SpaceOS.Cabinet.Catalog;

using Ardalis.Result;

/// <summary>
/// Represents a tenant's star-rating for a <see cref="CatalogEntry"/> owned by another tenant.
/// Self-rating is forbidden. Stars must be in the range 1–5. Comment is limited to 500 characters.
/// </summary>
public sealed class CatalogEntryRating
{
    /// <summary>Unique identifier of this rating.</summary>
    public Guid Id { get; private set; }

    /// <summary>The catalog entry being rated.</summary>
    public Guid CatalogEntryId { get; private set; }

    /// <summary>Tenant that submitted the rating.</summary>
    public Guid RaterTenantId { get; private set; }

    /// <summary>User within <see cref="RaterTenantId"/> who submitted the rating.</summary>
    public Guid RaterUserId { get; private set; }

    /// <summary>Star rating (1–5 inclusive).</summary>
    public int Stars { get; private set; }

    /// <summary>Optional free-text comment (max 500 characters).</summary>
    public string? Comment { get; private set; }

    /// <summary>UTC timestamp when the rating was created or last updated.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private CatalogEntryRating() { }

    /// <summary>
    /// Creates a new <see cref="CatalogEntryRating"/>.
    /// </summary>
    /// <param name="catalogEntryId">The entry being rated.</param>
    /// <param name="raterTenantId">Tenant submitting the rating. Must not equal <paramref name="entryOwnerTenantId"/>.</param>
    /// <param name="raterUserId">User submitting the rating.</param>
    /// <param name="stars">Star rating (1–5).</param>
    /// <param name="comment">Optional comment (max 500 characters).</param>
    /// <param name="entryOwnerTenantId">Owning tenant of the entry (used for self-rating guard).</param>
    /// <returns>Success with the new rating, or a validation result.</returns>
    public static Result<CatalogEntryRating> Create(
        Guid catalogEntryId,
        Guid raterTenantId,
        Guid raterUserId,
        int stars,
        string? comment,
        Guid entryOwnerTenantId)
    {
        if (raterTenantId == entryOwnerTenantId)
            return Result<CatalogEntryRating>.Invalid(new ValidationError("Self-rating not allowed."));
        if (stars < 1 || stars > 5)
            return Result<CatalogEntryRating>.Invalid(new ValidationError("Stars must be 1–5."));
        if (comment?.Length > 500)
            return Result<CatalogEntryRating>.Invalid(new ValidationError("Comment max 500 chars."));

        return Result<CatalogEntryRating>.Success(new CatalogEntryRating
        {
            Id = Guid.NewGuid(),
            CatalogEntryId = catalogEntryId,
            RaterTenantId = raterTenantId,
            RaterUserId = raterUserId,
            Stars = stars,
            Comment = comment,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Updates the star rating and optional comment in place (re-rating path).
    /// </summary>
    /// <param name="newStars">New star value (1–5).</param>
    /// <param name="newComment">New optional comment.</param>
    public void UpdateStars(int newStars, string? newComment)
    {
        Stars = newStars;
        Comment = newComment;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
