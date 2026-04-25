using Ardalis.Result;
using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// The mandatory four-sided (plus optional back panel) carcass structure.
/// Acts as the structural root of a <see cref="Skeleton"/> (A3).
/// </summary>
public sealed class BaseCuboid
{
    /// <summary>Left carcass side panel.</summary>
    public Part LeftSide { get; private set; }

    /// <summary>Right carcass side panel.</summary>
    public Part RightSide { get; private set; }

    /// <summary>Bottom (floor) panel.</summary>
    public Part Bottom { get; private set; }

    /// <summary>Top panel or cross-rail assembly.</summary>
    public Part Top { get; private set; }

    /// <summary>Optional back panel. <c>null</c> if not yet added.</summary>
    public Part? BackPanel { get; private set; }

    private BaseCuboid(Part left, Part right, Part bottom, Part top, Part? backPanel)
    {
        LeftSide = left;
        RightSide = right;
        Bottom = bottom;
        Top = top;
        BackPanel = backPanel;
    }

    /// <summary>
    /// Creates the four mandatory carcass parts based on the given assembly dimensions.
    /// All parts use the provided <paramref name="carcassThickness"/> as panel thickness.
    /// </summary>
    internal static Result<BaseCuboid> CreateDefault(Guid skeletonId, AssemblyDimension dim, double carcassThickness)
    {
        // Left side: at X=0, Height × Depth × thickness
        var leftDimResult = PartDimension.Create(dim.Height, dim.Depth, carcassThickness);
        if (!leftDimResult.IsSuccess)
            return Result<BaseCuboid>.Error($"Left side dimension invalid: {string.Join(", ", leftDimResult.ValidationErrors.Select(e => e.ErrorMessage))}");

        var leftFrameResult = PartFrame.Create(AffineTransform.Identity, leftDimResult.Value);
        if (!leftFrameResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create left side frame.");
        var left = new Part(Guid.NewGuid(), skeletonId, leftFrameResult.Value, "default-carcass", "base-left");
        left.AssignRole(Abstractions.PartRole.LeftSide);

        // Right side: at X = Width − thickness
        var rightDimResult = PartDimension.Create(dim.Height, dim.Depth, carcassThickness);
        if (!rightDimResult.IsSuccess)
            return Result<BaseCuboid>.Error("Right side dimension invalid.");

        var rightOffsetResult = Vector3.Create(dim.Width - carcassThickness, 0, 0);
        if (!rightOffsetResult.IsSuccess)
            return Result<BaseCuboid>.Error("Invalid right side offset.");

        var rightTranslationResult = AffineTransform.Translation(rightOffsetResult.Value);
        if (!rightTranslationResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create right side transform.");

        var rightFrameResult = PartFrame.Create(rightTranslationResult.Value, rightDimResult.Value);
        if (!rightFrameResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create right side frame.");
        var right = new Part(Guid.NewGuid(), skeletonId, rightFrameResult.Value, "default-carcass", "base-right");
        right.AssignRole(Abstractions.PartRole.RightSide);

        // Bottom: sits between the two sides at Z=0
        double innerWidth = Math.Max(dim.Width - 2 * carcassThickness, PartDimension.MinDimension);
        var bottomDimResult = PartDimension.Create(innerWidth, dim.Depth, carcassThickness);
        if (!bottomDimResult.IsSuccess)
            return Result<BaseCuboid>.Error("Bottom dimension invalid.");

        var bottomOffsetResult = Vector3.Create(carcassThickness, 0, 0);
        if (!bottomOffsetResult.IsSuccess)
            return Result<BaseCuboid>.Error("Invalid bottom offset.");

        var bottomTranslationResult = AffineTransform.Translation(bottomOffsetResult.Value);
        if (!bottomTranslationResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create bottom transform.");

        var bottomFrameResult = PartFrame.Create(bottomTranslationResult.Value, bottomDimResult.Value);
        if (!bottomFrameResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create bottom frame.");
        var bottom = new Part(Guid.NewGuid(), skeletonId, bottomFrameResult.Value, "default-carcass", "base-bottom");
        bottom.AssignRole(Abstractions.PartRole.Bottom);

        // Top: sits between the two sides at Z = Height − thickness
        var topDimResult = PartDimension.Create(innerWidth, dim.Depth, carcassThickness);
        if (!topDimResult.IsSuccess)
            return Result<BaseCuboid>.Error("Top dimension invalid.");

        var topOffsetResult = Vector3.Create(carcassThickness, 0, dim.Height - carcassThickness);
        if (!topOffsetResult.IsSuccess)
            return Result<BaseCuboid>.Error("Invalid top offset.");

        var topTranslationResult = AffineTransform.Translation(topOffsetResult.Value);
        if (!topTranslationResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create top transform.");

        var topFrameResult = PartFrame.Create(topTranslationResult.Value, topDimResult.Value);
        if (!topFrameResult.IsSuccess)
            return Result<BaseCuboid>.Error("Failed to create top frame.");
        var top = new Part(Guid.NewGuid(), skeletonId, topFrameResult.Value, "default-carcass", "base-top");
        top.AssignRole(Abstractions.PartRole.Top);

        return Result<BaseCuboid>.Success(new BaseCuboid(left, right, bottom, top, null));
    }

    /// <summary>Sets or clears the back panel.</summary>
    internal void SetBackPanel(Part? backPanel) => BackPanel = backPanel;

    /// <summary>Enumerates all parts that form the base cuboid (including back panel if present).</summary>
    internal IEnumerable<Part> GetAllParts()
    {
        yield return LeftSide;
        yield return RightSide;
        yield return Bottom;
        yield return Top;
        if (BackPanel is not null) yield return BackPanel;
    }
}
