namespace SpaceOS.Cabinet.Catalog.Payloads;

/// <summary>
/// Registry that maps <c>(CatalogType, schemaVersion)</c> pairs to their DTO <see cref="Type"/>.
/// Used by <see cref="CatalogPayloadValidator"/> to perform schema-aware deserialization.
/// </summary>
public static class CatalogPayloadSchemas
{
    private static readonly IReadOnlyDictionary<(CatalogType, string), Type> Map =
        new Dictionary<(CatalogType, string), Type>
        {
            [(CatalogType.HorizontalRole,      "horizontalRole/v1")]      = typeof(HorizontalRolePayloadV1),
            [(CatalogType.MaterialThickness,   "materialThickness/v1")]   = typeof(MaterialThicknessPayloadV1),
            [(CatalogType.JointType,           "jointType/v1")]           = typeof(JointTypePayloadV1),
            [(CatalogType.EdgeBandingRule,     "edgeBandingRule/v1")]     = typeof(EdgeBandingRulePayloadV1),
            [(CatalogType.HardwareSet,         "hardwareSet/v1")]         = typeof(HardwareSetPayloadV1),
            [(CatalogType.BackPanelStandard,   "backPanelStandard/v1")]   = typeof(BackPanelStandardPayloadV1),
            [(CatalogType.RasterStandard,      "rasterStandard/v1")]      = typeof(RasterStandardPayloadV1),
            [(CatalogType.ConstructionTemplate,"constructionTemplate/v1")]= typeof(ConstructionTemplatePayloadV1),
        };

    /// <summary>
    /// Returns the DTO <see cref="Type"/> for the given type/schema pair, or <c>null</c> if unknown.
    /// </summary>
    public static Type? GetDtoType(CatalogType type, string schemaVersion)
        => Map.TryGetValue((type, schemaVersion), out var dto) ? dto : null;

    /// <summary>All registered schema mappings.</summary>
    public static IReadOnlyDictionary<(CatalogType, string), Type> All => Map;
}
