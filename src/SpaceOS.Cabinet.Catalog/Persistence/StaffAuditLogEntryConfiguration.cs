namespace SpaceOS.Cabinet.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>EF Core configuration for <see cref="StaffAuditLogEntry"/>.</summary>
public sealed class StaffAuditLogEntryConfiguration : IEntityTypeConfiguration<StaffAuditLogEntry>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StaffAuditLogEntry> builder)
    {
        builder.ToTable("staff_audit_log");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.StaffUserId)
            .HasColumnName("staff_user_id")
            .IsRequired();

        builder.Property(e => e.Action)
            .HasColumnName("action")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CatalogEntryId)
            .HasColumnName("catalog_entry_id")
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(e => e.Details)
            .HasColumnName("details")
            .HasMaxLength(1000);

        builder.HasIndex(e => e.CatalogEntryId)
            .HasDatabaseName("ix_staff_audit_log_catalog_entry_id");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_staff_audit_log_timestamp");
    }
}
