using FluentAssertions;
using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Units;
using Xunit;

namespace SWAI.Core.Tests;

public class AssemblyTests
{
    [Fact]
    public void AssemblyDocument_CreateNew_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var assembly = new AssemblyDocument("TestAssembly");

        // Assert
        assembly.Name.Should().Be("TestAssembly");
        assembly.Components.Should().BeEmpty();
        assembly.Mates.Should().BeEmpty();
        assembly.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void AssemblyDocument_AddComponent_ShouldAddToList()
    {
        // Arrange
        var assembly = new AssemblyDocument("TestAssembly");
        var component = new AssemblyComponent("Part1", @"C:\Parts\Part1.sldprt");

        // Act
        assembly.AddComponent(component);

        // Assert
        assembly.Components.Should().HaveCount(1);
        assembly.Components[0].Should().Be(component);
    }

    [Fact]
    public void AssemblyDocument_FindComponent_ShouldFindByName()
    {
        // Arrange
        var assembly = new AssemblyDocument("TestAssembly");
        var component = new AssemblyComponent("Part1", @"C:\Parts\Part1.sldprt");
        assembly.AddComponent(component);

        // Act
        var found = assembly.FindComponent("Part1");

        // Assert
        found.Should().NotBeNull();
        found.Should().Be(component);
    }

    [Fact]
    public void AssemblyDocument_FindComponent_ShouldFindByInstanceName()
    {
        // Arrange
        var assembly = new AssemblyDocument("TestAssembly");
        var component = new AssemblyComponent("Part1", @"C:\Parts\Part1.sldprt")
        {
            InstanceName = "Part1-1"
        };
        assembly.AddComponent(component);

        // Act
        var found = assembly.FindComponent("Part1-1");

        // Assert
        found.Should().NotBeNull();
        found.Should().Be(component);
    }

    [Fact]
    public void AssemblyComponent_DefaultInstanceName_ShouldBeCorrect()
    {
        // Arrange & Act
        var component = new AssemblyComponent("TestPart", @"C:\Parts\TestPart.sldprt");

        // Assert
        component.InstanceName.Should().Be("TestPart-1");
        component.InstanceNumber.Should().Be(1);
    }

    [Fact]
    public void ComponentTransform_AtPosition_ShouldSetCoordinates()
    {
        // Arrange & Act
        var transform = ComponentTransform.AtPosition(0.1, 0.2, 0.3);

        // Assert
        transform.X.Should().Be(0.1);
        transform.Y.Should().Be(0.2);
        transform.Z.Should().Be(0.3);
    }

    [Fact]
    public void AssemblyMate_Coincident_ShouldCreateCorrectMate()
    {
        // Arrange
        var entity1 = MateReference.Face("Part1-1", "Face1");
        var entity2 = MateReference.Face("Part2-1", "Face1");

        // Act
        var mate = AssemblyMate.Coincident("Coincident1", entity1, entity2);

        // Assert
        mate.Type.Should().Be(MateType.Coincident);
        mate.Entity1.ComponentName.Should().Be("Part1-1");
        mate.Entity2.ComponentName.Should().Be("Part2-1");
    }

    [Fact]
    public void AssemblyMate_Distance_ShouldIncludeDistanceValue()
    {
        // Arrange
        var entity1 = MateReference.Face("Part1-1", "TopFace");
        var entity2 = MateReference.Face("Part2-1", "BottomFace");
        var distance = Dimension.Inches(2);

        // Act
        var mate = AssemblyMate.DistanceMate("Distance1", entity1, entity2, distance);

        // Assert
        mate.Type.Should().Be(MateType.Distance);
        mate.Distance.Should().NotBeNull();
        mate.Distance!.Value.Value.Should().Be(2);
    }

    [Fact]
    public void AssemblyMate_Angle_ShouldIncludeAngleValue()
    {
        // Arrange
        var entity1 = MateReference.Face("Part1-1", "Face1");
        var entity2 = MateReference.Face("Part2-1", "Face1");

        // Act
        var mate = AssemblyMate.AngleMate("Angle1", entity1, entity2, 45);

        // Assert
        mate.Type.Should().Be(MateType.Angle);
        mate.Angle.Should().Be(45);
    }

    [Fact]
    public void MateReference_Face_ShouldCreateCorrectReference()
    {
        // Arrange & Act
        var reference = MateReference.Face("Part1-1", "Front Face");

        // Assert
        reference.ComponentName.Should().Be("Part1-1");
        reference.EntityType.Should().Be("Face");
        reference.EntityName.Should().Be("Front Face");
    }

    [Fact]
    public void MateReference_Plane_ShouldCreateCorrectReference()
    {
        // Arrange & Act
        var reference = MateReference.Plane("Part1-1", "Front Plane");

        // Assert
        reference.EntityType.Should().Be("Plane");
        reference.EntityName.Should().Be("Front Plane");
    }

    [Fact]
    public void AssemblyDocument_AddMate_ShouldAddToList()
    {
        // Arrange
        var assembly = new AssemblyDocument("TestAssembly");
        var mate = new AssemblyMate("TestMate", MateType.Coincident);

        // Act
        assembly.AddMate(mate);

        // Assert
        assembly.Mates.Should().HaveCount(1);
        assembly.Mates[0].Should().Be(mate);
    }
}
