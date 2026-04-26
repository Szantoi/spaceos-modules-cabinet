namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.ConstructionTemplate"/> schema version 1.</summary>
public sealed record ConstructionTemplatePayloadV1
{
    /// <summary>Ordered list of construction rule identifiers (e.g. "R-32mm-LineBore").</summary>
    public required string[] Rules { get; init; }
}
