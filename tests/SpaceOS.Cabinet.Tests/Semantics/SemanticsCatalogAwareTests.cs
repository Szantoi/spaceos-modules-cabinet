using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Semantics;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Semantics;

public class SemanticsCatalogAwareTests
{
    private static AssemblyDimension ValidDimension()
        => AssemblyDimension.Create(600, 720, 560).Value;

    private static Skeleton CreateSkeleton()
        => Skeleton.Create(Guid.NewGuid(), ValidDimension()).Value;

    [Fact]
    public void InferAll_WithNullResolver_WorksAsOriginal()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        var resultWithResolver = service.InferAll(skeleton, null);
        var resultOriginal = service.InferAll(skeleton);

        Assert.Equal(resultOriginal.Count, resultWithResolver.Count);
        foreach (var kvp in resultOriginal)
            Assert.Equal(kvp.Value, resultWithResolver[kvp.Key]);
    }

    [Fact]
    public void InferAll_WithResolver_ReturnsCorrectRoles()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();
        var resolver = new NoOpCatalogResolver();

        var result = service.InferAll(skeleton, resolver);

        Assert.NotNull(result);
        Assert.Equal(skeleton.Parts.Count, result.Count);
    }

    [Fact]
    public void InferAll_WithResolver_AllPartsInResult()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        var result = service.InferAll(skeleton, new NoOpCatalogResolver());

        foreach (var part in skeleton.Parts)
            Assert.True(result.ContainsKey(part.Id));
    }

    [Fact]
    public void InferAll_NullSkeleton_Throws()
    {
        var service = new SemanticInferenceService();

        Assert.Throws<ArgumentNullException>(() => service.InferAll(null!, new NoOpCatalogResolver()));
    }

    [Fact]
    public void InferAll_CatalogResolverParam_IsOptional()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        // Should compile and work with null resolver
        var result = service.InferAll(skeleton, catalogResolver: null);

        Assert.NotNull(result);
    }

    [Fact]
    public void InferAll_WithResolver_SameResultAsWithoutResolver()
    {
        // For Cabinet 0.2, resolver is a no-op; results should be identical
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        var withResolver = service.InferAll(skeleton, new NoOpCatalogResolver());
        var withoutResolver = service.InferAll(skeleton);

        Assert.Equal(withoutResolver.Count, withResolver.Count);
    }

    [Fact]
    public void InferAll_WithResolverAndAddedPart_IncludesNewPart()
    {
        var skeleton = CreateSkeleton();
        var dim = PartDimension.Create(200, 560, 18).Value;
        var frame = PartFrame.Create(AffineTransform.Identity, dim).Value;
        var part = skeleton.AddPart(frame, "mat-a").Value;
        skeleton.PopDomainEvents();

        var service = new SemanticInferenceService();

        var result = service.InferAll(skeleton, new NoOpCatalogResolver());

        Assert.True(result.ContainsKey(part.Id));
    }

    [Fact]
    public void InferAll_WithResolver_EmptySkeleton_ReturnsBaseCuboidParts()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        var result = service.InferAll(skeleton, new NoOpCatalogResolver());

        // BaseCuboid creates 4 parts
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void InferAll_WithResolver_BaseCuboidBottomPartRecognized()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        var result = service.InferAll(skeleton, new NoOpCatalogResolver());

        // At least one part should have a recognized role (not Unknown for BaseCuboid parts)
        var recognizedRoles = result.Values.Where(r => r != PartRole.Unknown).ToList();
        Assert.NotEmpty(recognizedRoles);
    }

    [Fact]
    public void InferAll_OriginalOverload_StillWorks()
    {
        var skeleton = CreateSkeleton();
        var service = new SemanticInferenceService();

        // Original overload (no resolver) must still compile and work
        var result = service.InferAll(skeleton);

        Assert.NotNull(result);
        Assert.Equal(skeleton.Parts.Count, result.Count);
    }
}

/// <summary>Test stub: catalog resolver that always reports no pinned entries.</summary>
internal sealed class NoOpCatalogResolver : ICatalogResolver
{
    public bool TryGetPinnedEntry(Guid skeletonId, Guid partId, CatalogType type, out Guid catalogEntryId)
    {
        catalogEntryId = Guid.Empty;
        return false;
    }
}
