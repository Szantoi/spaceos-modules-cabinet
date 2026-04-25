using SpaceOS.Cabinet.Domain.Events;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class DomainEventTests
{
    private static Skeleton CreateSkeleton()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        return Skeleton.Create(Guid.NewGuid(), dim).Value;
    }

    // ── Interface implementation ───────────────────────────────────────────────

    [Fact]
    public void SkeletonCreated_ImplementsIDomainEvent()
    {
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(typeof(SkeletonCreated)));
    }

    [Fact]
    public void PartAdded_ImplementsIDomainEvent()
    {
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(typeof(PartAdded)));
    }

    [Fact]
    public void PartRemoved_ImplementsIDomainEvent()
    {
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(typeof(PartRemoved)));
    }

    [Fact]
    public void ConnectionAdded_ImplementsIDomainEvent()
    {
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(typeof(ConnectionAdded)));
    }

    [Fact]
    public void ConnectionRemoved_ImplementsIDomainEvent()
    {
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(typeof(ConnectionRemoved)));
    }

    [Fact]
    public void SkeletonResized_ImplementsIDomainEvent()
    {
        Assert.True(typeof(IDomainEvent).IsAssignableFrom(typeof(SkeletonResized)));
    }

    // ── Sequence number ────────────────────────────────────────────────────────

    [Fact]
    public void SkeletonCreated_HasSequenceNumberOne()
    {
        var skeleton = CreateSkeleton();
        var createdEvent = skeleton.DomainEvents.OfType<SkeletonCreated>().Single();

        Assert.Equal(1, createdEvent.SequenceNumber);
    }

    [Fact]
    public void PartAdded_HasSequenceNumberGreaterThanSkeletonCreated()
    {
        var skeleton = CreateSkeleton();
        var dim = PartDimension.Create(200, 560, 18).Value;
        var frame = PartFrame.Create(AffineTransform.Identity, dim).Value;
        skeleton.AddPart(frame, "mat");

        var created = skeleton.DomainEvents.OfType<SkeletonCreated>().Single();
        var partAdded = skeleton.DomainEvents.OfType<PartAdded>().Single();

        Assert.True(partAdded.SequenceNumber > created.SequenceNumber);
    }

    [Fact]
    public void AllEvents_HaveOccurredAtSet()
    {
        var skeleton = CreateSkeleton();

        foreach (var evt in skeleton.DomainEvents)
            Assert.True(evt.OccurredAt > DateTime.MinValue);
    }

    // ── Event-specific properties ─────────────────────────────────────────────

    [Fact]
    public void SkeletonCreated_HasCorrectSkeletonIdAndTenantId()
    {
        var tenantId = Guid.NewGuid();
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        var skeleton = Skeleton.Create(tenantId, dim).Value;
        var evt = skeleton.DomainEvents.OfType<SkeletonCreated>().Single();

        Assert.Equal(skeleton.Id, evt.SkeletonId);
        Assert.Equal(tenantId, evt.TenantId);
    }

    [Fact]
    public void SkeletonResized_ContainsOldAndNewDimensions()
    {
        var skeleton = CreateSkeleton();
        var oldDim = skeleton.Dimension;
        var newDim = AssemblyDimension.Create(800, 900, 600).Value;
        skeleton.ResizeAssembly(newDim);

        var resizedEvent = skeleton.DomainEvents.OfType<SkeletonResized>().Single();

        Assert.Equal(oldDim, resizedEvent.OldDim);
        Assert.Equal(newDim, resizedEvent.NewDim);
    }
}
