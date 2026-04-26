using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Catalog.Persistence;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Persistence;

/// <summary>
/// Unit tests for <see cref="CatalogEntryConfiguration"/> and <see cref="StaffAuditLogEntryConfiguration"/>
/// using EF Core's model builder directly — no database connection required.
/// </summary>
public class CatalogEntryConfigurationTests
{
    private static IModel BuildModel()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new CatalogEntryConfiguration());
        builder.ApplyConfiguration(new StaffAuditLogEntryConfiguration());
        return builder.FinalizeModel();
    }

    // ── CatalogEntry table mapping ─────────────────────────────────────────────

    [Fact]
    public void Configuration_SetsCorrectTableName()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        Assert.Equal("catalog_entries", entity.GetTableName());
    }

    [Fact]
    public void Configuration_SetsIdAsPrimaryKey()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var pk = entity.FindPrimaryKey()!;
        var prop = pk.Properties.Single();
        Assert.Equal("Id", prop.Name);
    }

    [Fact]
    public void Configuration_TenantId_IsRequired()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var prop = entity.FindProperty("TenantId")!;
        Assert.False(prop.IsNullable);
    }

    [Fact]
    public void Configuration_Name_HasMaxLength100()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var prop = entity.FindProperty("Name")!;
        Assert.Equal(100, prop.GetMaxLength());
    }

    [Fact]
    public void Configuration_Description_HasMaxLength500()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var prop = entity.FindProperty("Description")!;
        Assert.Equal(500, prop.GetMaxLength());
    }

    [Fact]
    public void Configuration_PayloadJson_IsJsonbType()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var prop = entity.FindProperty("PayloadJson")!;
        // GetColumnType() requires runtime type-mapping init; read the annotation directly instead.
        var annotation = prop.FindAnnotation("Relational:ColumnType");
        Assert.NotNull(annotation);
        Assert.Equal("jsonb", annotation.Value as string);
    }

    [Fact]
    public void Configuration_ContentHash_HasMaxLength64()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var prop = entity.FindProperty("ContentHash")!;
        Assert.Equal(64, prop.GetMaxLength());
    }

    [Fact]
    public void Configuration_Version_IsConcurrencyToken()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var prop = entity.FindProperty("Version")!;
        Assert.True(prop.IsConcurrencyToken);
    }

    [Fact]
    public void Configuration_HasIndex_TenantVisibilityTypeState()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var indexes = entity.GetIndexes().ToList();
        var hasComposite = indexes.Any(ix =>
            ix.GetDatabaseName() == "ix_catalog_entries_tenant_visibility_type_state");
        Assert.True(hasComposite);
    }

    [Fact]
    public void Configuration_HasIndex_ContentHash()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(CatalogEntry))!;
        var indexes = entity.GetIndexes().ToList();
        var hasContentHash = indexes.Any(ix =>
            ix.GetDatabaseName() == "ix_catalog_entries_content_hash");
        Assert.True(hasContentHash);
    }

    // ── StaffAuditLogEntry ─────────────────────────────────────────────────────

    [Fact]
    public void StaffAuditLogEntry_Create_WithValidData_Succeeds()
    {
        var staffId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var log = StaffAuditLogEntry.Create(staffId, "Approve", entryId, "All good");

        Assert.Equal(staffId, log.StaffUserId);
        Assert.Equal("Approve", log.Action);
        Assert.Equal(entryId, log.CatalogEntryId);
        Assert.Equal("All good", log.Details);
    }

    [Fact]
    public void StaffAuditLogEntry_Create_WithEmptyStaffUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StaffAuditLogEntry.Create(Guid.Empty, "Approve", Guid.NewGuid()));
    }

    [Fact]
    public void StaffAuditLogEntry_Create_WithEmptyAction_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StaffAuditLogEntry.Create(Guid.NewGuid(), "", Guid.NewGuid()));
    }

    [Fact]
    public void StaffAuditLogEntry_Create_WithWhitespaceAction_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StaffAuditLogEntry.Create(Guid.NewGuid(), "   ", Guid.NewGuid()));
    }

    [Fact]
    public void StaffAuditLogEntry_Create_WithEmptyCatalogEntryId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StaffAuditLogEntry.Create(Guid.NewGuid(), "Approve", Guid.Empty));
    }

    [Fact]
    public void StaffAuditLogEntry_HasId_AfterCreate()
    {
        var log = StaffAuditLogEntry.Create(Guid.NewGuid(), "Publish", Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, log.Id);
    }

    [Fact]
    public void StaffAuditLogEntry_HasTimestamp_AfterCreate()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var log = StaffAuditLogEntry.Create(Guid.NewGuid(), "Deprecate", Guid.NewGuid());
        Assert.True(log.Timestamp >= before);
    }

    [Fact]
    public void StaffAuditLogEntryConfiguration_SetsTableName()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(StaffAuditLogEntry))!;
        Assert.Equal("staff_audit_log", entity.GetTableName());
    }

    [Fact]
    public void StaffAuditLogEntryConfiguration_SetsCorrectIndexes()
    {
        var model = BuildModel();
        var entity = model.FindEntityType(typeof(StaffAuditLogEntry))!;
        var indexes = entity.GetIndexes().Select(ix => ix.GetDatabaseName()).ToList();
        Assert.Contains("ix_staff_audit_log_catalog_entry_id", indexes);
        Assert.Contains("ix_staff_audit_log_timestamp", indexes);
    }

    [Fact]
    public void NullStaffAuditLogger_LogAsync_CompletesWithoutException()
    {
        var logger = NullStaffAuditLogger.Instance;
        var task = logger.LogAsync(Guid.NewGuid(), "Approve", Guid.NewGuid());
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public void NullStaffAuditLogger_LogSystemActorActivation_CompletesWithoutException()
    {
        var logger = NullStaffAuditLogger.Instance;
        var task = logger.LogSystemActorActivationAsync(Guid.NewGuid(), "system seed");
        Assert.True(task.IsCompletedSuccessfully);
    }

}
