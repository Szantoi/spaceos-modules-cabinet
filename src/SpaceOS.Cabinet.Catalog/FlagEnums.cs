namespace SpaceOS.Cabinet.Catalog;

/// <summary>Reason given by the reporter when flagging a catalog entry.</summary>
public enum FlagReason
{
    /// <summary>Entry is spam or unsolicited advertising.</summary>
    Spam = 0,

    /// <summary>Entry contains inappropriate content.</summary>
    Inappropriate = 1,

    /// <summary>Entry copies another entry without attribution.</summary>
    Plagiarism = 2,

    /// <summary>Entry describes a construction that is unsafe or dangerous.</summary>
    DangerousConstruction = 3,

    /// <summary>Entry payload is broken or unparseable.</summary>
    BrokenContent = 4,

    /// <summary>Any other reason not covered above.</summary>
    Other = 99
}

/// <summary>Resolution state of a <see cref="CatalogEntryFlag"/>.</summary>
public enum FlagState
{
    /// <summary>Flag is open and awaiting admin review.</summary>
    Active = 0,

    /// <summary>Flag was reviewed and cleared by an admin.</summary>
    AdminCleared = 1,

    /// <summary>Flag was reviewed and upheld; entry action may follow.</summary>
    AdminUpheld = 2,

    /// <summary>Flag was withdrawn by the original reporter.</summary>
    Withdrawn = 3
}
