using SpaceOS.Cabinet.Assembly;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Assembly;

public class ExplodedViewTests
{
    [Fact]
    public void HardwareCallout_IsValid_WithValidData()
    {
        var hardware = new HardwareReference("SKU-001", "Blum");
        var callout = new HardwareCallout(hardware, Guid.NewGuid(), Vector3.Zero, "Hinge");

        Assert.True(callout.IsValid());
    }

    [Fact]
    public void HardwareCallout_IsValid_WithEmptyLabel_ReturnsFalse()
    {
        var hardware = new HardwareReference("SKU-001", "Blum");
        var callout = new HardwareCallout(hardware, Guid.NewGuid(), Vector3.Zero, "");

        Assert.False(callout.IsValid());
    }

    [Fact]
    public void HardwareCallout_IsValid_WithEmptyPartId_ReturnsFalse()
    {
        var hardware = new HardwareReference("SKU-001", "Blum");
        var callout = new HardwareCallout(hardware, Guid.Empty, Vector3.Zero, "Hinge");

        Assert.False(callout.IsValid());
    }

    [Fact]
    public void ExplodedView_HasLayers()
    {
        var layers = new List<ExplodedViewLayer>
        {
            new ExplodedViewLayer(0, new[] { Guid.NewGuid() })
        };

        var view = new ExplodedView(layers);

        Assert.Single(view.Layers);
    }

    [Fact]
    public void ExplodedViewLayer_HasPartIds()
    {
        var partId = Guid.NewGuid();
        var layer = new ExplodedViewLayer(0, new[] { partId });

        Assert.Contains(partId, layer.PartIds);
        Assert.Equal(0, layer.LayerIndex);
    }
}
