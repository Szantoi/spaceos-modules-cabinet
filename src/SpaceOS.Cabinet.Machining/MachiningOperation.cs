namespace SpaceOS.Cabinet.Machining;

/// <summary>
/// Describes the type of machining operation to be performed on a cabinet part.
/// </summary>
public enum MachiningOperation
{
    /// <summary>Cylindrical drill hole.</summary>
    Drill,

    /// <summary>Flat-bottomed groove cut with the grain.</summary>
    Groove,

    /// <summary>Stepped recess (rabbet/rebate) along an edge.</summary>
    Rabbet,

    /// <summary>Flat-bottomed recess in the face of a panel.</summary>
    Pocket,

    /// <summary>Profile cut along the perimeter or a path.</summary>
    Profile,

    /// <summary>Apply edge banding to an edge.</summary>
    EdgeBand,

    /// <summary>Straight through-cut.</summary>
    Cut,

    /// <summary>Chamfer (angled relief) on an edge.</summary>
    Chamfer,

    /// <summary>Round-over on an edge.</summary>
    Round
}
