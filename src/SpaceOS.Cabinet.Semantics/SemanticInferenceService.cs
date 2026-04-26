using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;

// ICatalogResolver is in SpaceOS.Cabinet.Abstractions

namespace SpaceOS.Cabinet.Semantics;

/// <summary>
/// Infers semantic roles for cabinet parts using gravity-vector analysis (spec §7.2, axiom A7).
/// Results are cached by skeleton version — a new version invalidates all prior inferences for that skeleton.
/// </summary>
public sealed class SemanticInferenceService
{
    private readonly SemanticInferenceCache _cache;

    /// <summary>
    /// Initialises the service with an optional external cache.
    /// If <paramref name="cache"/> is <c>null</c>, a new default cache is created.
    /// </summary>
    /// <param name="cache">Shared cache instance, or <c>null</c> to use a private cache.</param>
    public SemanticInferenceService(SemanticInferenceCache? cache = null)
    {
        _cache = cache ?? new SemanticInferenceCache();
    }

    /// <summary>
    /// Infers the semantic role of a single part within its skeleton.
    /// Uses gravity + position analysis per spec §7.2.
    /// Results are cached by (SkeletonVersion, PartId).
    /// </summary>
    /// <param name="part">The part whose role is to be inferred.</param>
    /// <param name="skeleton">The skeleton that owns the part.</param>
    /// <returns>The inferred <see cref="PartRole"/>.</returns>
    public PartRole InferRole(Part part, Skeleton skeleton)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(skeleton);

        var cached = _cache.TryGet(skeleton.Version, part.Id);
        if (cached.HasValue)
            return cached.Value;

        var role = ComputeRole(part, skeleton);
        _cache.Set(skeleton.Version, part.Id, role);
        return role;
    }

    /// <summary>
    /// Infers roles for all parts in the skeleton and returns a part-id-keyed dictionary.
    /// Complexity is O(N) with cache hits; O(N²) on cold start due to position comparisons.
    /// Acceptable per BE-CAB-1: MaxParts=500, &lt;100 ms worst case.
    /// </summary>
    /// <param name="skeleton">The skeleton whose parts are to be classified.</param>
    /// <returns>A read-only dictionary mapping each part ID to its inferred role.</returns>
    public IReadOnlyDictionary<Guid, PartRole> InferAll(Skeleton skeleton)
    {
        ArgumentNullException.ThrowIfNull(skeleton);

        var result = new Dictionary<Guid, PartRole>(skeleton.Parts.Count);
        foreach (var part in skeleton.Parts)
            result[part.Id] = InferRole(part, skeleton);

        return result;
    }

    /// <summary>
    /// Infers roles for all parts using an optional catalog resolver for A12 catalog defaults.
    /// The resolver is an extension point: pinned catalog entries at Curated level can
    /// override the computed role in a future Cabinet 0.3+ enhancement.
    /// For Cabinet 0.2, this overload delegates to the geometry-based inference.
    /// </summary>
    /// <param name="skeleton">The skeleton whose parts are to be classified. Must not be null.</param>
    /// <param name="catalogResolver">Optional catalog resolver. Accepted for API compatibility; not yet used for role override.</param>
    /// <returns>A read-only dictionary mapping each part ID to its inferred role.</returns>
    public IReadOnlyDictionary<Guid, PartRole> InferAll(Skeleton skeleton, ICatalogResolver? catalogResolver)
    {
        ArgumentNullException.ThrowIfNull(skeleton);

        var result = new Dictionary<Guid, PartRole>(skeleton.Parts.Count);
        foreach (var part in skeleton.Parts)
            result[part.Id] = InferRole(part, skeleton);

        return result;
    }

    // ── Private inference logic ──────────────────────────────────────────────

    private static PartRole ComputeRole(Part part, Skeleton skeleton)
    {
        // A12: user-assigned role always takes precedence.
        if (part.AssignedRole.HasValue)
            return part.AssignedRole.Value;

        // Extract normal and datum — both return Result<Vector3> because the transform may
        // theoretically be invalid after a broken deserialization path.
        var normalResult = part.Frame.NormalInAssembly();
        var datumResult = part.Frame.DatumInAssembly();

        if (!normalResult.IsSuccess || !datumResult.IsSuccess)
            return PartRole.Unknown;

        Vector3 normal = normalResult.Value;
        Vector3 gravity = GravityVector.Default;
        Vector3 datum = datumResult.Value;
        AssemblyDimension dim = skeleton.Dimension;

        double dotNG = normal.Dot(gravity);
        double eps = GeometryConstants.DimensionEpsilon; // 1e-3 mm — adequate for panel alignment

        // ── Horizontal panel: normal ∥ gravity  (dot ≈ ±1) ───────────────────
        // Normal points roughly in the Z direction (up or down).
        if (Math.Abs(Math.Abs(dotNG) - 1.0) < eps)
            return ClassifyHorizontal(datum, dim, part.Frame.Dimension.Thickness, eps);

        // ── Vertical panel: normal ⊥ gravity (|dot| ≈ 0) ─────────────────────
        // Subdivide by which world axis the normal aligns with.
        if (Math.Abs(dotNG) < eps)
            return ClassifyVertical(normal, datum, dim, part.Frame.Dimension.Thickness, eps, part, skeleton);

        // Tilted / angled part — cannot be classified by simple axis analysis.
        return PartRole.Unknown;
    }

    private static PartRole ClassifyHorizontal(
        Vector3 datum, AssemblyDimension dim, double thickness, double eps)
    {
        // Bottom: datum at Z=0
        if (Math.Abs(datum.Z) < eps)
            return PartRole.Bottom;

        // Top: datum at Z = Height - thickness  (or at Z = Height for flush-top)
        if (Math.Abs(datum.Z - (dim.Height - thickness)) < eps ||
            Math.Abs(datum.Z - dim.Height) < eps)
            return PartRole.Top;

        // Interior horizontal position — shelf
        if (datum.Z > eps && datum.Z < dim.Height - eps)
            return PartRole.Shelf;

        return PartRole.Unknown;
    }

    private static PartRole ClassifyVertical(
        Vector3 normal, Vector3 datum, AssemblyDimension dim, double thickness,
        double eps, Part part, Skeleton skeleton)
    {
        // Determine which world axis the surface normal predominantly aligns with.
        double absNX = Math.Abs(normal.X);
        double absNY = Math.Abs(normal.Y);

        // ── Normal mainly along X → left / right side panel or interior divider ─
        if (absNX > absNY && absNX > eps)
        {
            // Left side: datum at X=0 with no parts further left
            if (Math.Abs(datum.X) < eps)
            {
                bool hasPartFurtherLeft = skeleton.Parts.Any(p =>
                    p.Id != part.Id && IsXNormalVertical(p) && GetDatumX(p) < -eps);
                return hasPartFurtherLeft ? PartRole.VerticalDivider : PartRole.LeftSide;
            }

            // Right side: datum at or near X = Width - thickness
            if (Math.Abs(datum.X - (dim.Width - thickness)) < eps ||
                Math.Abs(datum.X - dim.Width) < eps)
                return PartRole.RightSide;

            // Interior: vertical divider
            if (datum.X > eps && datum.X < dim.Width - eps)
                return PartRole.VerticalDivider;

            return PartRole.Unknown;
        }

        // ── Normal mainly along Y → back panel or front panel ─────────────────
        if (absNY > absNX && absNY > eps)
        {
            // Back panel: datum at or near Y = Depth - thickness
            if (Math.Abs(datum.Y - (dim.Depth - thickness)) < eps ||
                Math.Abs(datum.Y - dim.Depth) < eps)
                return PartRole.BackPanel;

            // Front panel: datum at Y=0
            if (Math.Abs(datum.Y) < eps)
                return PartRole.Front;

            return PartRole.Unknown;
        }

        return PartRole.Unknown;
    }

    // ── Helpers for position-based checks ────────────────────────────────────

    private static bool IsXNormalVertical(Part p)
    {
        var normalResult = p.Frame.NormalInAssembly();
        if (!normalResult.IsSuccess)
            return false;
        Vector3 n = normalResult.Value;
        double dotNG = n.Dot(GravityVector.Default);
        return Math.Abs(dotNG) < GeometryConstants.DimensionEpsilon && Math.Abs(n.X) > Math.Abs(n.Y);
    }

    private static double GetDatumX(Part p)
    {
        var datumResult = p.Frame.DatumInAssembly();
        return datumResult.IsSuccess ? datumResult.Value.X : double.NaN;
    }
}
