namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.MaterialThickness"/> schema version 1.</summary>
public sealed record MaterialThicknessPayloadV1
{
    /// <summary>Thickness value.</summary>
    public required decimal Value { get; init; }

    /// <summary>Unit of measurement (e.g. "mm").</summary>
    public required string Unit { get; init; }

    /// <summary>Material name (e.g. "Particleboard", "MDF").</summary>
    public required string Material { get; init; }
}
