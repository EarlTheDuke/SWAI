using FluentAssertions;
using SWAI.AI.Parsing;
using SWAI.Core.Commands;
using SWAI.Core.Services;
using Xunit;

namespace SWAI.AI.Tests;

public class IncrementalParserTests
{
    private readonly ConversationContext _context;
    private readonly IncrementalParser _parser;

    public IncrementalParserTests()
    {
        _context = new ConversationContext();
        _parser = new IncrementalParser(_context);
    }

    [Fact]
    public void ParseMakeItThicker_ShouldReturnModifyCommand()
    {
        // Arrange
        var input = "make it thicker";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<ModifyDimensionCommand>();
        var modCmd = (ModifyDimensionCommand)command!;
        modCmd.DimensionType.Should().Be(DimensionType.Thickness);
        modCmd.ModificationType.Should().Be(ModificationType.IncreaseBy);
    }

    [Fact]
    public void ParseMakeItThickerWithAmount_ShouldIncludeValue()
    {
        // Arrange
        var input = "make it 2 inches thicker";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<ModifyDimensionCommand>();
        var modCmd = (ModifyDimensionCommand)command!;
        modCmd.Value.Value.Should().Be(2);
    }

    [Fact]
    public void ParseIncreaseWidth_ShouldReturnCorrectCommand()
    {
        // Arrange
        var input = "increase the width by 5 inches";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<ModifyDimensionCommand>();
        var modCmd = (ModifyDimensionCommand)command!;
        modCmd.DimensionType.Should().Be(DimensionType.Width);
        modCmd.ModificationType.Should().Be(ModificationType.IncreaseBy);
        modCmd.Value.Value.Should().Be(5);
    }

    [Fact]
    public void ParseDoubleTheWidth_ShouldReturnMultiplyCommand()
    {
        // Arrange
        var input = "double the width";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<ModifyDimensionCommand>();
        var modCmd = (ModifyDimensionCommand)command!;
        modCmd.ModificationType.Should().Be(ModificationType.MultiplyBy);
        modCmd.Value.Value.Should().Be(2);
    }

    [Fact]
    public void ParseHalveTheHeight_ShouldReturnDivideCommand()
    {
        // Arrange
        var input = "halve the height";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<ModifyDimensionCommand>();
        var modCmd = (ModifyDimensionCommand)command!;
        modCmd.ModificationType.Should().Be(ModificationType.DivideBy);
    }

    [Fact]
    public void ParseAddAnotherHole_WithContext_ShouldReturnNewHoleCommand()
    {
        // Arrange - set up context with previous hole
        var previousHole = new AddHoleCommand("Hole1", Core.Models.Units.Dimension.Inches(0.5))
        {
            ThroughAll = true
        };
        _context.LastCommand = previousHole;

        var input = "add another one";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<AddHoleCommand>();
        var holeCmd = (AddHoleCommand)command!;
        holeCmd.Diameter.Value.Should().Be(0.5);
    }

    [Fact]
    public void ParseRegularCommand_ShouldReturnNull()
    {
        // Arrange
        var input = "create a box 10 x 20 x 5 inches";

        // Act
        var command = _parser.TryParseIncremental(input);

        // Assert
        command.Should().BeNull();
    }
}
