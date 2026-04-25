using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// A serialisable, schema-versioned snapshot of a <see cref="Skeleton"/> aggregate.
/// Used for persistence and snapshot migration (DB-CAB-2, SEC-CAB-6).
/// </summary>
public sealed class SkeletonSnapshot
{
    /// <summary>Schema version of this snapshot. Format: "major.minor" (e.g. "0.1").</summary>
    public string SchemaVersion { get; init; } = "0.1";

    /// <summary>Skeleton unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Owning tenant identifier.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Optimistic concurrency version token.</summary>
    public Guid Version { get; init; }

    /// <summary>Last assigned domain event sequence number.</summary>
    public long LastSequenceNumber { get; init; }

    /// <summary>Outer cabinet width in mm.</summary>
    public double DimensionWidth { get; init; }

    /// <summary>Outer cabinet height in mm.</summary>
    public double DimensionHeight { get; init; }

    /// <summary>Outer cabinet depth in mm.</summary>
    public double DimensionDepth { get; init; }

    /// <summary>Serialised parts (including BaseCuboid parts).</summary>
    public List<PartSnapshot> Parts { get; init; } = new();

    /// <summary>Serialised connections.</summary>
    public List<ConnectionSnapshot> Connections { get; init; } = new();

    // ── Schema version validation ────────────────────────────────────────────

    private static readonly Regex SchemaVersionRegex =
        new(@"^\d+\.\d+$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    // ── JSON options ─────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions StrictOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        NumberHandling = JsonNumberHandling.Strict,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    // ── Serialisation ────────────────────────────────────────────────────────

    /// <summary>Serialises this snapshot to a JSON string.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, StrictOptions);

    /// <summary>
    /// Deserialises a snapshot from a JSON string and validates schema version (DB-CAB-2).
    /// </summary>
    public static Result<SkeletonSnapshot> FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result<SkeletonSnapshot>.Invalid(new ValidationError("JSON string is null or empty."));

        SkeletonSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<SkeletonSnapshot>(json, StrictOptions);
        }
        catch (JsonException ex)
        {
            return Result<SkeletonSnapshot>.Invalid(new ValidationError($"Invalid JSON: {ex.Message}"));
        }

        if (snapshot is null)
            return Result<SkeletonSnapshot>.Invalid(new ValidationError("Deserialized snapshot is null."));

        // DB-CAB-2: schema version format check
        if (!SchemaVersionRegex.IsMatch(snapshot.SchemaVersion))
            return Result<SkeletonSnapshot>.Invalid(
                new ValidationError($"Invalid schema version format: '{snapshot.SchemaVersion}'. Expected 'major.minor'."));

        // Version compatibility: only "0.x" is supported by Cabinet 0.1.x reader
        var parts = snapshot.SchemaVersion.Split('.');
        if (int.TryParse(parts[0], out int major) && major >= 1)
            return Result<SkeletonSnapshot>.Error(
                $"Schema version {snapshot.SchemaVersion} is incompatible with Cabinet 0.1.x reader.");

        return Result<SkeletonSnapshot>.Success(snapshot);
    }

    // ── Projection ───────────────────────────────────────────────────────────

    /// <summary>Creates a snapshot from a live <see cref="Skeleton"/> aggregate.</summary>
    public static SkeletonSnapshot FromSkeleton(Skeleton skeleton)
    {
        return new SkeletonSnapshot
        {
            SchemaVersion = "0.1",
            Id = skeleton.Id,
            TenantId = skeleton.TenantId,
            Version = skeleton.Version,
            LastSequenceNumber = skeleton.LastSequenceNumber,
            DimensionWidth = skeleton.Dimension.Width,
            DimensionHeight = skeleton.Dimension.Height,
            DimensionDepth = skeleton.Dimension.Depth,
            Parts = skeleton.Parts.Select(p => new PartSnapshot
            {
                Id = p.Id,
                SkeletonId = p.SkeletonId,
                MaterialReference = p.MaterialReference,
                PartCatalogReference = p.PartCatalogReference,
                AssignedRole = p.AssignedRole
            }).ToList(),
            Connections = skeleton.Connections.Select(c => new ConnectionSnapshot
            {
                Id = c.Id,
                SkeletonId = c.SkeletonId,
                ParentPartId = c.ParentPartId,
                ChildPartId = c.ChildPartId,
                JointType = c.JointType,
                ParentFace = c.Geometry.ParentFace,
                ChildEdge = c.Geometry.ChildEdge,
                EdgeOffset = c.Geometry.EdgeOffset
            }).ToList()
        };
    }
}

/// <summary>Serialised representation of a <see cref="Part"/> for snapshot persistence.</summary>
public sealed class PartSnapshot
{
    /// <summary>Part unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Owning skeleton identifier.</summary>
    public Guid SkeletonId { get; init; }

    /// <summary>Material reference key.</summary>
    public string MaterialReference { get; init; } = string.Empty;

    /// <summary>Catalog reference key (may be empty).</summary>
    public string PartCatalogReference { get; init; } = string.Empty;

    /// <summary>Assigned semantic role, or <c>null</c> if not inferred.</summary>
    public PartRole? AssignedRole { get; init; }
}

/// <summary>Serialised representation of a <see cref="Connection"/> for snapshot persistence.</summary>
public sealed class ConnectionSnapshot
{
    /// <summary>Connection unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Owning skeleton identifier.</summary>
    public Guid SkeletonId { get; init; }

    /// <summary>Parent part identifier.</summary>
    public Guid ParentPartId { get; init; }

    /// <summary>Child part identifier.</summary>
    public Guid ChildPartId { get; init; }

    /// <summary>Joint type for this connection.</summary>
    public JointType JointType { get; init; }

    /// <summary>Parent face where the child is attached.</summary>
    public PartFace ParentFace { get; init; }

    /// <summary>Child edge that contacts the parent face.</summary>
    public PartEdge ChildEdge { get; init; }

    /// <summary>Edge offset in mm.</summary>
    public double EdgeOffset { get; init; }
}
