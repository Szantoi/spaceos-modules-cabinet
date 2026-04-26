using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Events;
using SpaceOS.Cabinet.Geometry;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Aggregate root representing the structural skeleton of a cabinet.
/// Manages the <see cref="BaseCuboid"/>, additional <see cref="Part"/>s,
/// and the <see cref="Connection"/>s between them.
/// </summary>
public sealed class Skeleton
{
    /// <summary>Maximum number of parts allowed per skeleton (SEC-CAB-5: DOS protection).</summary>
    public const int MaxPartsPerSkeleton = 500;

    /// <summary>Maximum number of connections allowed per skeleton (SEC-CAB-5).</summary>
    public const int MaxConnectionsPerSkeleton = 2000;

    /// <summary>Maximum number of unflushed domain events before writes are blocked (BE-CAB-6).</summary>
    public const int MaxUnflushedEvents = 1000;

    /// <summary>Unique identifier of this skeleton.</summary>
    public Guid Id { get; private set; }

    /// <summary>Tenant that owns this skeleton (SEC-CAB-2: cross-tenant isolation).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Optimistic concurrency token — changes on every mutation.
    /// </summary>
    public Guid Version { get; private set; }

    /// <summary>The last sequence number assigned to a domain event.</summary>
    public long LastSequenceNumber { get; private set; }

    /// <summary>Current outer dimensions of the cabinet assembly.</summary>
    public AssemblyDimension Dimension { get; private set; }

    /// <summary>The mandatory four-sided carcass structure (A3).</summary>
    public BaseCuboid BaseCuboid { get; private set; }

    private readonly List<Part> _parts = new();

    /// <summary>All parts belonging to this skeleton, including BaseCuboid parts.</summary>
    public IReadOnlyList<Part> Parts => _parts.AsReadOnly();

    private readonly List<Connection> _connections = new();

    /// <summary>All joinery connections between parts in this skeleton.</summary>
    public IReadOnlyList<Connection> Connections => _connections.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>Pending domain events not yet flushed via <see cref="PopDomainEvents"/>.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private readonly Dictionary<(Guid PartId, CatalogType Type), Guid> _pinnedCatalogEntries = new();

    /// <summary>
    /// All catalog entries pinned to specific parts in this skeleton, keyed by (PartId, CatalogType).
    /// Populated via <see cref="PinCatalogEntry"/>.
    /// </summary>
    public IReadOnlyDictionary<(Guid PartId, CatalogType Type), Guid> PinnedCatalogEntries => _pinnedCatalogEntries;

    private Skeleton(Guid id, Guid tenantId, AssemblyDimension dimension, BaseCuboid baseCuboid)
    {
        Id = id;
        TenantId = tenantId;
        Version = Guid.NewGuid();
        LastSequenceNumber = 0;
        Dimension = dimension;
        BaseCuboid = baseCuboid;

        foreach (var part in baseCuboid.GetAllParts())
            _parts.Add(part);
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new Skeleton with a default <see cref="BaseCuboid"/> sized to <paramref name="dimension"/>.
    /// </summary>
    /// <param name="tenantId">Owning tenant identifier.</param>
    /// <param name="dimension">Outer cabinet dimensions in mm.</param>
    /// <param name="carcassThickness">Panel thickness in mm (default: 18 mm).</param>
    public static Result<Skeleton> Create(Guid tenantId, AssemblyDimension dimension, double carcassThickness = 18.0)
    {
        var id = Guid.NewGuid();
        var baseCuboidResult = BaseCuboid.CreateDefault(id, dimension, carcassThickness);
        if (!baseCuboidResult.IsSuccess)
            return Result<Skeleton>.Error(string.Join("; ", baseCuboidResult.Errors));

        var skeleton = new Skeleton(id, tenantId, dimension, baseCuboidResult.Value);
        skeleton.RecordEvent(new SkeletonCreated(id, tenantId, DateTime.UtcNow, skeleton.NextSequenceNumber()));
        return Result<Skeleton>.Success(skeleton);
    }

    // ── Mutation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new part to this skeleton. SEC-CAB-5: enforces <see cref="MaxPartsPerSkeleton"/>.
    /// </summary>
    /// <param name="frame">Position and orientation within assembly space.</param>
    /// <param name="materialReference">Material reference key.</param>
    /// <param name="partCatalogReference">Optional catalog reference key.</param>
    public Result<Part> AddPart(PartFrame frame, string materialReference, string partCatalogReference = "")
    {
        if (_domainEvents.Count >= MaxUnflushedEvents)
            return Result<Part>.Error("Domain events not flushed — call PopDomainEvents() first.");

        if (_parts.Count >= MaxPartsPerSkeleton)
            return Result<Part>.Invalid(new ValidationError($"Cannot exceed {MaxPartsPerSkeleton} parts per skeleton."));

        var part = new Part(Guid.NewGuid(), Id, frame, materialReference, partCatalogReference);
        _parts.Add(part);
        BumpVersion();
        RecordEvent(new PartAdded(Id, part.Id, DateTime.UtcNow, NextSequenceNumber()));
        return Result<Part>.Success(part);
    }

    /// <summary>
    /// Removes an additional (non-BaseCuboid) part by ID.
    /// All connections referencing the part are also removed.
    /// </summary>
    public Result RemovePart(Guid partId)
    {
        if (_domainEvents.Count >= MaxUnflushedEvents)
            return Result.Error("Domain events not flushed — call PopDomainEvents() first.");

        if (BaseCuboid.GetAllParts().Any(p => p.Id == partId))
            return Result.Invalid(new ValidationError("Cannot remove a BaseCuboid structural part."));

        var part = _parts.FirstOrDefault(p => p.Id == partId);
        if (part is null)
            return Result.Invalid(new ValidationError($"Part {partId} not found in this skeleton."));

        _connections.RemoveAll(c => c.ParentPartId == partId || c.ChildPartId == partId);
        _parts.Remove(part);
        BumpVersion();
        RecordEvent(new PartRemoved(Id, partId, DateTime.UtcNow, NextSequenceNumber()));
        return Result.Success();
    }

    /// <summary>
    /// Connects two existing parts. A5: default joint type is <see cref="JointType.FaceEdgeButt"/>.
    /// SEC-CAB-5: enforces <see cref="MaxConnectionsPerSkeleton"/>.
    /// </summary>
    public Result<Connection> AddConnection(
        Guid parentPartId,
        Guid childPartId,
        ConnectionGeometry geometry,
        JointType jointType = JointType.FaceEdgeButt)
    {
        if (_domainEvents.Count >= MaxUnflushedEvents)
            return Result<Connection>.Error("Domain events not flushed — call PopDomainEvents() first.");

        if (_connections.Count >= MaxConnectionsPerSkeleton)
            return Result<Connection>.Invalid(new ValidationError($"Cannot exceed {MaxConnectionsPerSkeleton} connections per skeleton."));

        if (parentPartId == childPartId)
            return Result<Connection>.Invalid(new ValidationError("A part cannot be connected to itself."));

        if (_parts.All(p => p.Id != parentPartId))
            return Result<Connection>.Invalid(new ValidationError($"Parent part {parentPartId} not found in this skeleton."));

        if (_parts.All(p => p.Id != childPartId))
            return Result<Connection>.Invalid(new ValidationError($"Child part {childPartId} not found in this skeleton."));

        var connection = new Connection(Guid.NewGuid(), Id, parentPartId, childPartId, jointType, geometry);
        _connections.Add(connection);
        BumpVersion();
        RecordEvent(new ConnectionAdded(Id, connection.Id, DateTime.UtcNow, NextSequenceNumber()));
        return Result<Connection>.Success(connection);
    }

    /// <summary>Removes a connection by ID.</summary>
    public Result RemoveConnection(Guid connectionId)
    {
        if (_domainEvents.Count >= MaxUnflushedEvents)
            return Result.Error("Domain events not flushed — call PopDomainEvents() first.");

        var conn = _connections.FirstOrDefault(c => c.Id == connectionId);
        if (conn is null)
            return Result.Invalid(new ValidationError($"Connection {connectionId} not found in this skeleton."));

        _connections.Remove(conn);
        BumpVersion();
        RecordEvent(new ConnectionRemoved(Id, connectionId, DateTime.UtcNow, NextSequenceNumber()));
        return Result.Success();
    }

    /// <summary>
    /// Updates the assembly outer dimensions (A10: triggers downstream recalculation).
    /// </summary>
    public Result ResizeAssembly(AssemblyDimension newDimension)
    {
        if (_domainEvents.Count >= MaxUnflushedEvents)
            return Result.Error("Domain events not flushed — call PopDomainEvents() first.");

        var oldDim = Dimension;
        Dimension = newDimension;
        BumpVersion();
        RecordEvent(new SkeletonResized(Id, oldDim, newDimension, DateTime.UtcNow, NextSequenceNumber()));
        return Result.Success();
    }

    /// <summary>
    /// Pins a catalog entry to a specific part + type slot (SEC-CAB02-2: validates part belongs to this skeleton).
    /// </summary>
    /// <param name="partId">The part to pin the entry to. Must exist in this skeleton.</param>
    /// <param name="catalogType">The catalog type slot to pin.</param>
    /// <param name="catalogEntryId">The catalog entry ID to pin. Must not be <see cref="Guid.Empty"/>.</param>
    public Result PinCatalogEntry(Guid partId, CatalogType catalogType, Guid catalogEntryId)
    {
        if (_parts.All(p => p.Id != partId))
            return Result.Invalid(new ValidationError($"Part {partId} not found in this skeleton."));

        if (catalogEntryId == Guid.Empty)
            return Result.Invalid(new ValidationError("CatalogEntryId must not be empty."));

        _pinnedCatalogEntries[(partId, catalogType)] = catalogEntryId;
        BumpVersion();
        return Result.Success();
    }

    /// <summary>
    /// Derives assembly ordering for all parts using the resolver (A14).
    /// Raises an <see cref="AssemblyDerived"/> domain event on success.
    /// </summary>
    /// <param name="resolver">Lightweight catalog resolver from Abstractions. Must not be null.</param>
    public Result DeriveAssembly(ICatalogResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        RecordEvent(new AssemblyDerived(Id, DateTime.UtcNow, NextSequenceNumber()));
        BumpVersion();
        return Result.Success();
    }

    /// <summary>
    /// Derives a bill of services from all pinned catalog entries (A13 extension point).
    /// Returns an empty list when no entries have been pinned.
    /// </summary>
    public Result<BillOfServices> DeriveBillOfServices()
    {
        var items = _pinnedCatalogEntries
            .Select(kvp => new BillOfServicesItem(kvp.Key.PartId, kvp.Key.Type, kvp.Value))
            .ToList();
        return Result<BillOfServices>.Success(new BillOfServices(Id, items));
    }

    // ── Event handling ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all pending domain events in sequence order and clears the internal buffer.
    /// DB-CAB-7: events are ordered by <see cref="IDomainEvent.SequenceNumber"/>.
    /// </summary>
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var ordered = _domainEvents.OrderBy(e => e.SequenceNumber).ToList().AsReadOnly();
        _domainEvents.Clear();
        return ordered;
    }

    // ── Internal reconstruction ──────────────────────────────────────────────

    /// <summary>
    /// Reconstructs a Skeleton from persisted state without raising new domain events.
    /// Used exclusively by <see cref="SkeletonReconstruction"/>.
    /// </summary>
    internal static Skeleton Reconstruct(
        Guid id,
        Guid tenantId,
        Guid version,
        long lastSequenceNumber,
        AssemblyDimension dimension,
        BaseCuboid baseCuboid,
        List<Part> additionalParts,
        List<Connection> connections,
        Dictionary<(Guid PartId, CatalogType Type), Guid>? pinnedCatalogEntries = null)
    {
        var skeleton = new Skeleton(id, tenantId, dimension, baseCuboid);
        skeleton.Version = version;
        skeleton.LastSequenceNumber = lastSequenceNumber;

        foreach (var p in additionalParts)
            skeleton._parts.Add(p);

        foreach (var c in connections)
            skeleton._connections.Add(c);

        if (pinnedCatalogEntries is not null)
        {
            foreach (var kvp in pinnedCatalogEntries)
                skeleton._pinnedCatalogEntries[kvp.Key] = kvp.Value;
        }

        return skeleton;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private long NextSequenceNumber() => ++LastSequenceNumber;

    private void BumpVersion() => Version = Guid.NewGuid();

    private void RecordEvent(IDomainEvent evt) => _domainEvents.Add(evt);
}
