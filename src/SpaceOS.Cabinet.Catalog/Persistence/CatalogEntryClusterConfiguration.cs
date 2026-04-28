namespace SpaceOS.Cabinet.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for <see cref="CatalogEntryCluster"/>.
/// Registered by the consumer in their DbContext.
/// MemberEntryIds is stored as a JSONB array of GUIDs.
/// </summary>
public sealed class CatalogEntryClusterConfiguration : IEntityTypeConfiguration<CatalogEntryCluster>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CatalogEntryCluster> builder)
    {
        builder.ToTable("catalog_entry_clusters");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Fingerprint)
            .HasColumnName("fingerprint")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CanonicalEntryId)
            .HasColumnName("canonical_entry_id")
            .IsRequired();

        builder.Property(e => e.IsRemoved)
            .HasColumnName("is_removed")
            .IsRequired();

        // MemberEntryIds: List<Guid> stored as JSONB
        builder.Property<List<Guid>>("_memberEntryIds")
            .HasColumnName("member_entry_ids")
            .HasColumnType("jsonb")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Version: optimistic concurrency token
        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(e => e.Fingerprint)
            .HasDatabaseName("ix_catalog_entry_clusters_fingerprint");

        builder.HasIndex(e => new { e.Fingerprint, e.Type })
            .HasDatabaseName("ix_catalog_entry_clusters_fingerprint_type");
    }
}
