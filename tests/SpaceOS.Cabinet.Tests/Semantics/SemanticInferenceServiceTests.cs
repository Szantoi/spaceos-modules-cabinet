using System.Reflection;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Semantics;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Semantics;

public class SemanticInferenceServiceTests
{
    // Standard 600×720×560 cabinet (W×H×D), 18 mm carcass thickness
    private const double Width = 600;
    private const double Height = 720;
    private const double Depth = 560;
    private const double Thickness = 18;

    private static AssemblyDimension Dim()
        => AssemblyDimension.Create(Width, Height, Depth).Value;

    private static Skeleton CreateSkeleton()
        => Skeleton.Create(Guid.NewGuid(), Dim()).Value;

    // Translate + optional rotation helper
    private static PartFrame FrameWithTranslation(double x, double y, double z,
        double length, double width, double thickness)
    {
        var offset = Vector3.Create(x, y, z).Value;
        var transform = AffineTransform.Translation(offset).Value;
        var dim = PartDimension.Create(length, width, thickness).Value;
        return PartFrame.Create(transform, dim).Value;
    }

    // Creates a frame with a Y-axis 90° rotation (local Z → world X; for vertical X-normal panels)
    private static PartFrame FrameRotatedAroundY90(double x, double y, double z,
        double length, double width, double thickness)
    {
        var rotation = AffineTransform.Rotation(Vector3.UnitY, Math.PI / 2.0).Value;
        var translation = AffineTransform.Translation(Vector3.Create(x, y, z).Value).Value;
        var composed = AffineTransform.Compose(rotation, translation).Value;
        var dim = PartDimension.Create(length, width, thickness).Value;
        return PartFrame.Create(composed, dim).Value;
    }

    // Creates a frame with a X-axis 90° rotation (local Z → world Y; for vertical Y-normal panels)
    private static PartFrame FrameRotatedAroundX90(double x, double y, double z,
        double length, double width, double thickness)
    {
        var rotation = AffineTransform.Rotation(Vector3.UnitX, -Math.PI / 2.0).Value;
        var translation = AffineTransform.Translation(Vector3.Create(x, y, z).Value).Value;
        var composed = AffineTransform.Compose(rotation, translation).Value;
        var dim = PartDimension.Create(length, width, thickness).Value;
        return PartFrame.Create(composed, dim).Value;
    }

    private static void AssignRole(Part part, PartRole role)
    {
        var method = typeof(Part).GetMethod("AssignRole",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(part, [role]);
    }

    // ── A12: AssignedRole overrides inference ─────────────────────────────────

    [Fact]
    public void InferRole_AssignedRole_OverridesGeometryInference()
    {
        var skeleton = CreateSkeleton();
        // Add a part with an identity transform (would infer Bottom at Z=0)
        var frame = FrameWithTranslation(0, 0, 0, 200, 560, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;
        AssignRole(part, PartRole.Shelf);

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Shelf, svc.InferRole(part, skeleton));
    }

    // ── BaseCuboid parts — returned via A12 path ──────────────────────────────

    [Fact]
    public void InferRole_BaseCuboidLeftSide_ReturnsLeftSide()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.LeftSide, svc.InferRole(skeleton.BaseCuboid.LeftSide, skeleton));
    }

    [Fact]
    public void InferRole_BaseCuboidRightSide_ReturnsRightSide()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.RightSide, svc.InferRole(skeleton.BaseCuboid.RightSide, skeleton));
    }

    [Fact]
    public void InferRole_BaseCuboidBottom_ReturnsBottom()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Bottom, svc.InferRole(skeleton.BaseCuboid.Bottom, skeleton));
    }

    [Fact]
    public void InferRole_BaseCuboidTop_ReturnsTop()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Top, svc.InferRole(skeleton.BaseCuboid.Top, skeleton));
    }

    // ── Geometry-based inference: horizontal panels ───────────────────────────
    // Parts with Identity/Translation transform have normal = (0,0,1), dot(gravity) = -1 → horizontal.

    [Fact]
    public void InferRole_InteriorHorizontalPart_ReturnsShelf()
    {
        var skeleton = CreateSkeleton();
        // Shelf at Z=360 (mid-height interior), using Translation — no AssignedRole
        var frame = FrameWithTranslation(Thickness, 0, 360, Width - 2 * Thickness, Depth, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Shelf, svc.InferRole(part, skeleton));
    }

    [Fact]
    public void InferRole_HorizontalPartAtBottomZ_ReturnsBottom()
    {
        var skeleton = CreateSkeleton();
        // Part with datum at Z=0 and no assigned role → Bottom
        var frame = FrameWithTranslation(Thickness, 0, 0, Width - 2 * Thickness, Depth, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Bottom, svc.InferRole(part, skeleton));
    }

    [Fact]
    public void InferRole_HorizontalPartAtTopZ_ReturnsTop()
    {
        var skeleton = CreateSkeleton();
        // Part with datum at Z = Height - Thickness → Top
        var frame = FrameWithTranslation(Thickness, 0, Height - Thickness, Width - 2 * Thickness, Depth, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Top, svc.InferRole(part, skeleton));
    }

    // ── Geometry-based inference: vertical panels (rotated) ──────────────────

    [Fact]
    public void InferRole_InteriorVerticalPart_RotatedAroundY_ReturnsVerticalDivider()
    {
        var skeleton = CreateSkeleton();
        // Rotate 90° around Y so that local Z → world X; datum at interior X → VerticalDivider
        double dividerX = Width / 2.0;
        var frame = FrameRotatedAroundY90(dividerX, 0, 0, Height, Depth, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.VerticalDivider, svc.InferRole(part, skeleton));
    }

    [Fact]
    public void InferRole_VerticalPartAtX0_RotatedAroundY_ReturnsLeftSide()
    {
        var skeleton = CreateSkeleton();
        // Rotated panel with datum at X=0 and no parts further left → LeftSide
        var frame = FrameRotatedAroundY90(0, 0, 0, Height, Depth, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.LeftSide, svc.InferRole(part, skeleton));
    }

    [Fact]
    public void InferRole_BackPanelPosition_ReturnsBackPanel()
    {
        var skeleton = CreateSkeleton();
        // Rotated 90° around X so local Z → world Y; datum at Y = Depth - Thickness → BackPanel
        var frame = FrameRotatedAroundX90(0, Depth - Thickness, 0, Width, Height, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.BackPanel, svc.InferRole(part, skeleton));
    }

    [Fact]
    public void InferRole_FrontPosition_ReturnsFront()
    {
        var skeleton = CreateSkeleton();
        // Rotated 90° around X so local Z → world Y; datum at Y=0 → Front
        var frame = FrameRotatedAroundX90(0, 0, 0, Width, Height, Thickness);
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Front, svc.InferRole(part, skeleton));
    }

    [Fact]
    public void InferRole_TiltedPart_ReturnsUnknown()
    {
        var skeleton = CreateSkeleton();
        // 45° rotation around Y → normal has both X and Z components; |dot| ≈ 0.7 — neither ≈0 nor ≈1
        var rotation = AffineTransform.Rotation(Vector3.UnitY, Math.PI / 4.0).Value;
        var dim = PartDimension.Create(200, 560, Thickness).Value;
        var frame = PartFrame.Create(rotation, dim).Value;
        var part = skeleton.AddPart(frame, "mat").Value;

        var svc = new SemanticInferenceService();

        Assert.Equal(PartRole.Unknown, svc.InferRole(part, skeleton));
    }

    // ── InferAll ─────────────────────────────────────────────────────────────

    [Fact]
    public void InferAll_ReturnsDictionaryWithAllPartIds()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        var result = svc.InferAll(skeleton);

        Assert.Equal(skeleton.Parts.Count, result.Count);
        foreach (var part in skeleton.Parts)
            Assert.True(result.ContainsKey(part.Id));
    }

    [Fact]
    public void InferAll_BaseCuboidParts_HaveCorrectRoles()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        var result = svc.InferAll(skeleton);

        Assert.Equal(PartRole.LeftSide, result[skeleton.BaseCuboid.LeftSide.Id]);
        Assert.Equal(PartRole.RightSide, result[skeleton.BaseCuboid.RightSide.Id]);
        Assert.Equal(PartRole.Bottom, result[skeleton.BaseCuboid.Bottom.Id]);
        Assert.Equal(PartRole.Top, result[skeleton.BaseCuboid.Top.Id]);
    }

    // ── Caching behaviour ─────────────────────────────────────────────────────

    [Fact]
    public void InferRole_CalledTwice_ReturnsSameResult()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        var first = svc.InferRole(skeleton.BaseCuboid.Bottom, skeleton);
        var second = svc.InferRole(skeleton.BaseCuboid.Bottom, skeleton);

        Assert.Equal(first, second);
    }

    [Fact]
    public void InferAll_CacheHit_PopulatesCache()
    {
        var cache = new SemanticInferenceCache();
        var svc = new SemanticInferenceService(cache);
        var skeleton = CreateSkeleton();

        svc.InferAll(skeleton);

        // All parts should now be in the cache under the current version
        foreach (var part in skeleton.Parts)
        {
            var cached = cache.TryGet(skeleton.Version, part.Id);
            Assert.NotNull(cached);
        }
    }

    // ── Null argument guards ──────────────────────────────────────────────────

    [Fact]
    public void InferRole_NullPart_ThrowsArgumentNullException()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        Assert.Throws<ArgumentNullException>(() => svc.InferRole(null!, skeleton));
    }

    [Fact]
    public void InferRole_NullSkeleton_ThrowsArgumentNullException()
    {
        var skeleton = CreateSkeleton();
        var svc = new SemanticInferenceService();

        Assert.Throws<ArgumentNullException>(() => svc.InferRole(skeleton.BaseCuboid.Bottom, null!));
    }

    [Fact]
    public void InferAll_NullSkeleton_ThrowsArgumentNullException()
    {
        var svc = new SemanticInferenceService();

        Assert.Throws<ArgumentNullException>(() => svc.InferAll(null!));
    }
}
