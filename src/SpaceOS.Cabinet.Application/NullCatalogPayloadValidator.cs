namespace SpaceOS.Cabinet.Application;

using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;

/// <summary>
/// No-op <see cref="ICatalogPayloadValidator"/> that accepts any payload.
/// Intended for use in environments where schema validation is delegated to the consumer
/// (e.g., integration tests, lightweight pipelines).
/// </summary>
public sealed class NullCatalogPayloadValidator : ICatalogPayloadValidator
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullCatalogPayloadValidator Instance = new();

    private NullCatalogPayloadValidator() { }

    /// <inheritdoc/>
    public Result Validate(CatalogType type, string payloadSchemaVersion, string payloadJson)
        => Result.Success();
}
