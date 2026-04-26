using Ardalis.Result;
using SpaceOS.Cabinet.Catalog;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Catalog;

public class CatalogPayloadValidatorTests
{
    private readonly CatalogPayloadValidator _validator = new();

    [Fact]
    public void Validate_ValidHorizontalRole_Succeeds()
    {
        var result = _validator.Validate(
            CatalogType.HorizontalRole,
            "horizontalRole/v1",
            """{"role":"Shelf","priority":1}""");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ValidMaterialThickness_Succeeds()
    {
        var result = _validator.Validate(
            CatalogType.MaterialThickness,
            "materialThickness/v1",
            """{"value":18,"unit":"mm","material":"Particleboard"}""");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ValidConstructionTemplate_Succeeds()
    {
        var result = _validator.Validate(
            CatalogType.ConstructionTemplate,
            "constructionTemplate/v1",
            """{"rules":["R-32mm-LineBore","R-Default-Joint"]}""");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ValidBackPanelStandard_Succeeds()
    {
        var result = _validator.Validate(
            CatalogType.BackPanelStandard,
            "backPanelStandard/v1",
            """{"thickness":4,"attachment":"Groove","material":"HDF"}""");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ValidRasterStandard_Succeeds()
    {
        var result = _validator.Validate(
            CatalogType.RasterStandard,
            "rasterStandard/v1",
            """{"pitch":32,"firstHole":38,"holeDiameter":5}""");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_UnknownSchemaVersion_ReturnsError()
    {
        var result = _validator.Validate(
            CatalogType.HorizontalRole,
            "unknown/v99",
            """{"role":"Shelf","priority":1}""");

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Validate_InvalidJson_ReturnsError()
    {
        var result = _validator.Validate(
            CatalogType.HorizontalRole,
            "horizontalRole/v1",
            "not json {{");

        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Validate_EmptyPayload_ReturnsError()
    {
        var result = _validator.Validate(
            CatalogType.HorizontalRole,
            "horizontalRole/v1",
            "");

        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
