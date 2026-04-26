namespace SpaceOS.Cabinet.Assembly;

using Ardalis.Result;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// Generates ordered assembly documentation for a <see cref="Skeleton"/> (A14).
/// Complexity: O(N+E) topological sort on the connections graph (Kahn's algorithm).
/// BE-CAB02-8: algorithm complexity documented per spec.
/// </summary>
public sealed class AssemblyDocumentationService
{
    private readonly IMarkdownSanitizer _sanitizer;

    /// <summary>
    /// Initializes the service with an optional markdown sanitizer.
    /// Defaults to <see cref="MarkdownSanitizer"/> when <paramref name="sanitizer"/> is <c>null</c>.
    /// </summary>
    /// <param name="sanitizer">Markdown sanitizer for step instructions, or <c>null</c> to use the default.</param>
    public AssemblyDocumentationService(IMarkdownSanitizer? sanitizer = null)
    {
        _sanitizer = sanitizer ?? new MarkdownSanitizer();
    }

    /// <summary>
    /// Generates assembly steps ordered topologically by part connections (gravity-aware, base parts first).
    /// Parts with no parent connections are emitted first; children follow in dependency order.
    /// Cyclic sub-graphs have their remaining parts appended at the end.
    /// </summary>
    /// <param name="skeleton">The skeleton to generate steps for. Must not be null.</param>
    /// <param name="resolver">Optional full catalog resolver (for future catalog-driven step enrichment).</param>
    /// <returns>An ordered list of <see cref="AssemblyStep"/>, one per part.</returns>
    public Result<IReadOnlyList<AssemblyStep>> GenerateAssemblySteps(
        Skeleton skeleton,
        ICatalogResolutionProvider? resolver = null)
    {
        ArgumentNullException.ThrowIfNull(skeleton);

        var partIds = skeleton.Parts.Select(p => p.Id).ToHashSet();
        var inDegree = partIds.ToDictionary(id => id, _ => 0);
        var edges = new Dictionary<Guid, List<Guid>>();

        // Build adjacency list: parent → children
        foreach (var conn in skeleton.Connections)
        {
            if (!edges.ContainsKey(conn.ParentPartId))
                edges[conn.ParentPartId] = new List<Guid>();

            edges[conn.ParentPartId].Add(conn.ChildPartId);
            inDegree[conn.ChildPartId]++;
        }

        // Kahn's algorithm: start with parts that have no parents
        var queue = new Queue<Guid>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var ordered = new List<Guid>(partIds.Count);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            ordered.Add(current);

            if (edges.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    inDegree[child]--;
                    if (inDegree[child] == 0)
                        queue.Enqueue(child);
                }
            }
        }

        // Append any parts not visited (cyclic sub-graphs)
        foreach (var remaining in partIds.Except(ordered))
            ordered.Add(remaining);

        var steps = new List<AssemblyStep>(ordered.Count);
        int order = 0;

        foreach (var partId in ordered)
        {
            var connectionIds = skeleton.Connections
                .Where(c => c.ParentPartId == partId || c.ChildPartId == partId)
                .Select(c => c.Id)
                .ToList();

            var title = $"Assemble part {order + 1}";
            var instruction = $"Place part {partId} into position.";

            var stepResult = AssemblyStep.Create(
                order: order++,
                title: title,
                rawInstruction: instruction,
                primaryPartId: partId,
                requiredConnectionIds: connectionIds,
                sanitizer: _sanitizer);

            if (!stepResult.IsSuccess)
                return Result<IReadOnlyList<AssemblyStep>>.Error(string.Join("; ", stepResult.Errors));

            steps.Add(stepResult.Value);
        }

        return Result<IReadOnlyList<AssemblyStep>>.Success(steps);
    }

    /// <summary>
    /// Builds an exploded-view diagram by grouping parts by their topological depth from base parts.
    /// Currently groups all parts with no parent connections into layer 0 and all others into layer 1.
    /// Full depth-layering is a Cabinet 0.3 enhancement.
    /// </summary>
    /// <param name="skeleton">The skeleton to build the exploded view for. Must not be null.</param>
    /// <returns>An <see cref="ExplodedView"/> with parts grouped into layers.</returns>
    public ExplodedView GenerateExplodedView(Skeleton skeleton)
    {
        ArgumentNullException.ThrowIfNull(skeleton);

        var childPartIds = skeleton.Connections.Select(c => c.ChildPartId).ToHashSet();

        var baseParts = skeleton.Parts
            .Where(p => !childPartIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToList();

        var dependentParts = skeleton.Parts
            .Where(p => childPartIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToList();

        var layers = new List<ExplodedViewLayer>();

        if (baseParts.Count > 0)
            layers.Add(new ExplodedViewLayer(0, baseParts));

        if (dependentParts.Count > 0)
            layers.Add(new ExplodedViewLayer(1, dependentParts));

        // Skeleton with no connections: all parts in one layer
        if (layers.Count == 0 && skeleton.Parts.Count > 0)
            layers.Add(new ExplodedViewLayer(0, skeleton.Parts.Select(p => p.Id).ToList()));

        return new ExplodedView(layers);
    }
}
