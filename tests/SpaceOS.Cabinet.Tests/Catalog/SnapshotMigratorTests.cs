using System.Text.Json;
using System.Text.Json.Nodes;
using Ardalis.Result;
using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class SnapshotMigratorTests
{
    private readonly SnapshotMigrator_0_1_to_0_2 _migrator = new();

    private static string ValidSnapshot_0_1() =>
        """{"schemaVersion":"0.1","entries":[]}""";

    [Fact]
    public void CanMigrate_0_1_to_0_2_ReturnsTrue()
    {
        Assert.True(_migrator.CanMigrate("0.1", "0.2"));
    }

    [Fact]
    public void CanMigrate_WrongVersions_ReturnsFalse()
    {
        Assert.False(_migrator.CanMigrate("0.2", "0.3"));
    }

    [Fact]
    public void Migrate_ValidSnapshot_UpdatesSchemaVersion()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_1(), "0.1", "0.2");

        Assert.True(result.IsSuccess);
        var doc = JsonNode.Parse(result.Value)!.AsObject();
        Assert.Equal("0.2", doc["schemaVersion"]!.GetValue<string>());
    }

    [Fact]
    public void Migrate_ValidSnapshot_AddsNewFields()
    {
        var result = _migrator.Migrate(ValidSnapshot_0_1(), "0.1", "0.2");

        Assert.True(result.IsSuccess);
        var doc = JsonNode.Parse(result.Value)!.AsObject();
        Assert.NotNull(doc["roleAssignments"]);
        Assert.NotNull(doc["pinnedCatalogEntries"]);
    }

    [Fact]
    public void Migrate_WrongSourceVersion_ReturnsError()
    {
        var snapshot = """{"schemaVersion":"0.2","entries":[]}""";

        var result = _migrator.Migrate(snapshot, "0.1", "0.2");

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Migrate_InvalidJson_ReturnsError()
    {
        var result = _migrator.Migrate("not valid json", "0.1", "0.2");

        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
