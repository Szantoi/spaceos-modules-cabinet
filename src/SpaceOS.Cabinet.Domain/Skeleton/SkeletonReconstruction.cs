using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Reconstructs a <see cref="Skeleton"/> aggregate from a <see cref="SkeletonSnapshot"/>.
/// SEC-CAB-6: performs full post-deserialise validation before reconstruction.
/// </summary>
/// <remarks>
/// Frame data (AffineTransform matrices) is not stored in Cabinet 0.1 snapshots to keep the
/// snapshot schema simple. BaseCuboid parts are reconstructed geometrically from the stored
/// assembly dimensions; full per-part frame persistence is Cabinet 0.2 scope.
/// </remarks>
public static class SkeletonReconstruction
{
    /// <summary>
    /// Validates and reconstructs a <see cref="Skeleton"/> from a snapshot.
    /// Returns an error result if any invariant is violated (SEC-CAB-5, SEC-CAB-6).
    /// </summary>
    public static Result<Skeleton> FromSnapshot(SkeletonSnapshot snapshot)
    {
        // SEC-CAB-5: enforce part and connection count limits
        if (snapshot.Parts.Count > Skeleton.MaxPartsPerSkeleton)
            return Result<Skeleton>.Error(
                $"Part count {snapshot.Parts.Count} exceeds maximum {Skeleton.MaxPartsPerSkeleton}.");

        if (snapshot.Connections.Count > Skeleton.MaxConnectionsPerSkeleton)
            return Result<Skeleton>.Error(
                $"Connection count {snapshot.Connections.Count} exceeds maximum {Skeleton.MaxConnectionsPerSkeleton}.");

        // SEC-CAB-2: cross-tenant part isolation check
        foreach (var part in snapshot.Parts)
        {
            if (part.SkeletonId != snapshot.Id)
                return Result<Skeleton>.Error(
                    $"Cross-tenant Part detected: Part {part.Id} has SkeletonId {part.SkeletonId}, expected {snapshot.Id}.");
        }

        // Validate connections reference existing parts and belong to the correct skeleton
        var partIds = new HashSet<Guid>(snapshot.Parts.Select(p => p.Id));

        foreach (var conn in snapshot.Connections)
        {
            if (conn.SkeletonId != snapshot.Id)
                return Result<Skeleton>.Error(
                    $"Connection {conn.Id} has mismatched SkeletonId {conn.SkeletonId}, expected {snapshot.Id}.");

            if (!partIds.Contains(conn.ParentPartId))
                return Result<Skeleton>.Error(
                    $"Connection {conn.Id} references non-existent parent part {conn.ParentPartId}.");

            if (!partIds.Contains(conn.ChildPartId))
                return Result<Skeleton>.Error(
                    $"Connection {conn.Id} references non-existent child part {conn.ChildPartId}.");
        }

        // Validate and reconstruct assembly dimensions
        var dimensionResult = AssemblyDimension.Create(
            snapshot.DimensionWidth,
            snapshot.DimensionHeight,
            snapshot.DimensionDepth);

        if (!dimensionResult.IsSuccess)
            return Result<Skeleton>.Error(
                $"Invalid assembly dimensions in snapshot: {string.Join(", ", dimensionResult.ValidationErrors.Select(e => e.ErrorMessage))}");

        var dimension = dimensionResult.Value;

        // Reconstruct BaseCuboid from dimensions (frame data is Cabinet 0.2 scope)
        var baseCuboidResult = BaseCuboid.CreateDefault(snapshot.Id, dimension, carcassThickness: 18.0);
        if (!baseCuboidResult.IsSuccess)
            return Result<Skeleton>.Error(
                $"Failed to reconstruct BaseCuboid: {string.Join(", ", baseCuboidResult.Errors)}");

        // Reconstruct additional (non-BaseCuboid) parts using identity frames
        // Full per-part frame persistence is Cabinet 0.2 scope
        var baseCuboidPartCount = baseCuboidResult.Value.GetAllParts().Count();
        var additionalParts = new List<Part>();

        foreach (var partSnap in snapshot.Parts.Skip(baseCuboidPartCount))
        {
            var dimResult = PartDimension.Create(100, 100, 18); // placeholder — Cabinet 0.2 stores frame data
            if (!dimResult.IsSuccess) continue;

            var frameResult = PartFrame.Create(AffineTransform.Identity, dimResult.Value);
            if (!frameResult.IsSuccess) continue;

            var part = new Part(partSnap.Id, snapshot.Id, frameResult.Value,
                partSnap.MaterialReference, partSnap.PartCatalogReference);

            if (partSnap.AssignedRole.HasValue)
                part.AssignRole(partSnap.AssignedRole.Value);

            additionalParts.Add(part);
        }

        // Reconstruct connections
        var connections = snapshot.Connections.Select(c => new Connection(
            c.Id, c.SkeletonId, c.ParentPartId, c.ChildPartId, c.JointType,
            new ConnectionGeometry(c.ParentFace, c.ChildEdge, c.EdgeOffset))).ToList();

        // Restore pinned catalog entries (v0.2+)
        var pinnedCatalogEntries = new Dictionary<(Guid PartId, CatalogType Type), Guid>();
        foreach (var pinned in snapshot.PinnedCatalogEntries)
        {
            var catalogType = (CatalogType)pinned.CatalogType;
            pinnedCatalogEntries[(pinned.PartId, catalogType)] = pinned.CatalogEntryId;
        }

        var skeleton = Skeleton.Reconstruct(
            snapshot.Id,
            snapshot.TenantId,
            snapshot.Version,
            snapshot.LastSequenceNumber,
            dimension,
            baseCuboidResult.Value,
            additionalParts,
            connections,
            pinnedCatalogEntries);

        return Result<Skeleton>.Success(skeleton);
    }
}
