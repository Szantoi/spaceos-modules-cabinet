namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.HardwareSet"/> schema version 1.</summary>
public sealed record HardwareSetPayloadV1
{
    /// <summary>Hinge article codes.</summary>
    public required string[] Hinges { get; init; }

    /// <summary>Shelf-pin article codes.</summary>
    public required string[] ShelfPins { get; init; }
}
