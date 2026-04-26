namespace SpaceOS.Cabinet.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for <see cref="CatalogEntry"/>.
/// Registered by the consumer (Kernel, CabinetBuilder) in their DbContext.
/// EF Core 8 maps private setters natively — no field access mode override required.
/// </summary>
public sealed class CatalogEntryConfiguration : IEntityTypeConfiguration<CatalogEntry>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CatalogEntry> builder)
    {
        builder.ToTable("catalog_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Visibility)
            .HasColumnName("visibility")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // JSONB column + 64 KB CHECK constraint (SEC-CAB02-5)
        builder.Property(e => e.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_catalog_entries_payload_size",
            "octet_length(payload_json::text) <= 65536"));

        builder.Property(e => e.PayloadSchemaVersion)
            .HasColumnName("payload_schema_version")
            .HasMaxLength(100)
            .IsRequired();

        // ContentHash: SHA-256 hex = 64 characters, immutable after Published
        builder.Property(e => e.ContentHash)
            .HasColumnName("content_hash")
            .HasMaxLength(64)
            .IsRequired();

        // Version: optimistic concurrency token (DB-CAB02-2)
        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(e => e.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(e => e.DeprecatedAt)
            .HasColumnName("deprecated_at");

        // Composite index: (TenantId, Visibility, Type, State) — supports resolution queries
        builder.HasIndex(e => new { e.TenantId, e.Visibility, e.Type, e.State })
            .HasDatabaseName("ix_catalog_entries_tenant_visibility_type_state");

        builder.HasIndex(e => e.ContentHash)
            .HasDatabaseName("ix_catalog_entries_content_hash");
    }
}
