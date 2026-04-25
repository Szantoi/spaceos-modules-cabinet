using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Construction;

/// <summary>
/// Contextual data available to all <see cref="IConstructionRule"/> implementations during execution.
/// Provides tenant-specific standards and the current assembly dimension.
/// </summary>
public interface IConstructionContext
{
    /// <summary>Tenant-specific defaults (material, line-bore config, back-panel attachment, etc.).</summary>
    ITenantStandardProvider TenantStandard { get; }

    /// <summary>Outer dimensions of the cabinet being evaluated.</summary>
    AssemblyDimension AssemblyDimension { get; }
}
