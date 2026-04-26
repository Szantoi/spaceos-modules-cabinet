namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.EdgeBandingRule"/> schema version 1.</summary>
public sealed record EdgeBandingRulePayloadV1
{
    /// <summary>Surfaces to band (e.g. ["Front", "SideExposed"]).</summary>
    public required string[] Surfaces { get; init; }

    /// <summary>Edge banding thickness in mm.</summary>
    public required decimal Thickness { get; init; }
}
