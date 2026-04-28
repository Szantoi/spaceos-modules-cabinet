namespace SpaceOS.Cabinet.Tests.Infrastructure;

using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;

/// <summary>
/// In-memory test double for <see cref="IRatingRepository"/>.
/// </summary>
internal sealed class InMemoryRatingRepository : IRatingRepository
{
    private readonly Dictionary<(Guid entryId, Guid raterTenantId), (int Stars, string? Comment)> _store = new();

    /// <summary>Adds or replaces a rating directly (test helper).</summary>
    public void Set(Guid entryId, Guid raterTenantId, int stars, string? comment = null)
        => _store[(entryId, raterTenantId)] = (stars, comment);

    /// <inheritdoc/>
    public Result<(int Stars, string? Comment)> GetByEntryAndTenant(Guid entryId, Guid raterTenantId)
    {
        if (_store.TryGetValue((entryId, raterTenantId), out var value))
            return Result<(int Stars, string? Comment)>.Success(value);

        return Result<(int Stars, string? Comment)>.Error("Rating not found.");
    }
}
