using Ardalis.Result;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Represents a directed joinery connection between two Parts in the same Skeleton.
/// Construction is intentionally internal — use <see cref="Skeleton.AddConnection"/> to create connections.
/// </summary>
public sealed class Connection
{
    /// <summary>Unique identifier of this connection.</summary>
    public Guid Id { get; private set; }

    /// <summary>ID of the owning Skeleton.</summary>
    public Guid SkeletonId { get; private set; }

    /// <summary>ID of the parent Part (the part that receives the child).</summary>
    public Guid ParentPartId { get; private set; }

    /// <summary>ID of the child Part (the part that is attached to the parent).</summary>
    public Guid ChildPartId { get; private set; }

    /// <summary>The joinery method used for this connection.</summary>
    public JointType JointType { get; private set; }

    /// <summary>Geometric descriptor of the joint location and orientation.</summary>
    public ConnectionGeometry Geometry { get; private set; }

    // Internal ctor — connections are created exclusively through Skeleton.AddConnection().
    internal Connection(
        Guid id,
        Guid skeletonId,
        Guid parentPartId,
        Guid childPartId,
        JointType jointType,
        ConnectionGeometry geometry)
    {
        Id = id;
        SkeletonId = skeletonId;
        ParentPartId = parentPartId;
        ChildPartId = childPartId;
        JointType = jointType;
        Geometry = geometry;
    }

    /// <summary>Changes the joint type for this connection.</summary>
    internal Result SetJointType(JointType jointType)
    {
        JointType = jointType;
        return Result.Success();
    }
}
