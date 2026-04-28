namespace SpaceOS.Cabinet.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// EF Core configuration for <see cref="CatalogEntryRating"/>.
/// Registered by the consumer in their DbContext.
/// </summary>
public sealed class CatalogEntryRatingConfiguration : IEntityTypeConfiguration<CatalogEntryRating>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CatalogEntryRating> builder)
    {
        builder.ToTable("catalog_entry_ratings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.CatalogEntryId)
            .HasColumnName("catalog_entry_id")
            .IsRequired();

        builder.Property(e => e.RaterTenantId)
            .HasColumnName("rater_tenant_id")
            .IsRequired();

        builder.Property(e => e.RaterUserId)
            .HasColumnName("rater_user_id")
            .IsRequired();

        builder.Property(e => e.Stars)
            .HasColumnName("stars")
            .IsRequired();

        builder.Property(e => e.Comment)
            .HasColumnName("comment")
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraint: one rating per (entry, rater tenant) pair
        builder.HasIndex(e => new { e.CatalogEntryId, e.RaterTenantId })
            .IsUnique()
            .HasDatabaseName("ix_catalog_entry_ratings_entry_rater");

        builder.HasIndex(e => e.CatalogEntryId)
            .HasDatabaseName("ix_catalog_entry_ratings_entry_id");
    }
}
