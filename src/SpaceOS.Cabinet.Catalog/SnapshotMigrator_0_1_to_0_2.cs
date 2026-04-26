namespace SpaceOS.Cabinet.Catalog;

using System.Text.Json;
using System.Text.Json.Nodes;
using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Migrates a Catalog snapshot from schema version "0.1" to "0.2".
/// Adds the <c>roleAssignments</c> array and <c>pinnedCatalogEntries</c> object introduced in v0.2.
/// </summary>
public sealed class SnapshotMigrator_0_1_to_0_2 : ISnapshotMigrator
{
    /// <inheritdoc/>
    public bool CanMigrate(string fromVersion, string toVersion)
        => fromVersion == "0.1" && toVersion == "0.2";

    /// <inheritdoc/>
    public Result<string> Migrate(string snapshotJson, string fromVersion, string toVersion)
    {
        if (fromVersion != "0.1" || toVersion != "0.2")
            return Result<string>.Error($"This migrator only supports 0.1 -> 0.2, got '{fromVersion}' -> '{toVersion}'");

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
        if (version != "0.1")
            return Result<string>.Error($"Source must be SchemaVersion 0.1, got '{version}'");

        doc["schemaVersion"] = "0.2";
        doc["roleAssignments"] = new JsonArray();
        doc["pinnedCatalogEntries"] = new JsonObject();

        return Result<string>.Success(doc.ToJsonString());
    }
}
