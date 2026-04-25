namespace SpaceOS.Cabinet.Abstractions;

/// <summary>How the back panel is attached to the carcass by default.</summary>
public enum BackPanelAttachmentDefault
{
    /// <summary>Flush / butt-joint attachment.</summary>
    Stumpf,

    /// <summary>Rabbet (rebate) attachment.</summary>
    Rabbet,

    /// <summary>Groove (dado) attachment.</summary>
    Groove
}

/// <summary>The construction type for the top of the cabinet.</summary>
public enum TopType
{
    /// <summary>A single solid top panel spanning the full width.</summary>
    FullTop,

    /// <summary>A pair of cross rails instead of a full top.</summary>
    CrossRailPair
}

/// <summary>Severity levels for design advisories (A11).</summary>
public enum AdvisorySeverity
{
    /// <summary>Informational — no action required.</summary>
    Info,

    /// <summary>Warning — user should review but design is still valid.</summary>
    Warning,

    /// <summary>Error — design has a problem that should be fixed.</summary>
    Error,

    /// <summary>Critical — design cannot be manufactured as-is.</summary>
    Critical
}

/// <summary>The six faces of a rectangular panel part.</summary>
public enum PartFace
{
    /// <summary>Top face (+Z in part-local space).</summary>
    Top,

    /// <summary>Bottom face (-Z in part-local space).</summary>
    Bottom,

    /// <summary>Left face (-X in part-local space).</summary>
    Left,

    /// <summary>Right face (+X in part-local space).</summary>
    Right,

    /// <summary>Front face (-Y in part-local space).</summary>
    Front,

    /// <summary>Back face (+Y in part-local space).</summary>
    Back
}

/// <summary>The twelve edges of a rectangular panel part.</summary>
public enum PartEdge
{
    /// <summary>Top-front edge.</summary>
    TopFront,

    /// <summary>Top-back edge.</summary>
    TopBack,

    /// <summary>Top-left edge.</summary>
    TopLeft,

    /// <summary>Top-right edge.</summary>
    TopRight,

    /// <summary>Bottom-front edge.</summary>
    BottomFront,

    /// <summary>Bottom-back edge.</summary>
    BottomBack,

    /// <summary>Bottom-left edge.</summary>
    BottomLeft,

    /// <summary>Bottom-right edge.</summary>
    BottomRight,

    /// <summary>Front-left edge.</summary>
    FrontLeft,

    /// <summary>Front-right edge.</summary>
    FrontRight,

    /// <summary>Back-left edge.</summary>
    BackLeft,

    /// <summary>Back-right edge.</summary>
    BackRight
}

/// <summary>Semantic role of a part within the cabinet skeleton.</summary>
public enum PartRole
{
    /// <summary>Left carcass side panel.</summary>
    LeftSide,

    /// <summary>Right carcass side panel.</summary>
    RightSide,

    /// <summary>Top panel or top rail.</summary>
    Top,

    /// <summary>Bottom / floor panel.</summary>
    Bottom,

    /// <summary>Back panel.</summary>
    BackPanel,

    /// <summary>Horizontal shelf.</summary>
    Shelf,

    /// <summary>Vertical divider.</summary>
    VerticalDivider,

    /// <summary>Door or drawer front.</summary>
    Front,

    /// <summary>Role not yet determined.</summary>
    Unknown
}
