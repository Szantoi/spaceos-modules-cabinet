namespace SpaceOS.Cabinet.Catalog;

using Ardalis.Result;

/// <summary>
/// Validates the JSON payload of a <see cref="CatalogEntry"/> against its declared schema version.
/// </summary>
public interface ICatalogPayloadValidator
{
    /// <summary>
    /// Validates <paramref name="payloadJson"/> against the schema identified by
    /// <paramref name="type"/> and <paramref name="payloadSchemaVersion"/>.
    /// </summary>
    /// <param name="type">The catalog type that defines the payload shape.</param>
    /// <param name="payloadSchemaVersion">Schema version string (e.g. "horizontalRole/v1").</param>
    /// <param name="payloadJson">Raw JSON to validate.</param>
    /// <returns>Success, or an error result with validation details.</returns>
    Result Validate(CatalogType type, string payloadSchemaVersion, string payloadJson);
}
