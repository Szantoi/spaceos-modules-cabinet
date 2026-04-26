namespace SpaceOS.Cabinet.Catalog;

/// <summary>
/// Provides the 16 curated seed entry definitions (2 per <see cref="CatalogType"/>).
/// These are created at deploy-time by migration and used as the Curated fallback layer.
/// </summary>
public static class SystemCatalogSeeds
{
    /// <summary>
    /// All 16 curated seed definitions. Each tuple contains the type, display name,
    /// schema version, and pre-validated payload JSON.
    /// </summary>
    public static IReadOnlyList<(CatalogType Type, string Name, string SchemaVersion, string PayloadJson)> All { get; } =
        new List<(CatalogType, string, string, string)>
        {
            (CatalogType.HorizontalRole,      "Default Shelf Role",           "horizontalRole/v1",       """{"role":"Shelf","priority":1}"""),
            (CatalogType.HorizontalRole,      "Default CrossRail Role",       "horizontalRole/v1",       """{"role":"CrossRail","priority":2}"""),
            (CatalogType.MaterialThickness,   "18mm Particleboard",           "materialThickness/v1",    """{"value":18,"unit":"mm","material":"Particleboard"}"""),
            (CatalogType.MaterialThickness,   "25mm MDF",                     "materialThickness/v1",    """{"value":25,"unit":"mm","material":"MDF"}"""),
            (CatalogType.JointType,           "Default FaceEdgeButt",         "jointType/v1",            """{"type":"FaceEdgeButt"}"""),
            (CatalogType.JointType,           "Default Dado",                 "jointType/v1",            """{"type":"Dado"}"""),
            (CatalogType.EdgeBandingRule,     "Front Edges 2mm ABS",          "edgeBandingRule/v1",      """{"surfaces":["Front","SideExposed"],"thickness":2}"""),
            (CatalogType.EdgeBandingRule,     "All Edges 0.5mm PVC",          "edgeBandingRule/v1",      """{"surfaces":["Front","Back","SideExposed","SideHidden"],"thickness":0.5}"""),
            (CatalogType.HardwareSet,         "Blum Hinges + KFV Pins",       "hardwareSet/v1",          """{"hinges":["BLUM_71B3550"],"shelfPins":["KFV_5MM"]}"""),
            (CatalogType.HardwareSet,         "Hettich Hinges + Hafele Pins", "hardwareSet/v1",          """{"hinges":["HETTICH_9071204"],"shelfPins":["HAFELE_282.49"]}"""),
            (CatalogType.BackPanelStandard,   "4mm HDF Groove",               "backPanelStandard/v1",    """{"thickness":4,"attachment":"Groove","material":"HDF"}"""),
            (CatalogType.BackPanelStandard,   "5mm HDF Rabbet",               "backPanelStandard/v1",    """{"thickness":5,"attachment":"Rabbet","material":"HDF"}"""),
            (CatalogType.RasterStandard,      "32mm System Standard",         "rasterStandard/v1",       """{"pitch":32,"firstHole":38,"holeDiameter":5}"""),
            (CatalogType.RasterStandard,      "25mm System Compact",          "rasterStandard/v1",       """{"pitch":25,"firstHole":25,"holeDiameter":5}"""),
            (CatalogType.ConstructionTemplate,"Standard Cabinet Rules",        "constructionTemplate/v1", """{"rules":["R-32mm-LineBore","R-Default-Joint","R-BackPanel-Hidden","R-EdgeBand-FrontVisible"]}"""),
            (CatalogType.ConstructionTemplate,"Minimal Cabinet Rules",         "constructionTemplate/v1", """{"rules":["R-Default-Joint","R-EdgeBand-FrontVisible"]}"""),
        };
}
