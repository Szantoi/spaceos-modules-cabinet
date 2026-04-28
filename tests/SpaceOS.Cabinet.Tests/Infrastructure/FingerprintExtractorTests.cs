using System.Text.Json;
using SpaceOS.Cabinet.Catalog.Infrastructure;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="DefaultCatalogFingerprintExtractor"/>.
/// </summary>
public class FingerprintExtractorTests
{
    private static readonly DefaultCatalogFingerprintExtractor Extractor = new();

    private static JsonDocument ParseJson(string json) => JsonDocument.Parse(json);

    // ── Successful extraction ─────────────────────────────────────────────────

    [Fact]
    public void Extract_WithVendorCodeVariant_ReturnsNormalizedFingerprint()
    {
        var payload = ParseJson("""{"vendor":"Blum","code":"ABC-123","variant":"steel"}""");

        var result = Extractor.Extract(CatalogType.HardwareSet, payload);

        Assert.Equal("hardwareset:blum:abc-123:steel", result);
    }

    [Fact]
    public void Extract_DifferentCatalogTypes_UsesTypeName()
    {
        var payload = ParseJson("""{"vendor":"X","code":"Y","variant":"Z"}""");

        var fpHR = Extractor.Extract(CatalogType.HorizontalRole, payload);
        var fpMT = Extractor.Extract(CatalogType.MaterialThickness, payload);

        Assert.StartsWith("horizontalrole:", fpHR);
        Assert.StartsWith("materialthickness:", fpMT);
    }

    [Fact]
    public void Extract_AllLowercase_Normalized()
    {
        var payload = ParseJson("""{"vendor":"ACME","code":"CODE-99","variant":"VARIANT-A"}""");

        var result = Extractor.Extract(CatalogType.EdgeBandingRule, payload);

        Assert.Equal("edgebandingrule:acme:code-99:variant-a", result);
    }

    [Fact]
    public void Extract_VendorWithUppercase_LowercasesOutput()
    {
        var payload = ParseJson("""{"vendor":"Häfele","code":"111.22.333","variant":"standard"}""");

        var result = Extractor.Extract(CatalogType.HardwareSet, payload);

        Assert.NotNull(result);
        Assert.Equal(result, result!.ToLowerInvariant());
    }

    [Fact]
    public void Extract_ExtraFields_IgnoresThem()
    {
        var payload = ParseJson("""{"vendor":"X","code":"Y","variant":"Z","extra":"ignored","count":5}""");

        var result = Extractor.Extract(CatalogType.JointType, payload);

        Assert.Equal("jointtype:x:y:z", result);
    }

    // ── Missing required fields ───────────────────────────────────────────────

    [Fact]
    public void Extract_MissingVendor_ReturnsNull()
    {
        var payload = ParseJson("""{"code":"ABC","variant":"v1"}""");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_MissingCode_ReturnsNull()
    {
        var payload = ParseJson("""{"vendor":"X","variant":"v1"}""");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_MissingVariant_ReturnsNull()
    {
        var payload = ParseJson("""{"vendor":"X","code":"Y"}""");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_EmptyPayload_ReturnsNull()
    {
        var payload = ParseJson("{}");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_NullPayload_ReturnsNull()
    {
        var result = Extractor.Extract(CatalogType.HorizontalRole, null!);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_EmptyVendorString_ReturnsNull()
    {
        var payload = ParseJson("""{"vendor":"","code":"Y","variant":"Z"}""");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_WhitespaceVendorString_ReturnsNull()
    {
        var payload = ParseJson("""{"vendor":"  ","code":"Y","variant":"Z"}""");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }

    [Fact]
    public void Extract_NumericCodeField_ReturnsNull()
    {
        // code is a number, not a string — should return null
        var payload = ParseJson("""{"vendor":"X","code":123,"variant":"Z"}""");

        var result = Extractor.Extract(CatalogType.HorizontalRole, payload);

        Assert.Null(result);
    }
}
