namespace SpaceOS.Cabinet.Catalog.Payloads;

using System.Text.Json;

/// <summary>Payload for <see cref="CatalogType.JointType"/> schema version 1.</summary>
public sealed record JointTypePayloadV1
{
    /// <summary>Joint type identifier (e.g. "FaceEdgeButt", "Dado").</summary>
    public required string Type { get; init; }

    /// <summary>Optional type-specific parameters.</summary>
    public Dictionary<string, JsonElement>? Options { get; init; }
}
