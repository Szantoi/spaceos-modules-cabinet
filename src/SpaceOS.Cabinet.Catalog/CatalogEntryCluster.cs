namespace SpaceOS.Cabinet.Catalog;

using Ardalis.Result;

/// <summary>
/// Groups <see cref="CatalogEntry"/> instances that share the same similarity fingerprint.
/// Tracks a canonical entry (highest-scoring, non-flagged, non-probationary member).
/// New members must survive a 7-day probation before being eligible for canonical.
/// </summary>
public sealed class CatalogEntryCluster
{
    /// <summary>Unique identifier of this cluster.</summary>
    public Guid Id { get; private set; }

    /// <summary>Normalized fingerprint that all members share (e.g. "type:vendor:code:variant").</summary>
    public string Fingerprint { get; private set; } = string.Empty;

    /// <summary>Catalog type discriminator shared by all members.</summary>
    public CatalogType Type { get; private set; }

    private readonly List<Guid> _memberEntryIds = new();

    /// <summary>Ordered list of member catalog entry IDs.</summary>
    public IReadOnlyList<Guid> MemberEntryIds => _memberEntryIds.AsReadOnly();

    /// <summary>Currently designated canonical (best-quality) entry in this cluster.</summary>
    public Guid CanonicalEntryId { get; private set; }

    /// <summary>True when the cluster has been logically removed (all members removed).</summary>
    public bool IsRemoved { get; private set; }

    /// <summary>UTC timestamp when the cluster was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>UTC timestamp of the last mutation.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Optimistic concurrency version counter; incremented on every mutation.</summary>
    public long Version { get; private set; }

    private CatalogEntryCluster() { }

    /// <summary>
    /// Creates a new cluster seeded with a single initial entry as the canonical.
    /// </summary>
    /// <param name="fingerprint">Non-empty normalized fingerprint for this cluster.</param>
    /// <param name="type">Catalog type of the entries in this cluster.</param>
    /// <param name="initialEntryId">The first (and initial canonical) member entry.</param>
    /// <returns>Success with the new cluster, or a validation result.</returns>
    public static Result<CatalogEntryCluster> CreateForEntry(
        string fingerprint, CatalogType type, Guid initialEntryId)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return Result<CatalogEntryCluster>.Invalid(new ValidationError("Fingerprint required."));
        if (initialEntryId == Guid.Empty)
            return Result<CatalogEntryCluster>.Invalid(new ValidationError("InitialEntryId required."));

        var now = DateTimeOffset.UtcNow;
        var cluster = new CatalogEntryCluster
        {
            Id = Guid.NewGuid(),
            Fingerprint = fingerprint,
            Type = type,
            CanonicalEntryId = initialEntryId,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };
        cluster._memberEntryIds.Add(initialEntryId);
        return Result<CatalogEntryCluster>.Success(cluster);
    }

    /// <summary>
    /// Adds a new member entry to this cluster.
    /// </summary>
    /// <param name="entryId">ID of the entry to add. Must not already be a member.</param>
    public Result AddMember(Guid entryId)
    {
        if (_memberEntryIds.Contains(entryId))
            return Result.Error("Already a member.");
        _memberEntryIds.Add(entryId);
        BumpVersion();
        return Result.Success();
    }

    /// <summary>
    /// Removes a member entry from this cluster. If the removed entry was canonical,
    /// the next member becomes canonical. If the cluster becomes empty, it is marked removed.
    /// </summary>
    /// <param name="entryId">ID of the entry to remove.</param>
    public Result RemoveMember(Guid entryId)
    {
        if (!_memberEntryIds.Remove(entryId))
            return Result.Invalid(new ValidationError($"Entry {entryId} not a member."));

        if (_memberEntryIds.Count == 0)
        {
            IsRemoved = true;
        }
        else if (CanonicalEntryId == entryId)
        {
            CanonicalEntryId = _memberEntryIds[0];
        }

        BumpVersion();
        return Result.Success();
    }

    /// <summary>
    /// Recomputes the canonical entry using a scoring model that weights rating average (50%),
    /// payload richness (30%), and recency (20%). Entries in 7-day probation or with active flags
    /// are excluded from candidacy. If no eligible candidate exists, the canonical is unchanged.
    /// </summary>
    /// <param name="memberScores">Scoring inputs keyed by member entry ID.</param>
    public Result RecomputeCanonical(IReadOnlyDictionary<Guid, ClusterScoringInputs> memberScores)
    {
        if (_memberEntryIds.Count == 0)
            return Result.Invalid(new ValidationError("Empty cluster."));

        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);
        var eligible = _memberEntryIds
            .Where(id => memberScores.TryGetValue(id, out var s)
                && s.CreatedAt < cutoff
                && s.ActiveFlagCount == 0)
            .ToList();

        if (eligible.Count == 0)
            return Result.Success();

        var best = eligible
            .Select(id => (id, score: Score(memberScores[id])))
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.id)
            .First().id;

        if (best != CanonicalEntryId)
        {
            CanonicalEntryId = best;
            BumpVersion();
        }

        return Result.Success();
    }

    private static double Score(ClusterScoringInputs s)
    {
        var recency = 1.0 / (1.0 + (DateTimeOffset.UtcNow - s.CreatedAt).TotalDays / 30);
        return ((double)s.Ratings.AverageStars * 0.5)
             + (s.PayloadFieldCount / 10.0 * 0.3)
             + (recency * 0.2);
    }

    private void BumpVersion()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }
}

/// <summary>Scoring inputs for a single cluster member used by <see cref="CatalogEntryCluster.RecomputeCanonical"/>.</summary>
public sealed record ClusterScoringInputs(
    RatingAggregate Ratings,
    int PayloadFieldCount,
    DateTimeOffset CreatedAt,
    int ActiveFlagCount);
