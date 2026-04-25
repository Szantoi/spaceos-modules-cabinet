using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Machining;

public class MachiningPatternTests
{
    private static PlaneSubject ValidSubject()
        => new(Guid.NewGuid(), PartFace.Front);

    private static HardwareReference ValidHardware()
        => new("SKU-HINGE-01", "Blum");

    private static MachiningTemplate DrillTemplate()
        => new(MachiningOperation.Drill, new MachiningParameters(depth: 13.0, diameter: 35.0));

    [Fact]
    public void GenerateFeatures_ValidInput_ReturnsFeatures()
    {
        var pattern = new MachiningPattern("blum-clip", "Blum Clip Top", [DrillTemplate()]);

        var result = pattern.GenerateFeatures(ValidSubject(), ValidHardware());

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public void GenerateFeatures_NullSubject_ReturnsInvalid()
    {
        var pattern = new MachiningPattern("blum-clip", "Blum Clip Top", [DrillTemplate()]);

        var result = pattern.GenerateFeatures(null!, ValidHardware());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void GenerateFeatures_MultipleTemplates_GeneratesAll()
    {
        var templates = new[]
        {
            DrillTemplate(),
            new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters(depth: 10.0, diameter: 8.0))
        };
        var pattern = new MachiningPattern("two-drill", "Two Drills", templates);

        var result = pattern.GenerateFeatures(ValidSubject(), ValidHardware());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public void GenerateFeatures_EmptyTemplates_ReturnsEmptyList()
    {
        var pattern = new MachiningPattern("empty", "Empty Pattern", []);

        var result = pattern.GenerateFeatures(ValidSubject(), ValidHardware());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void Pattern_HasCorrectId()
    {
        var pattern = new MachiningPattern("test-id", "Description", []);

        Assert.Equal("test-id", pattern.PatternId);
    }

    [Fact]
    public void Pattern_HasCorrectDescription()
    {
        var pattern = new MachiningPattern("test-id", "My Description", []);

        Assert.Equal("My Description", pattern.Description);
    }

    [Fact]
    public void DowelPattern_GeneratesTwoDrills()
    {
        var dowelPattern = new MachiningPattern(
            "dowel-8mm",
            "8mm Dowel Pair",
            [
                new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters(depth: 30.0, diameter: 8.0)),
                new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters(depth: 30.0, diameter: 8.0))
            ]);

        var result = dowelPattern.GenerateFeatures(ValidSubject(), ValidHardware());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, f => Assert.Equal(MachiningOperation.Drill, f.Operation));
    }

    [Fact]
    public void HingePattern_GeneratesMultipleDrills()
    {
        // A typical Blum Clip Top hinge requires a 35mm cup hole + 2 mounting holes.
        var hingePattern = new MachiningPattern(
            "blum-clip-top",
            "Blum Clip Top Hinge",
            [
                new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters(depth: 13.5, diameter: 35.0)),
                new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters(depth: 10.0, diameter: 4.5)),
                new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters(depth: 10.0, diameter: 4.5))
            ]);

        var result = hingePattern.GenerateFeatures(ValidSubject(), ValidHardware());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
    }
}
