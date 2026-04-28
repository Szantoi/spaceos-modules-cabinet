using System.Text.Json;
using System.Text.Json.Nodes;
using Ardalis.Result;
using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class SnapshotMigrator023Tests
{
    private readonly SnapshotMigrator_0_2_to_0_3 _migrator = new();

    /// <summary>
    /// A minimal but structurally valid v0.2 skeleton snapshot JSON.
    /// All required fields for SkeletonSnapshot are present.
    /// </summary>
    private static string ValidSnapshot_0_2() =>
        """
        {
          "schemaVersion":"0.2",
          "id":"11111111-1111-1111-1111-111111111111",
          "tenantId":"22222222-2222-2222-2222-222222222222",
          "version":"33333333-3333-3333-3333-333333333333",
          "lastSequenceNumber":5,
          "dimensionWidth":600,
          "dimensionHeight":720,
          "dimensionDepth":560,
          "parts":[],
          "connections":[],
          "roleAssignments":[],
          "pinnedCatalogEntries":[]
        }
        """;

    // ── CanMigrate ────────────────────────────────────────────────────────────

    [Fact]
    public void CanMigrate_0_2_To_0_3_ReturnsTrue()
    {
        Assert.True(_migrator.CanMigrate("0.2", "0.3"));
    }

    [Fact]
    public void CanMigrate_0_1_To_0_3_ReturnsFalse()
    {
        Assert.False(_migrator.CanMigrate("0.1", "0.3"));
    }

    [Fact]
    public void CanMigrate_0_3_To_0_4_ReturnsFalse()
    {
        Assert.False(_migrator.CanMigrate("0.3", "0.4"));
    }

    // ── Migrate success ───────────────────────────────────────────────────────

    [Fact]
    public void Migrate_ValidV02_Succeeds()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_2(), "0.2", "0.3");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Migrate_SetsSchemaVersion03()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_2(), "0.2", "0.3");

        var doc = JsonNode.Parse(result.Value)!.AsObject();
        Assert.Equal("0.3", doc["schemaVersion"]!.GetValue<string>());
    }

    [Fact]
    public void Migrate_AddsAppliedTenantStandardAsNull()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_2(), "0.2", "0.3");

        var doc = JsonNode.Parse(result.Value)!.AsObject();
        Assert.True(doc.ContainsKey("appliedTenantStandard"));
        // The field must be present but its value is JSON null
        Assert.Null(doc["appliedTenantStandard"]?.GetValue<string>());
    }

    [Fact]
    public void Migrate_PreservesOtherFields()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_2(), "0.2", "0.3");

        var doc = JsonNode.Parse(result.Value)!.AsObject();
        Assert.Equal("11111111-1111-1111-1111-111111111111", doc["id"]!.GetValue<string>());
        Assert.Equal("22222222-2222-2222-2222-222222222222", doc["tenantId"]!.GetValue<string>());
        Assert.Equal(5, doc["lastSequenceNumber"]!.GetValue<int>());
        Assert.Equal(600, doc["dimensionWidth"]!.GetValue<int>());
    }

    // ── Migrate errors ────────────────────────────────────────────────────────

    [Fact]
    public void Migrate_WrongFromVersion_ReturnsError()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_2(), "0.1", "0.3");

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Migrate_InvalidJson_ReturnsError()
    {
        var result = _migrator.Migrate("not valid json {{{", "0.2", "0.3");

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Migrate_EmptyString_ReturnsError()
    {
        var result = _migrator.Migrate("", "0.2", "0.3");

        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
