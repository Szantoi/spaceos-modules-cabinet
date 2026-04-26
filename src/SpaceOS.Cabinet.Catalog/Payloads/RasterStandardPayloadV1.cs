namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.RasterStandard"/> schema version 1.</summary>
public sealed record RasterStandardPayloadV1
{
    /// <summary>Hole pitch in mm (typically 32).</summary>
    public required decimal Pitch { get; init; }

    /// <summary>Distance from reference edge to first hole in mm.</summary>
    public required decimal FirstHole { get; init; }

    /// <summary>Drill hole diameter in mm.</summary>
    public required decimal HoleDiameter { get; init; }
}
