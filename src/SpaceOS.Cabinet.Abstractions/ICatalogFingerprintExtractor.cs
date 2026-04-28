namespace SpaceOS.Cabinet.Abstractions;

using System.Text.Json;

/// <summary>
/// Type-specific fingerprint extraction from <c>CatalogEntry</c> payload (SEC-02: server-side only).
/// Implementations are responsible for producing a normalized, deterministic fingerprint
/// that can be used to cluster similar entries across tenants.
/// </summary>
public interface ICatalogFingerprintExtractor
{
    /// <summary>
    /// Returns a normalized <c>"type:vendor:code:variant"</c> string, or <c>null</c> if the
    /// entry is not clusterable (e.g. payload lacks the required fields).
    /// </summary>
    /// <param name="type">The catalog type of the entry.</param>
    /// <param name="payload">Parsed JSON payload of the catalog entry.</param>
    /// <returns>A normalized fingerprint string, or <c>null</c>.</returns>
    string? Extract(CatalogType type, JsonDocument payload);
}
