using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class CatalogEntryClusterTests
{
    private static CatalogEntryCluster CreateValid(Guid? initialId = null)
    {
        var id = initialId ?? Guid.NewGuid();
        return CatalogEntryCluster.CreateForEntry("hardware:vendor:HNG-100:std", CatalogType.HardwareSet, id).Value;
    }

    // ── CreateForEntry ────────────────────────────────────────────────────────

    [Fact]
    public void CreateForEntry_Valid_SuccessWithMemberCount1AndCanonical()
    {
        var initialId = Guid.NewGuid();
        var result = CatalogEntryCluster.CreateForEntry("fp:a:b:c", CatalogType.HorizontalRole, initialId);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.MemberEntryIds);
        Assert.Equal(initialId, result.Value.CanonicalEntryId);
    }

    [Fact]
    public void CreateForEntry_EmptyFingerprint_ReturnsInvalid()
    {
        var result = CatalogEntryCluster.CreateForEntry("", CatalogType.HorizontalRole, Guid.NewGuid());

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void CreateForEntry_EmptyInitialEntry_ReturnsInvalid()
    {
        var result = CatalogEntryCluster.CreateForEntry("fp:x:y:z", CatalogType.HorizontalRole, Guid.Empty);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    // ── AddMember ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddMember_New_SuccessAndMemberCount2()
    {
        var cluster = CreateValid();
        var newId = Guid.NewGuid();

        var result = cluster.AddMember(newId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, cluster.MemberEntryIds.Count);
    }

    [Fact]
    public void AddMember_Duplicate_ReturnsError()
    {
        var existingId = Guid.NewGuid();
        var cluster = CreateValid(existingId);

        var result = cluster.AddMember(existingId);

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    // ── RemoveMember ──────────────────────────────────────────────────────────

    [Fact]
    public void RemoveMember_Existing_Success()
    {
        var id1 = Guid.NewGuid();
        var cluster = CreateValid(id1);
        var id2 = Guid.NewGuid();
        cluster.AddMember(id2);

        var result = cluster.RemoveMember(id2);

        Assert.True(result.IsSuccess);
        Assert.Single(cluster.MemberEntryIds);
    }

    [Fact]
    public void RemoveMember_LastMember_SetsIsRemoved()
    {
        var id = Guid.NewGuid();
        var cluster = CreateValid(id);

        cluster.RemoveMember(id);

        Assert.True(cluster.IsRemoved);
    }

    [Fact]
    public void RemoveMember_WhenRemoved_CanonicalChangesToNextMember()
    {
        var id1 = Guid.NewGuid();
        var cluster = CreateValid(id1);
        var id2 = Guid.NewGuid();
        cluster.AddMember(id2);

        cluster.RemoveMember(id1);

        Assert.Equal(id2, cluster.CanonicalEntryId);
    }

    [Fact]
    public void RemoveMember_NonMember_ReturnsInvalid()
    {
        var cluster = CreateValid();

        var result = cluster.RemoveMember(Guid.NewGuid());

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    // ── RecomputeCanonical ────────────────────────────────────────────────────

    [Fact]
    public void RecomputeCanonical_SkipsProbationMembers()
    {
        // Both members are brand-new (within 7-day probation), so no change.
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var cluster = CreateValid(id1);
        cluster.AddMember(id2);

        var scores = new Dictionary<Guid, ClusterScoringInputs>
        {
            [id1] = new(new RatingAggregate(5, 5m, null), 10, DateTimeOffset.UtcNow, 0),
            [id2] = new(new RatingAggregate(1, 1m, null), 1,  DateTimeOffset.UtcNow, 0)
        };

        var result = cluster.RecomputeCanonical(scores);

        Assert.True(result.IsSuccess);
        // Both are in probation — canonical unchanged
        Assert.Equal(id1, cluster.CanonicalEntryId);
    }

    [Fact]
    public void RecomputeCanonical_SkipsFlaggedMembers()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var cluster = CreateValid(id1);
        cluster.AddMember(id2);

        var oldDate = DateTimeOffset.UtcNow.AddDays(-30);
        var scores = new Dictionary<Guid, ClusterScoringInputs>
        {
            // id1 has flags — must be skipped
            [id1] = new(new RatingAggregate(5, 5m, null), 10, oldDate, 1),
            // id2 has no flags but probation cleared
            [id2] = new(new RatingAggregate(1, 1m, null), 5,  oldDate, 0)
        };

        cluster.RecomputeCanonical(scores);

        Assert.Equal(id2, cluster.CanonicalEntryId);
    }

    [Fact]
    public void RecomputeCanonical_PicksBestScoringEntry()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var cluster = CreateValid(id1);
        cluster.AddMember(id2);

        var oldDate = DateTimeOffset.UtcNow.AddDays(-30);
        var scores = new Dictionary<Guid, ClusterScoringInputs>
        {
            [id1] = new(new RatingAggregate(1, 1m, null), 1, oldDate, 0),
            // id2 has higher ratings and more fields → wins
            [id2] = new(new RatingAggregate(5, 5m, null), 10, oldDate, 0)
        };

        cluster.RecomputeCanonical(scores);

        Assert.Equal(id2, cluster.CanonicalEntryId);
    }

    [Fact]
    public void RecomputeCanonical_EmptyCluster_ReturnsInvalid()
    {
        var id = Guid.NewGuid();
        var cluster = CreateValid(id);
        cluster.RemoveMember(id);

        var result = cluster.RecomputeCanonical(new Dictionary<Guid, ClusterScoringInputs>());

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void RecomputeCanonical_AllInProbation_CanonicalUnchanged()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var cluster = CreateValid(id1);
        cluster.AddMember(id2);

        var scores = new Dictionary<Guid, ClusterScoringInputs>
        {
            // Both created just now (in probation)
            [id1] = new(RatingAggregate.Empty, 10, DateTimeOffset.UtcNow, 0),
            [id2] = new(RatingAggregate.Empty, 10, DateTimeOffset.UtcNow, 0)
        };

        cluster.RecomputeCanonical(scores);

        Assert.Equal(id1, cluster.CanonicalEntryId);
    }

    // ── Version ───────────────────────────────────────────────────────────────

    [Fact]
    public void BumpVersion_OnEveryMutation()
    {
        var cluster = CreateValid();
        var v1 = cluster.Version;

        cluster.AddMember(Guid.NewGuid());
        var v2 = cluster.Version;

        cluster.AddMember(Guid.NewGuid());
        var v3 = cluster.Version;

        Assert.True(v2 > v1);
        Assert.True(v3 > v2);
    }
}
