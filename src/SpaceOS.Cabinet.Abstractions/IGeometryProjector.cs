namespace SpaceOS.Cabinet.Abstractions;

/// <summary>
/// Projects cabinet geometry into an external representation (e.g. SVG, DXF, or a 3D viewport).
/// Implementations are platform-specific and live outside the core domain (A8).
/// This is a marker interface in Cabinet 0.1; full projection API is defined in Cabinet 0.2+.
/// </summary>
public interface IGeometryProjector { }
