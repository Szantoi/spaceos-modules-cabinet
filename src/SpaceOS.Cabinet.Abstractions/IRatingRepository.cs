namespace SpaceOS.Cabinet.Abstractions;

using Ardalis.Result;

/// <summary>
/// Thin port for rating lookup (consumer implements).
/// Decouples the domain from the infrastructure layer.
/// </summary>
public interface IRatingRepository
{
    /// <summary>
    /// Returns the <c>(Stars, Comment)</c> tuple for a given entry and rater tenant pair,
    /// or an error result if no rating is found.
    /// </summary>
    /// <param name="entryId">The catalog entry identifier.</param>
    /// <param name="raterTenantId">The tenant that submitted the rating.</param>
    /// <returns>A success result with <c>(Stars, Comment)</c>, or an error result.</returns>
    Result<(int Stars, string? Comment)> GetByEntryAndTenant(Guid entryId, Guid raterTenantId);
}
