namespace SpaceOS.Cabinet.Abstractions;

/// <summary>Discriminator for the type of standard a catalog entry represents.</summary>
public enum CatalogType
{
    /// <summary>Horizontal member role assignment (shelf, cross-rail, etc.).</summary>
    HorizontalRole,

    /// <summary>Material thickness standard.</summary>
    MaterialThickness,

    /// <summary>Joint type between two parts.</summary>
    JointType,

    /// <summary>Edge banding rule for exposed edges.</summary>
    EdgeBandingRule,

    /// <summary>Hardware set (hinges, shelf pins, etc.).</summary>
    HardwareSet,

    /// <summary>Back panel construction standard.</summary>
    BackPanelStandard,

    /// <summary>32mm / raster system drilling standard.</summary>
    RasterStandard,

    /// <summary>Construction rule template (a named set of rule references).</summary>
    ConstructionTemplate
}
