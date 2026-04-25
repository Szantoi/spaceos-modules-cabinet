using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Machining;

public class HardwareReferenceTests
{
    [Fact]
    public void IsValid_ValidReference_ReturnsTrue()
    {
        var reference = new HardwareReference("SKU-001", "Blum");

        Assert.True(reference.IsValid());
    }

    [Fact]
    public void IsValid_EmptyCatalogId_ReturnsFalse()
    {
        var reference = new HardwareReference(string.Empty, "Blum");

        Assert.False(reference.IsValid());
    }

    [Fact]
    public void IsValid_EmptyCatalogType_ReturnsFalse()
    {
        var reference = new HardwareReference("SKU-001", string.Empty);

        Assert.False(reference.IsValid());
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new HardwareReference("SKU-001", "Blum");
        var b = new HardwareReference("SKU-001", "Blum");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Create_WithValues_SetsProperties()
    {
        var reference = new HardwareReference("SKU-999", "Häfele");

        Assert.Equal("SKU-999", reference.CatalogId);
        Assert.Equal("Häfele", reference.CatalogType);
    }
}
