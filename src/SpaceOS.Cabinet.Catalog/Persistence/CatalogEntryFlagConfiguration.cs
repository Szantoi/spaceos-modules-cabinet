namespace SpaceOS.Cabinet.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for <see cref="CatalogEntryFlag"/>.
/// Registered by the consumer in their DbContext.
/// </summary>
public sealed class CatalogEntryFlagConfiguration : IEntityTypeConfiguration<CatalogEntryFlag>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CatalogEntryFlag> builder)
    {
        builder.ToTable("catalog_entry_flags");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.CatalogEntryId)
            .HasColumnName("catalog_entry_id")
            .IsRequired();

        builder.Property(e => e.ReporterTenantId)
            .HasColumnName("reporter_tenant_id")
            .IsRequired();

        builder.Property(e => e.ReporterUserId)
            .HasColumnName("reporter_user_id")
            .IsRequired();

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(1000);

        builder.Property(e => e.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(e => e.ResolvedByUserId)
            .HasColumnName("resolved_by_user_id");

        builder.HasIndex(e => e.CatalogEntryId)
            .HasDatabaseName("ix_catalog_entry_flags_entry_id");

        builder.HasIndex(e => new { e.CatalogEntryId, e.State })
            .HasDatabaseName("ix_catalog_entry_flags_entry_state");
    }
}
