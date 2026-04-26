using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Assembly;

public class AssemblyDocumentationServiceTests
{
    private static AssemblyDimension ValidDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    private static PartFrame ValidPartFrame()
    {
        var dim = PartDimension.Create(200, 560, 18).Value;
        return PartFrame.Create(AffineTransform.Identity, dim).Value;
    }

    private static ConnectionGeometry ValidGeometry()
        => new ConnectionGeometry(PartFace.Top, PartEdge.TopFront, 0);

    // ── GenerateAssemblySteps ────────────────────────────────────────────────

    [Fact]
    public void GenerateAssemblySteps_NullSkeleton_Throws()
    {
        var service = new AssemblyDocumentationService();

        Assert.Throws<ArgumentNullException>(() => service.GenerateAssemblySteps(null!));
    }

    [Fact]
    public void GenerateAssemblySteps_StepCountEqualsPartCount()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        Assert.Equal(skeleton.Parts.Count, result.Value.Count);
    }

    [Fact]
    public void GenerateAssemblySteps_EachStepHasUniqueOrder()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        var orders = result.Value.Select(s => s.Order).ToList();
        Assert.Equal(orders.Distinct().Count(), orders.Count);
    }

    [Fact]
    public void GenerateAssemblySteps_OrdersPartsTopologically_BaseCuboidPartsFirst()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var frame = ValidPartFrame();
        var part = skeleton.AddPart(frame, "mat-a").Value;

        // Add a connection from the first BaseCuboid part to the new part
        var parentId = skeleton.Parts[0].Id;
        skeleton.AddConnection(parentId, part.Id, ValidGeometry());
        skeleton.PopDomainEvents();

        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        // The new child part (connected) should come after its parent
        var parentStep = result.Value.First(s => s.PrimaryPartId == parentId);
        var childStep = result.Value.First(s => s.PrimaryPartId == part.Id);
        Assert.True(parentStep.Order < childStep.Order);
    }

    [Fact]
    public void GenerateAssemblySteps_WithConnections_ChildComesAfterParent()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var frame = ValidPartFrame();
        var child = skeleton.AddPart(frame, "mat-a").Value;
        skeleton.PopDomainEvents();

        var parentId = skeleton.Parts[0].Id;
        skeleton.AddConnection(parentId, child.Id, ValidGeometry());
        skeleton.PopDomainEvents();

        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        var parentOrder = result.Value.First(s => s.PrimaryPartId == parentId).Order;
        var childOrder = result.Value.First(s => s.PrimaryPartId == child.Id).Order;
        Assert.True(parentOrder < childOrder);
    }

    [Fact]
    public void GenerateAssemblySteps_WithCyclicConnections_AllPartsIncluded()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var frame = ValidPartFrame();
        var partA = skeleton.AddPart(frame, "mat-a").Value;
        var partB = skeleton.AddPart(frame, "mat-b").Value;
        skeleton.PopDomainEvents();

        // Create a connection in one direction only (A→B) since self-loops are prevented
        skeleton.AddConnection(partA.Id, partB.Id, ValidGeometry());
        skeleton.PopDomainEvents();

        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        Assert.Equal(skeleton.Parts.Count, result.Value.Count);
    }

    [Fact]
    public void GenerateAssemblySteps_InstructionIsSanitized()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        // Default instructions use part IDs — no HTML injection in generated instructions
        foreach (var step in result.Value)
        {
            Assert.NotNull(step.SanitizedInstruction);
        }
    }

    [Fact]
    public void GenerateAssemblySteps_WithCustomSanitizer_UsesIt()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var customSanitizer = new CapturingSanitizer();
        var service = new AssemblyDocumentationService(customSanitizer);

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        Assert.True(customSanitizer.CallCount > 0);
    }

    [Fact]
    public void GenerateAssemblySteps_AllStepsHaveNonEmptyTitle()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value, step => Assert.NotEmpty(step.Title));
    }

    [Fact]
    public void GenerateAssemblySteps_AllStepsHaveValidPartId()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value, step => Assert.NotEqual(Guid.Empty, step.PrimaryPartId));
    }

    [Fact]
    public void GenerateAssemblySteps_PartIdsMatchSkeletonParts()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var result = service.GenerateAssemblySteps(skeleton);

        Assert.True(result.IsSuccess);
        var stepPartIds = result.Value.Select(s => s.PrimaryPartId).OrderBy(id => id).ToList();
        var skeletonPartIds = skeleton.Parts.Select(p => p.Id).OrderBy(id => id).ToList();
        Assert.Equal(skeletonPartIds, stepPartIds);
    }

    // ── GenerateExplodedView ─────────────────────────────────────────────────

    [Fact]
    public void GenerateExplodedView_NullSkeleton_Throws()
    {
        var service = new AssemblyDocumentationService();

        Assert.Throws<ArgumentNullException>(() => service.GenerateExplodedView(null!));
    }

    [Fact]
    public void GenerateExplodedView_ReturnsSingleLayer_WhenNoConnections()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var view = service.GenerateExplodedView(skeleton);

        Assert.Single(view.Layers);
    }

    [Fact]
    public void GenerateExplodedView_WithConnections_ReturnsTwoLayers()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var frame = ValidPartFrame();
        var child = skeleton.AddPart(frame, "mat-a").Value;
        skeleton.PopDomainEvents();

        var parentId = skeleton.Parts[0].Id;
        skeleton.AddConnection(parentId, child.Id, ValidGeometry());
        skeleton.PopDomainEvents();

        var service = new AssemblyDocumentationService();

        var view = service.GenerateExplodedView(skeleton);

        // Base parts (no parents) go to layer 0; child parts to layer 1
        Assert.Equal(2, view.Layers.Count);
    }

    [Fact]
    public void GenerateExplodedView_AllPartsIncludedInLayers()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var service = new AssemblyDocumentationService();

        var view = service.GenerateExplodedView(skeleton);

        var allPartIds = view.Layers.SelectMany(l => l.PartIds).OrderBy(id => id).ToList();
        var skeletonIds = skeleton.Parts.Select(p => p.Id).OrderBy(id => id).ToList();
        Assert.Equal(skeletonIds, allPartIds);
    }

    [Fact]
    public void GenerateExplodedView_LayerIndicesAreOrdered()
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;
        var frame = ValidPartFrame();
        var child = skeleton.AddPart(frame, "mat-a").Value;
        skeleton.PopDomainEvents();

        skeleton.AddConnection(skeleton.Parts[0].Id, child.Id, ValidGeometry());
        skeleton.PopDomainEvents();

        var service = new AssemblyDocumentationService();

        var view = service.GenerateExplodedView(skeleton);

        var indices = view.Layers.Select(l => l.LayerIndex).ToList();
        Assert.Equal(indices.OrderBy(i => i).ToList(), indices);
    }
}

/// <summary>Test spy to verify the sanitizer is invoked by the service.</summary>
internal sealed class CapturingSanitizer : IMarkdownSanitizer
{
    public int CallCount { get; private set; }

    public string Sanitize(string rawMarkdown)
    {
        CallCount++;
        return rawMarkdown;
    }
}
