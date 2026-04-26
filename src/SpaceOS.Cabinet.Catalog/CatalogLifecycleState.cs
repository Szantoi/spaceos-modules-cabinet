namespace SpaceOS.Cabinet.Catalog;

/// <summary>5-state FSM lifecycle for a <see cref="CatalogEntry"/>.</summary>
public enum CatalogLifecycleState
{
    /// <summary>Entry is being authored — not yet submitted for review.</summary>
    Draft,

    /// <summary>Entry has been submitted and is awaiting approval.</summary>
    Submitted,

    /// <summary>Entry has been approved and is ready to be published.</summary>
    Approved,

    /// <summary>Entry is live and resolvable by the tenant.</summary>
    Published,

    /// <summary>Entry has been superseded and should no longer be used.</summary>
    Deprecated,

    /// <summary>Entry was rejected during review; can be revised and re-submitted.</summary>
    Rejected
}
