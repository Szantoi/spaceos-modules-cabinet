using SpaceOS.Cabinet.Domain.Skeleton;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Domain;

public class DependencyGraphTests
{
    // ── Empty graph ──────────────────────────────────────────────────────────

    [Fact]
    public void EmptyDirtyList_ReturnsEmptyOrder()
    {
        var graph = new DependencyGraph();
        graph.AddNode(Guid.NewGuid());

        var result = graph.GetRecalculationOrder(Array.Empty<Guid>());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void DirtyNodeNotInGraph_ReturnsEmptyOrder()
    {
        var graph = new DependencyGraph();

        var result = graph.GetRecalculationOrder(new[] { Guid.NewGuid() });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    // ── Single node ──────────────────────────────────────────────────────────

    [Fact]
    public void SingleDirtyNode_NoEdges_ReturnsThatNode()
    {
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        graph.AddNode(a);

        var result = graph.GetRecalculationOrder(new[] { a });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(a, result.Value[0]);
    }

    // ── Linear chain ─────────────────────────────────────────────────────────

    [Fact]
    public void LinearChain_A_DependsOn_B_DependsOn_C_DirtyC_ReturnsCorrectOrder()
    {
        // C -> B -> A (A depends on B, B depends on C)
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        graph.AddDependency(b, c);  // B depends on C
        graph.AddDependency(a, b);  // A depends on B

        var result = graph.GetRecalculationOrder(new[] { c });

        Assert.True(result.IsSuccess);
        // All three should be in the recalculation order
        Assert.Contains(c, result.Value);
        Assert.Contains(b, result.Value);
        Assert.Contains(a, result.Value);
        // c must come before b, b before a
        Assert.True(result.Value.ToList().IndexOf(c) < result.Value.ToList().IndexOf(b));
        Assert.True(result.Value.ToList().IndexOf(b) < result.Value.ToList().IndexOf(a));
    }

    // ── Diamond shape ─────────────────────────────────────────────────────────

    [Fact]
    public void DiamondGraph_DirtyRoot_ReturnsValidTopologicalOrder()
    {
        // root -> left -> top
        //      -> right -> top
        var graph = new DependencyGraph();
        var root = Guid.NewGuid();
        var left = Guid.NewGuid();
        var right = Guid.NewGuid();
        var top = Guid.NewGuid();

        graph.AddDependency(left, root);
        graph.AddDependency(right, root);
        graph.AddDependency(top, left);
        graph.AddDependency(top, right);

        var result = graph.GetRecalculationOrder(new[] { root });

        Assert.True(result.IsSuccess);
        var order = result.Value.ToList();
        // root before left and right; both before top
        Assert.True(order.IndexOf(root) < order.IndexOf(left));
        Assert.True(order.IndexOf(root) < order.IndexOf(right));
        Assert.True(order.IndexOf(left) < order.IndexOf(top));
        Assert.True(order.IndexOf(right) < order.IndexOf(top));
    }

    // ── Cycle detection (SEC-CAB-7) ──────────────────────────────────────────

    [Fact]
    public void CycleDetection_TwoNodeCycle_ReturnsError()
    {
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        graph.AddDependency(a, b);
        graph.AddDependency(b, a);  // creates a cycle

        var result = graph.GetRecalculationOrder(new[] { a });

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    [Fact]
    public void CycleDetection_ThreeNodeCycle_ReturnsError()
    {
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        graph.AddDependency(a, b);
        graph.AddDependency(b, c);
        graph.AddDependency(c, a);  // closes the cycle

        var result = graph.GetRecalculationOrder(new[] { a });

        Assert.False(result.IsSuccess);
        Assert.Equal(Ardalis.Result.ResultStatus.Error, result.Status);
    }

    // ── Dirty subset ──────────────────────────────────────────────────────────

    [Fact]
    public void DirtySubset_OnlyTransitivelyAffectedNodesReturned()
    {
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var isolated = Guid.NewGuid();

        graph.AddDependency(b, a);  // b depends on a
        graph.AddDependency(c, b);  // c depends on b
        graph.AddNode(isolated);    // isolated — not connected

        var result = graph.GetRecalculationOrder(new[] { a });

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(isolated, result.Value);
        Assert.Contains(a, result.Value);
        Assert.Contains(b, result.Value);
        Assert.Contains(c, result.Value);
    }

    // ── Multiple dirty nodes ──────────────────────────────────────────────────

    [Fact]
    public void MultipleDirtyNodes_AllTransitiveDependentsIncluded()
    {
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();

        graph.AddDependency(c, a);
        graph.AddDependency(d, b);

        var result = graph.GetRecalculationOrder(new[] { a, b });

        Assert.True(result.IsSuccess);
        Assert.Contains(a, result.Value);
        Assert.Contains(b, result.Value);
        Assert.Contains(c, result.Value);
        Assert.Contains(d, result.Value);
    }

    // ── Nodes property ────────────────────────────────────────────────────────

    [Fact]
    public void Nodes_ReflectsAllRegisteredNodes()
    {
        var graph = new DependencyGraph();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        graph.AddDependency(a, b);

        Assert.Contains(a, graph.Nodes);
        Assert.Contains(b, graph.Nodes);
    }
}
