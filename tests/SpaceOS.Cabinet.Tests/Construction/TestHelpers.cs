using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Tests.Construction;

/// <summary>
/// Test implementation of <see cref="ITenantStandardProvider"/> with sensible defaults.
/// </summary>
internal sealed class TestTenantStandardProvider : ITenantStandardProvider
{
    public Guid TenantId { get; init; } = Guid.NewGuid();
    public string DefaultCarcassMaterial { get; init; } = "lamiboard-18mm-white";
    public double DefaultCarcassThickness { get; init; } = 18.0;
    public string DefaultBackPanelMaterial { get; init; } = "hdf-5mm";
    public double DefaultBackPanelThickness { get; init; } = 5.0;
    public BackPanelAttachmentDefault BackPanelAttachment { get; init; } = BackPanelAttachmentDefault.Groove;
    public TopType TopType { get; init; } = TopType.FullTop;
    public bool LineBoreEnabled { get; init; } = true;
    public double LineBoreFirstHoleOffset { get; init; } = 38.0;
    public double LineBoreSpacing { get; init; } = 32.0;
    public double LineBoreDiameter { get; init; } = 5.0;
    public double TallCabinetHeightThreshold { get; init; } = 2000.0;
    public double LongShelfThreshold { get; init; } = 800.0;
    public IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides { get; init; }
        = new Dictionary<string, AdvisorySeverity>();
}

/// <summary>
/// Test implementation of <see cref="IConstructionContext"/> backed by
/// a <see cref="TestTenantStandardProvider"/> and a given assembly dimension.
/// </summary>
internal sealed class TestConstructionContext : IConstructionContext
{
    public ITenantStandardProvider TenantStandard { get; }
    public AssemblyDimension AssemblyDimension { get; }

    public TestConstructionContext(
        ITenantStandardProvider? tenantStandard = null,
        AssemblyDimension? assemblyDimension = null)
    {
        TenantStandard = tenantStandard ?? new TestTenantStandardProvider();
        AssemblyDimension = assemblyDimension ?? AssemblyDimension.Create(600, 720, 560).Value;
    }
}
