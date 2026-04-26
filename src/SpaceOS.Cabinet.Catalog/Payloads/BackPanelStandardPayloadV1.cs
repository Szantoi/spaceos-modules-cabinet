namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.BackPanelStandard"/> schema version 1.</summary>
public sealed record BackPanelStandardPayloadV1
{
    /// <summary>Back panel thickness in mm.</summary>
    public required decimal Thickness { get; init; }

    /// <summary>Attachment method (e.g. "Groove", "Rabbet").</summary>
    public required string Attachment { get; init; }

    /// <summary>Material name (e.g. "HDF").</summary>
    public required string Material { get; init; }
}
