namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Provides tenant-specific default standards and configuration for cabinet construction.
/// Implementations are registered per-tenant and injected into rule engines and services.
/// </summary>
public interface ITenantStandardProvider
{
    /// <summary>Unique identifier of the tenant this provider applies to.</summary>
    Guid TenantId { get; }

    /// <summary>Default carcass material reference key (e.g. SKU or material code).</summary>
    string DefaultCarcassMaterial { get; }

    /// <summary>Default carcass panel thickness in millimetres.</summary>
    double DefaultCarcassThickness { get; }

    /// <summary>Default back panel material reference key.</summary>
    string DefaultBackPanelMaterial { get; }

    /// <summary>Default back panel thickness in millimetres.</summary>
    double DefaultBackPanelThickness { get; }

    /// <summary>How the back panel is attached to the carcass by default.</summary>
    BackPanelAttachmentDefault BackPanelAttachment { get; }

    /// <summary>Construction type for the top of the cabinet.</summary>
    TopType TopType { get; }

    /// <summary>Whether line boring is enabled for this tenant.</summary>
    bool LineBoreEnabled { get; }

    /// <summary>Distance from the datum edge to the first line-bore hole centre in millimetres.</summary>
    double LineBoreFirstHoleOffset { get; }

    /// <summary>Spacing between consecutive line-bore holes in millimetres (typically 32 mm).</summary>
    double LineBoreSpacing { get; }

    /// <summary>Diameter of line-bore holes in millimetres.</summary>
    double LineBoreDiameter { get; }

    /// <summary>Height threshold in millimetres above which a cabinet is classified as "tall".</summary>
    double TallCabinetHeightThreshold { get; }

    /// <summary>Shelf length threshold in millimetres above which a shelf is considered "long" (sag risk).</summary>
    double LongShelfThreshold { get; }

    /// <summary>
    /// Per-rule severity overrides keyed by rule identifier.
    /// Allows tenants to escalate or downgrade specific advisory rules.
    /// </summary>
    IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides { get; }
}
