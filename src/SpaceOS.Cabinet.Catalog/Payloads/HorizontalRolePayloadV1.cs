namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>Payload for <see cref="CatalogType.HorizontalRole"/> schema version 1.</summary>
public sealed record HorizontalRolePayloadV1
{
    /// <summary>Named role (e.g. "Shelf", "CrossRail").</summary>
    public required string Role { get; init; }

    /// <summary>Resolution priority — lower wins.</summary>
    public required int Priority { get; init; }
}
