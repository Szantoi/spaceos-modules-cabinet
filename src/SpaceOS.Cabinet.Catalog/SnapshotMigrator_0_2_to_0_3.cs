namespace SpaceOS.Cabinet.Catalog;

using System.Text.Json;
using System.Text.Json.Nodes;
using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Forward-only migration from <c>SkeletonSnapshot</c> schema version "0.2" to "0.3".
/// Adds the <c>appliedTenantStandard</c> field (null) introduced in v0.3.
/// </summary>
public sealed class SnapshotMigrator_0_2_to_0_3 : ISnapshotMigrator
{
    /// <inheritdoc/>
    public bool CanMigrate(string fromVersion, string toVersion)
        => fromVersion == "0.2" && toVersion == "0.3";

    /// <inheritdoc/>
    public Result<string> Migrate(string snapshotJson, string fromVersion, string toVersion)
    {
        if (fromVersion != "0.2" || toVersion != "0.3")
            return Result<string>.Error(
                $"This migrator only supports 0.2 -> 0.3, got '{fromVersion}' -> '{toVersion}'");

        if (string.IsNullOrWhiteSpace(snapshotJson))
            return Result<string>.Error("Snapshot JSON is null or empty.");

        JsonObject? doc;
        try
        {
            doc = JsonNode.Parse(snapshotJson)?.AsObject();
        }
        catch (JsonException ex)
        {
            return Result<string>.Error($"Invalid JSON: {ex.Message}");
        }

        if (doc is null)
            return Result<string>.Error("Snapshot is not a JSON object.");

        var version = doc["schemaVersion"]?.GetValue<string>();
        if (version != "0.2")
            return Result<string>.Error($"Source must be SchemaVersion 0.2, got '{version}'");

        doc["schemaVersion"] = "0.3";
        doc["appliedTenantStandard"] = JsonValue.Create<string?>(null);

        return Result<string>.Success(doc.ToJsonString());
    }
}
