namespace SpaceOS.Cabinet.Catalog.Infrastructure;

using System.Text.Json;
using SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Default implementation of <see cref="ICatalogFingerprintExtractor"/>.
/// Extracts a normalized <c>"type:vendor:code:variant"</c> fingerprint from the JSON payload.
/// Returns <c>null</c> if any of the three required fields (<c>vendor</c>, <c>code</c>, <c>variant</c>) are absent.
/// All output is lowercased for deterministic clustering.
/// </summary>
public sealed class DefaultCatalogFingerprintExtractor : ICatalogFingerprintExtractor
{
    /// <inheritdoc/>
    public string? Extract(CatalogType type, JsonDocument payload)
    {
        if (payload is null)
            return null;

        var root = payload.RootElement;

        if (!TryGetString(root, "vendor", out var vendor) || string.IsNullOrWhiteSpace(vendor))
            return null;

        if (!TryGetString(root, "code", out var code) || string.IsNullOrWhiteSpace(code))
            return null;

        if (!TryGetString(root, "variant", out var variant) || string.IsNullOrWhiteSpace(variant))
            return null;

        var typePrefix = type.ToString().ToLowerInvariant();
        return $"{typePrefix}:{vendor!.ToLowerInvariant()}:{code!.ToLowerInvariant()}:{variant!.ToLowerInvariant()}";
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (!element.TryGetProperty(propertyName, out var prop))
            return false;

        if (prop.ValueKind != JsonValueKind.String)
            return false;

        value = prop.GetString();
        return true;
    }
}
