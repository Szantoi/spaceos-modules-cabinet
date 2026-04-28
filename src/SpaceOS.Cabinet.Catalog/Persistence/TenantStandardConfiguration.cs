namespace SpaceOS.Cabinet.Catalog.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain;

/// <summary>
/// EF Core configuration for <see cref="TenantStandard"/>.
/// Registered by the consumer in their DbContext.
/// Owned entities (MaterialDefaults, LineBoreSettings, RuleThresholds) are mapped as embedded columns.
/// RuleSeverityOverrides is stored as JSONB.
/// </summary>
public sealed class TenantStandardConfiguration : IEntityTypeConfiguration<TenantStandard>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TenantStandard> builder)
    {
        builder.ToTable("tenant_standards");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        // Version: optimistic concurrency token
        builder.Property(e => e.Version)
            .HasColumnName("version")
            .IsConcurrencyToken();

        builder.Property(e => e.BackPanelAttachment)
            .HasColumnName("back_panel_attachment")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.TopType)
            .HasColumnName("top_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Owned: MaterialDefaults (embedded columns)
        builder.OwnsOne(e => e.Materials, m =>
        {
            m.Property(x => x.CarcassMaterial)
                .HasColumnName("carcass_material")
                .HasMaxLength(200)
                .IsRequired();

            m.Property(x => x.CarcassThicknessMm)
                .HasColumnName("carcass_thickness_mm")
                .IsRequired();

            m.Property(x => x.BackPanelMaterial)
                .HasColumnName("back_panel_material")
                .HasMaxLength(200)
                .IsRequired();

            m.Property(x => x.BackPanelThicknessMm)
                .HasColumnName("back_panel_thickness_mm")
                .IsRequired();
        });

        // Owned: LineBoreSettings (embedded columns)
        builder.OwnsOne(e => e.LineBore, lb =>
        {
            lb.Property(x => x.Enabled)
                .HasColumnName("line_bore_enabled")
                .IsRequired();

            lb.Property(x => x.FirstHoleOffsetMm)
                .HasColumnName("first_hole_offset_mm")
                .IsRequired();

            lb.Property(x => x.SpacingMm)
                .HasColumnName("spacing_mm")
                .IsRequired();

            lb.Property(x => x.DiameterMm)
                .HasColumnName("diameter_mm")
                .IsRequired();
        });

        // Owned: RuleThresholds (embedded columns)
        builder.OwnsOne(e => e.Thresholds, t =>
        {
            t.Property(x => x.TallCabinetHeightMm)
                .HasColumnName("tall_cabinet_height_mm")
                .IsRequired();

            t.Property(x => x.LongShelfMm)
                .HasColumnName("long_shelf_mm")
                .IsRequired();
        });

        // RuleSeverityOverrides: dictionary stored as JSONB
        builder.Property<Dictionary<string, AdvisorySeverity>>("_ruleSeverityOverrides")
            .HasColumnName("rule_severity_overrides")
            .HasColumnType("jsonb")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("ix_tenant_standards_tenant_id");
    }
}
