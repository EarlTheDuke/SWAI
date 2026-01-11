using FluentAssertions;
using SWAI.AI.Parsing;
using SWAI.Core.Commands;
using SWAI.Core.Models.Assembly;
using Xunit;

namespace SWAI.AI.Tests;

public class AssemblyParserTests
{
    private readonly AssemblyParser _parser = new();

    [Fact]
    public void TryParse_CreateAssembly_ShouldReturnCreateCommand()
    {
        // Arrange
        var input = "Create a new assembly called Cabinet";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<CreateAssemblyCommand>();
        var cmd = (CreateAssemblyCommand)command!;
        cmd.AssemblyName.Should().Be("Cabinet");
    }

    [Fact]
    public void TryParse_InsertComponent_ShouldReturnInsertCommand()
    {
        // Arrange
        var input = "Insert the component Side";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<InsertComponentCommand>();
        var cmd = (InsertComponentCommand)command!;
        cmd.ComponentPath.Should().Contain("Side");
    }

    [Fact]
    public void TryParse_CoincidentMate_ShouldReturnMateCommand()
    {
        // Arrange
        var input = "Add a coincident mate between Part1-1 and Part2-1";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<AddMateCommand>();
        var cmd = (AddMateCommand)command!;
        cmd.MateType.Should().Be(MateType.Coincident);
    }

    [Fact]
    public void TryParse_ConcentricMate_ShouldReturnMateCommand()
    {
        // Arrange
        var input = "Make a concentric mate between Shaft-1 and Hole-1";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<AddMateCommand>();
        var cmd = (AddMateCommand)command!;
        cmd.MateType.Should().Be(MateType.Concentric);
    }

    [Fact]
    public void TryParse_FixComponent_ShouldReturnFixCommand()
    {
        // Arrange
        var input = "Fix the component Base-1";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<FixComponentCommand>();
        var cmd = (FixComponentCommand)command!;
        cmd.ComponentName.Should().Be("Base-1");
        cmd.Fix.Should().BeTrue();
    }

    [Fact]
    public void TryParse_FloatComponent_ShouldReturnFixCommandWithFixFalse()
    {
        // Arrange
        var input = "Float the component Part1-1";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<FixComponentCommand>();
        var cmd = (FixComponentCommand)command!;
        cmd.Fix.Should().BeFalse();
    }

    [Fact]
    public void TryParse_SaveAssembly_ShouldReturnSaveCommand()
    {
        // Arrange
        var input = "Save the assembly";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<SaveAssemblyCommand>();
    }

    [Fact]
    public void TryParse_NonAssemblyCommand_ShouldReturnNull()
    {
        // Arrange
        var input = "Create a box 10 x 20 x 5 inches";

        // Act
        var command = _parser.TryParse(input);

        // Assert
        command.Should().BeNull();
    }

    [Fact]
    public void IsAssemblyRelated_WithAssemblyKeyword_ShouldReturnTrue()
    {
        // Arrange & Act & Assert
        AssemblyParser.IsAssemblyRelated("Create an assembly").Should().BeTrue();
        AssemblyParser.IsAssemblyRelated("Insert a component").Should().BeTrue();
        AssemblyParser.IsAssemblyRelated("Add a coincident mate").Should().BeTrue();
        AssemblyParser.IsAssemblyRelated("Fix the part").Should().BeTrue();
    }

    [Fact]
    public void IsAssemblyRelated_WithPartKeyword_ShouldReturnFalse()
    {
        // Arrange & Act & Assert
        AssemblyParser.IsAssemblyRelated("Create a box").Should().BeFalse();
        AssemblyParser.IsAssemblyRelated("Add a fillet").Should().BeFalse();
    }
}
