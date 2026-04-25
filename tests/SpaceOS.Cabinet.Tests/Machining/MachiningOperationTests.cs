using SpaceOS.Cabinet.Machining;
using Xunit;

namespace SpaceOS.Cabinet.Tests.Machining;

public class MachiningOperationTests
{
    [Theory]
    [InlineData(MachiningOperation.Drill)]
    [InlineData(MachiningOperation.Groove)]
    [InlineData(MachiningOperation.Rabbet)]
    [InlineData(MachiningOperation.Pocket)]
    [InlineData(MachiningOperation.Profile)]
    [InlineData(MachiningOperation.EdgeBand)]
    [InlineData(MachiningOperation.Cut)]
    [InlineData(MachiningOperation.Chamfer)]
    [InlineData(MachiningOperation.Round)]
    public void MachiningOperation_EachValueIsDefined(MachiningOperation operation)
    {
        Assert.True(Enum.IsDefined(operation));
    }

    [Fact]
    public void MachiningFeature_Create_NewId_IsAssigned()
    {
        var subject = new PlaneSubject(Guid.NewGuid(), SpaceOS.Cabinet.Abstractions.PartFace.Front);
        var parameters = new MachiningParameters(depth: 10);

        var result = MachiningFeature.Create(subject, MachiningOperation.Drill, parameters);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Fact]
    public void MachiningFeature_TwoCreates_HaveDifferentIds()
    {
        var subject = new PlaneSubject(Guid.NewGuid(), SpaceOS.Cabinet.Abstractions.PartFace.Front);
        var parameters = new MachiningParameters(depth: 10);

        var a = MachiningFeature.Create(subject, MachiningOperation.Drill, parameters).Value;
        var b = MachiningFeature.Create(subject, MachiningOperation.Drill, parameters).Value;

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void MachiningFeature_SubjectAndOperation_ArePreserved()
    {
        var partId = Guid.NewGuid();
        var subject = new EdgeSubject(partId, SpaceOS.Cabinet.Abstractions.PartEdge.BackLeft);
        var parameters = new MachiningParameters(depth: 8, width: 5.2);

        var result = MachiningFeature.Create(subject, MachiningOperation.Groove, parameters);

        Assert.True(result.IsSuccess);
        Assert.Equal(subject, result.Value.Subject);
        Assert.Equal(MachiningOperation.Groove, result.Value.Operation);
    }

    [Fact]
    public void MachiningTemplate_Record_HasCorrectValues()
    {
        var parameters = new MachiningParameters(depth: 13.5, diameter: 35.0);
        var template = new MachiningTemplate(MachiningOperation.Drill, parameters);

        Assert.Equal(MachiningOperation.Drill, template.Operation);
        Assert.Equal(parameters, template.Parameters);
    }

    [Fact]
    public void MachiningPattern_Templates_AreReadOnly()
    {
        var pattern = new MachiningPattern("test", "desc",
        [
            new MachiningTemplate(MachiningOperation.Drill, new MachiningParameters())
        ]);

        Assert.IsAssignableFrom<IReadOnlyList<MachiningTemplate>>(pattern.Templates);
    }
}
