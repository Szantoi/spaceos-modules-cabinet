namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Provides access to tenant-specific part catalog entries (hardware, carcass materials, etc.).
/// Implementations resolve catalog references used on <c>Part.PartCatalogReference</c>.
/// This is a marker interface in Cabinet 0.1; full catalog lookup API is defined in Cabinet 0.2+.
/// </summary>
public interface IPartCatalog { }
