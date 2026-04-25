using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Machining;

public class MachiningFeatureTests
{
    private static PlaneSubject ValidPlaneSubject()
        => new(Guid.NewGuid(), PartFace.Front);

    private static MachiningParameters ValidParameters()
        => new(depth: 12.0, diameter: 5.0);

    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var result = MachiningFeature.Create(ValidPlaneSubject(), MachiningOperation.Drill, ValidParameters());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_NullSubject_ReturnsInvalid()
    {
        var result = MachiningFeature.Create(null!, MachiningOperation.Drill, ValidParameters());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_NullParameters_ReturnsInvalid()
    {
        var result = MachiningFeature.Create(ValidPlaneSubject(), MachiningOperation.Drill, null!);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_WithHardware_SetsHardware()
    {
        var hardware = new HardwareReference("SKU-123", "Blum");

        var result = MachiningFeature.Create(ValidPlaneSubject(), MachiningOperation.Drill, ValidParameters(), hardware);

        Assert.True(result.IsSuccess);
        Assert.Equal(hardware, result.Value.Hardware);
    }

    [Fact]
    public void Create_WithInvalidHardware_ReturnsInvalid()
    {
        var invalidHardware = new HardwareReference(string.Empty, "Blum");

        var result = MachiningFeature.Create(ValidPlaneSubject(), MachiningOperation.Drill, ValidParameters(), invalidHardware);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Create_WithoutHardware_HardwareIsNull()
    {
        var result = MachiningFeature.Create(ValidPlaneSubject(), MachiningOperation.Drill, ValidParameters());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Hardware);
    }

    [Fact]
    public void PlaneSubject_HasCorrectPartIdAndFace()
    {
        var partId = Guid.NewGuid();
        var subject = new PlaneSubject(partId, PartFace.Back);

        Assert.Equal(partId, subject.PartId);
        Assert.Equal(PartFace.Back, subject.Face);
    }

    [Fact]
    public void EdgeSubject_HasCorrectPartIdAndEdge()
    {
        var partId = Guid.NewGuid();
        var subject = new EdgeSubject(partId, PartEdge.BackLeft);

        Assert.Equal(partId, subject.PartId);
        Assert.Equal(PartEdge.BackLeft, subject.Edge);
    }

    [Fact]
    public void ConnectionSubject_HasCorrectConnectionId()
    {
        var connId = Guid.NewGuid();
        var subject = new ConnectionSubject(connId);

        Assert.Equal(connId, subject.ConnectionId);
    }

    [Fact]
    public void MachiningOperation_HasAllExpectedValues()
    {
        var values = Enum.GetValues<MachiningOperation>();

        Assert.Equal(9, values.Length);
    }

    [Fact]
    public void Parameters_AllPropertiesAreOptional()
    {
        // Default construction should succeed with all nulls.
        var parameters = new MachiningParameters();

        Assert.Null(parameters.Depth);
        Assert.Null(parameters.Width);
        Assert.Null(parameters.Diameter);
        Assert.Null(parameters.Length);
        Assert.Null(parameters.Direction);
        Assert.Null(parameters.Placement);
    }

    [Fact]
    public void Parameters_Constructor_SetsValues()
    {
        var dir = Vector3.Create(0, 0, -1).Value;
        var parameters = new MachiningParameters(depth: 10, width: 5, diameter: 8, length: 100, direction: dir);

        Assert.Equal(10, parameters.Depth);
        Assert.Equal(5, parameters.Width);
        Assert.Equal(8, parameters.Diameter);
        Assert.Equal(100, parameters.Length);
        Assert.Equal(dir, parameters.Direction);
    }
}
