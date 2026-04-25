using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Represents a single rectangular panel within a cabinet Skeleton.
/// Parts are always owned by exactly one Skeleton (SEC-CAB-2: cross-tenant isolation).
/// Construction is intentionally internal — use <see cref="Skeleton.AddPart"/> to create parts.
/// </summary>
public sealed class Part
{
    /// <summary>Unique identifier of this part.</summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// ID of the owning Skeleton. Immutable after construction (SEC-CAB-2).
    /// </summary>
    public Guid SkeletonId { get; private set; }

    /// <summary>Position and orientation of this part within assembly space.</summary>
    public PartFrame Frame { get; private set; }

    /// <summary>Reference key to the carcass or panel material (e.g. a SKU or material code).</summary>
    public string MaterialReference { get; private set; }

    /// <summary>Reference key into the tenant part catalog. May be empty for ad-hoc parts.</summary>
    public string PartCatalogReference { get; private set; }

    /// <summary>Semantic role assigned to this part, or <c>null</c> if not yet inferred.</summary>
    public PartRole? AssignedRole { get; private set; }

    // SEC-CAB-2: internal ctor only — Parts are created exclusively through Skeleton.AddPart().
    internal Part(Guid id, Guid skeletonId, PartFrame frame, string materialReference, string partCatalogReference)
    {
        Id = id;
        SkeletonId = skeletonId;
        Frame = frame;
        MaterialReference = materialReference;
        PartCatalogReference = partCatalogReference;
    }

    /// <summary>Updates the part's frame. Only callable within the domain assembly.</summary>
    internal Result UpdateFrame(PartFrame newFrame)
    {
        Frame = newFrame;
        return Result.Success();
    }

    /// <summary>Assigns a semantic role to this part.</summary>
    internal Result AssignRole(PartRole role)
    {
        AssignedRole = role;
        return Result.Success();
    }

    /// <summary>Clears any previously assigned semantic role.</summary>
    internal Result ClearAssignedRole()
    {
        AssignedRole = null;
        return Result.Success();
    }
}
