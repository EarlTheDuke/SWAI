using FluentAssertions;
using SWAI.AI.Parsing;
using SWAI.Core.Models.Units;
using Xunit;

namespace SWAI.AI.Tests;

public class CommandParserTests
{
    private readonly CommandParser _parser = new();

    [Fact]
    public void ParseCreateBox_XYZFormat_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Create a box 10 x 20 x 5 inches";

        // Act
        var command = _parser.ParseCreateBox(input);

        // Assert
        command.Should().NotBeNull();
        command!.Width.Value.Should().Be(10);
        command.Length.Value.Should().Be(20);
        command.Height.Value.Should().Be(5);
        command.Width.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void ParseCreateBox_DescriptiveFormat_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Make a plate 36 inches wide, 96 inches long, 0.75 inches thick";

        // Act
        var command = _parser.ParseCreateBox(input);

        // Assert
        command.Should().NotBeNull();
        command!.Width.Value.Should().Be(36);
        command.Length.Value.Should().Be(96);
        command.Height.Value.Should().Be(0.75);
    }

    [Fact]
    public void ParseCreateBox_MixedUnits_ShouldUseLastUnit()
    {
        // Arrange
        var input = "Create a box 100 x 200 x 50 mm";

        // Act
        var command = _parser.ParseCreateBox(input);

        // Assert
        command.Should().NotBeNull();
        command!.Width.Unit.Should().Be(UnitSystem.Millimeters);
    }

    [Fact]
    public void ParseCreateCylinder_ShouldParseCorrectly()
    {
        // Arrange - use explicit "diameter" keyword at the start
        var input = "Create a cylinder with diameter 2 inches and height 6 inches";

        // Act
        var command = _parser.ParseCreateCylinder(input);

        // Assert
        command.Should().NotBeNull();
        command!.Diameter.Value.Should().Be(2);
        command.Height.Value.Should().Be(6);
    }

    [Fact]
    public void ParseFillet_WithRadius_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Add a 0.25 inch fillet to all edges";

        // Act
        var command = _parser.ParseFillet(input);

        // Assert
        command.Should().NotBeNull();
        command!.Radius.Value.Should().Be(0.25);
        command.AllEdges.Should().BeTrue();
    }

    [Fact]
    public void ParseChamfer_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Add a 0.5 inch chamfer";

        // Act
        var command = _parser.ParseChamfer(input);

        // Assert
        command.Should().NotBeNull();
        command!.Distance.Value.Should().Be(0.5);
    }

    [Fact]
    public void ParseHole_ThroughAll_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Cut a 1 inch through hole";

        // Act
        var command = _parser.ParseHole(input);

        // Assert
        command.Should().NotBeNull();
        command!.Diameter.Value.Should().Be(1);
        command.ThroughAll.Should().BeTrue();
    }

    [Fact]
    public void ParseExport_STEP_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Export as STEP file";

        // Act
        var command = _parser.ParseExport(input);

        // Assert
        command.Should().NotBeNull();
        command!.Format.Should().Be(Core.Models.Documents.ExportFormat.STEP);
    }

    [Fact]
    public void ParseExport_STL_ShouldParseCorrectly()
    {
        // Arrange
        var input = "Save as STL";

        // Act
        var command = _parser.ParseExport(input);

        // Assert
        command.Should().NotBeNull();
        command!.Format.Should().Be(Core.Models.Documents.ExportFormat.STL);
    }

    [Fact]
    public void ParseCreateBox_MissingDimensions_ShouldReturnNull()
    {
        // Arrange
        var input = "Create a box";

        // Act
        var command = _parser.ParseCreateBox(input);

        // Assert
        command.Should().BeNull();
    }
}
