using System.Reflection;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Catalog;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Domain;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using SpaceOS.Cabinet.Semantics;
using Xunit;

namespace SpaceOS.Cabinet.Tests.CrossCutting;

/// <summary>
/// Verifies NuGet package versions via <see cref="AssemblyInformationalVersionAttribute"/>
/// and structural invariants (e.g., no public setters on federation properties).
/// </summary>
public class VersionConsistencyTests
{
    private static string InformationalVersion(System.Type anchorType)
    {
        var attr = anchorType.Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attr?.InformationalVersion ?? string.Empty;
    }

    // ── Geometry: 0.2.1 ─────────────────────────────────────────────────────────

    [Fact]
    public void Geometry_Version_Is_0_2_1()
    {
        var version = InformationalVersion(typeof(Vector3));
        Assert.StartsWith("0.2.1", version);
    }

    // ── Machining: 0.2.1 ────────────────────────────────────────────────────────

    [Fact]
    public void Machining_Version_Is_0_2_1()
    {
        var version = InformationalVersion(typeof(MachiningFeature));
        Assert.StartsWith("0.2.1", version);
    }

    // ── Assembly: 0.2.1 ─────────────────────────────────────────────────────────

    [Fact]
    public void Assembly_Version_Is_0_2_1()
    {
        var version = InformationalVersion(typeof(AssemblyDocumentationService));
        Assert.StartsWith("0.2.1", version);
    }

    // ── Semantics: 0.2.1 ────────────────────────────────────────────────────────

    [Fact]
    public void Semantics_Version_Is_0_2_1()
    {
        var version = InformationalVersion(typeof(SemanticInferenceService));
        Assert.StartsWith("0.2.1", version);
    }

    // ── Domain: 0.3.0 ───────────────────────────────────────────────────────────

    [Fact]
    public void Domain_Version_Is_0_3_0()
    {
        var version = InformationalVersion(typeof(Skeleton));
        Assert.StartsWith("0.3.0", version);
    }

    // ── Construction: 0.3.0 ─────────────────────────────────────────────────────

    [Fact]
    public void Construction_Version_Is_0_3_0()
    {
        var version = InformationalVersion(typeof(ConstructionRuleEngine));
        Assert.StartsWith("0.3.0", version);
    }

    // ── Catalog: 0.3.0 ──────────────────────────────────────────────────────────

    [Fact]
    public void Catalog_Version_Is_0_3_0()
    {
        var version = InformationalVersion(typeof(CatalogEntry));
        Assert.StartsWith("0.3.0", version);
    }

    // ── Application: 0.3.0 ──────────────────────────────────────────────────────

    [Fact]
    public void Application_Version_Is_0_3_0()
    {
        var version = InformationalVersion(typeof(SpaceOS.Cabinet.Application.Extensions.CabinetServiceCollectionExtensions));
        Assert.StartsWith("0.3.0", version);
    }

    // ── Structural invariants ────────────────────────────────────────────────────

    [Fact]
    public void SimilarityFingerprint_Setter_Is_Not_Public()
    {
        var prop = typeof(CatalogEntry).GetProperty(
            nameof(CatalogEntry.SimilarityFingerprint),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);
        Assert.False(prop!.SetMethod?.IsPublic ?? false);
    }

    [Fact]
    public void SimilarityFingerprint_Setter_Is_Private()
    {
        var prop = typeof(CatalogEntry).GetProperty(
            nameof(CatalogEntry.SimilarityFingerprint),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);
        Assert.True(prop!.SetMethod?.IsPrivate ?? false);
    }

    [Fact]
    public void TenantStandard_Version_Property_Is_Long()
    {
        var prop = typeof(TenantStandard).GetProperty(
            nameof(TenantStandard.Version),
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);
        Assert.Equal(typeof(long), prop!.PropertyType);
    }
}
