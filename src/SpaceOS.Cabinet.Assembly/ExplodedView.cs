namespace SpaceOS.Cabinet.Assembly;

using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;

/// <summary>A layer in an exploded-view diagram grouping parts by assembly depth.</summary>
/// <param name="LayerIndex">Zero-based layer depth (0 = base parts, 1 = next level, etc.).</param>
/// <param name="PartIds">Parts belonging to this layer.</param>
public sealed record ExplodedViewLayer(int LayerIndex, IReadOnlyList<Guid> PartIds);

/// <summary>
/// An exploded-view representation of a cabinet skeleton, organizing parts into
/// ordered assembly layers for visual and instructional output.
/// </summary>
/// <param name="Layers">All layers in assembly order, from base outward.</param>
public sealed record ExplodedView(IReadOnlyList<ExplodedViewLayer> Layers);

/// <summary>
/// A hardware callout annotation for an exploded-view diagram.
/// Associates a hardware item with a target part and a display position.
/// </summary>
/// <param name="Hardware">The hardware item referenced.</param>
/// <param name="TargetPartId">The part this hardware attaches to.</param>
/// <param name="Position">3D position of the callout label in assembly space.</param>
/// <param name="CalloutLabel">Human-readable label text.</param>
public sealed record HardwareCallout(
    HardwareReference Hardware,
    Guid TargetPartId,
    Vector3 Position,
    string CalloutLabel)
{
    /// <summary>
    /// Returns <c>true</c> when all fields are populated and valid.
    /// Requires a valid hardware reference, non-empty target part ID, and a non-blank label.
    /// </summary>
    public bool IsValid() =>
        Hardware.IsValid() &&
        TargetPartId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(CalloutLabel);
}
