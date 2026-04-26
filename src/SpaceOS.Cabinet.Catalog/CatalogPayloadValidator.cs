namespace SpaceOS.Cabinet.Catalog;

using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.Result;
using SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>
/// Default <see cref="ICatalogPayloadValidator"/> that deserializes the JSON payload
/// against the DTO type registered in <see cref="CatalogPayloadSchemas"/>.
/// </summary>
public sealed class CatalogPayloadValidator : ICatalogPayloadValidator
{
    private static readonly JsonSerializerOptions StrictOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <inheritdoc/>
    public Result Validate(CatalogType type, string schemaVersion, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return Result.Error("PayloadJson is required.");

        var dtoType = CatalogPayloadSchemas.GetDtoType(type, schemaVersion);
        if (dtoType is null)
            return Result.Error($"Unknown payload schema: {type}/{schemaVersion}");

        try
        {
            var dto = JsonSerializer.Deserialize(payloadJson, dtoType, StrictOptions);
            if (dto is null)
                return Result.Error("Payload deserialized to null.");

            return Result.Success();
        }
        catch (JsonException ex)
        {
            return Result.Error($"Payload validation failed: {ex.Message}");
        }
    }
}
