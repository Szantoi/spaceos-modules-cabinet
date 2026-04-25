using Ardalis.Result;

namespace SpaceOS.Cabinet.Domain.Skeleton;

/// <summary>
/// A directed acyclic graph of part dependencies used for selective recalculation (A10).
/// Implements Kahn's topological sort algorithm.
/// SEC-CAB-7: cycle detection returns an error result rather than throwing.
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<Guid, HashSet<Guid>> _adjacency = new();  // node → set of nodes it depends on
    private readonly HashSet<Guid> _nodes = new();

    /// <summary>All node IDs registered in this graph.</summary>
    public IReadOnlySet<Guid> Nodes => _nodes;

    /// <summary>Registers a node without any dependencies.</summary>
    public void AddNode(Guid nodeId)
    {
        _nodes.Add(nodeId);
        if (!_adjacency.ContainsKey(nodeId))
            _adjacency[nodeId] = new HashSet<Guid>();
    }

    /// <summary>
    /// Declares that <paramref name="dependent"/> depends on <paramref name="dependency"/>.
    /// Both nodes are registered if not already present.
    /// </summary>
    /// <param name="dependent">The node that requires a recalculation when <paramref name="dependency"/> changes.</param>
    /// <param name="dependency">The node whose change triggers <paramref name="dependent"/>.</param>
    public void AddDependency(Guid dependent, Guid dependency)
    {
        AddNode(dependent);
        AddNode(dependency);
        _adjacency[dependent].Add(dependency);
    }

    /// <summary>
    /// Returns the topologically sorted recalculation order for all nodes transitively affected
    /// by changes to <paramref name="dirtyIds"/>.
    /// SEC-CAB-7: returns <see cref="ResultStatus.Error"/> if a cycle is detected.
    /// </summary>
    /// <param name="dirtyIds">The set of nodes that have changed and need recalculation.</param>
    public Result<IReadOnlyList<Guid>> GetRecalculationOrder(IReadOnlyList<Guid> dirtyIds)
    {
        if (dirtyIds.Count == 0)
            return Result<IReadOnlyList<Guid>>.Success(Array.Empty<Guid>());

        // Build the set of all transitively affected nodes using reverse adjacency (who depends on me)
        var reverseAdj = BuildReverseAdjacency();
        var affected = new HashSet<Guid>();
        var propagationQueue = new Queue<Guid>();

        foreach (var id in dirtyIds)
        {
            if (_nodes.Contains(id) && affected.Add(id))
                propagationQueue.Enqueue(id);
        }

        while (propagationQueue.Count > 0)
        {
            var current = propagationQueue.Dequeue();
            if (reverseAdj.TryGetValue(current, out var dependents))
            {
                foreach (var dep in dependents)
                {
                    if (affected.Add(dep))
                        propagationQueue.Enqueue(dep);
                }
            }
        }

        if (affected.Count == 0)
            return Result<IReadOnlyList<Guid>>.Success(Array.Empty<Guid>());

        // Kahn's algorithm on the affected subgraph
        // In-degree counts how many of a node's *dependencies* are still in the affected set
        var inDegree = new Dictionary<Guid, int>(affected.Count);
        foreach (var node in affected)
            inDegree[node] = 0;

        foreach (var node in affected)
        {
            if (_adjacency.TryGetValue(node, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (affected.Contains(dep))
                        inDegree[node]++;
                }
            }
        }

        // Nodes with zero in-degree (no un-processed dependencies) can be emitted first
        var readyQueue = new Queue<Guid>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var sorted = new List<Guid>(affected.Count);

        while (readyQueue.Count > 0)
        {
            var node = readyQueue.Dequeue();
            sorted.Add(node);

            // Reduce in-degree for every node that depends on the now-processed node
            if (reverseAdj.TryGetValue(node, out var dependents))
            {
                foreach (var dep in dependents)
                {
                    if (!affected.Contains(dep)) continue;
                    inDegree[dep]--;
                    if (inDegree[dep] == 0)
                        readyQueue.Enqueue(dep);
                }
            }
        }

        // SEC-CAB-7: if we could not process all affected nodes a cycle exists
        if (sorted.Count < affected.Count)
            return Result<IReadOnlyList<Guid>>.Error("Cycle detected in dependency graph — recalculation order cannot be determined.");

        return Result<IReadOnlyList<Guid>>.Success(sorted.AsReadOnly());
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private Dictionary<Guid, HashSet<Guid>> BuildReverseAdjacency()
    {
        var reverse = new Dictionary<Guid, HashSet<Guid>>();

        foreach (var (node, deps) in _adjacency)
        {
            foreach (var dep in deps)
            {
                if (!reverse.TryGetValue(dep, out var set))
                {
                    set = new HashSet<Guid>();
                    reverse[dep] = set;
                }
                set.Add(node);
            }
        }

        return reverse;
    }
}
