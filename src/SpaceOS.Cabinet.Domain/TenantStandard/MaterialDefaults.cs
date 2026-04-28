namespace SpaceOS.Cabinet.Domain;

/// <summary>Immutable value object holding the default material specifications for a cabinet carcass.</summary>
public sealed record MaterialDefaults(
    string CarcassMaterial,
    double CarcassThicknessMm,
    string BackPanelMaterial,
    double BackPanelThicknessMm);

/// <summary>Immutable value object holding line-boring configuration defaults.</summary>
public sealed record LineBoreSettings(
    bool Enabled,
    double FirstHoleOffsetMm,
    double SpacingMm,
    double DiameterMm);

/// <summary>Immutable value object holding advisory rule threshold defaults.</summary>
public sealed record RuleThresholds(
    double TallCabinetHeightMm,
    double LongShelfMm);
