using FluentAssertions;
using SWAI.Core.Models.Units;
using Xunit;

namespace SWAI.Core.Tests;

public class DimensionTests
{
    [Fact]
    public void Parse_SimpleInches_ShouldParseCorrectly()
    {
        // Arrange & Act
        var dimension = Dimension.Parse("36 inches");

        // Assert
        dimension.Value.Should().Be(36);
        dimension.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Parse_DecimalInches_ShouldParseCorrectly()
    {
        // Arrange & Act
        var dimension = Dimension.Parse("0.75 in");

        // Assert
        dimension.Value.Should().Be(0.75);
        dimension.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Parse_QuoteNotation_ShouldParseAsInches()
    {
        // Arrange & Act
        var dimension = Dimension.Parse("36\"");

        // Assert
        dimension.Value.Should().Be(36);
        dimension.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Parse_Millimeters_ShouldParseCorrectly()
    {
        // Arrange & Act
        var dimension = Dimension.Parse("500mm");

        // Assert
        dimension.Value.Should().Be(500);
        dimension.Unit.Should().Be(UnitSystem.Millimeters);
    }

    [Fact]
    public void Parse_FractionalInches_ShouldParseCorrectly()
    {
        // Arrange & Act
        var dimension = Dimension.Parse("3/4 inch");

        // Assert
        dimension.Value.Should().Be(0.75);
        dimension.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Parse_NoUnit_ShouldUseDefault()
    {
        // Arrange & Act
        var dimension = Dimension.Parse("10", UnitSystem.Millimeters);

        // Assert
        dimension.Value.Should().Be(10);
        dimension.Unit.Should().Be(UnitSystem.Millimeters);
    }

    [Fact]
    public void ConvertTo_InchesToMillimeters_ShouldConvertCorrectly()
    {
        // Arrange
        var inches = Dimension.Inches(1);

        // Act
        var mm = inches.ConvertTo(UnitSystem.Millimeters);

        // Assert
        mm.Value.Should().BeApproximately(25.4, 0.001);
        mm.Unit.Should().Be(UnitSystem.Millimeters);
    }

    [Fact]
    public void ConvertTo_MillimetersToInches_ShouldConvertCorrectly()
    {
        // Arrange
        var mm = Dimension.Millimeters(25.4);

        // Act
        var inches = mm.ConvertTo(UnitSystem.Inches);

        // Assert
        inches.Value.Should().BeApproximately(1.0, 0.001);
        inches.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Meters_ShouldReturnCorrectValue()
    {
        // Arrange
        var dimension = Dimension.Inches(1);

        // Act
        var meters = dimension.Meters;

        // Assert
        meters.Should().BeApproximately(0.0254, 0.00001);
    }

    [Fact]
    public void Addition_SameSameUnits_ShouldAddCorrectly()
    {
        // Arrange
        var a = Dimension.Inches(10);
        var b = Dimension.Inches(5);

        // Act
        var result = a + b;

        // Assert
        result.Value.Should().Be(15);
        result.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Addition_DifferentUnits_ShouldConvertAndAdd()
    {
        // Arrange
        var inches = Dimension.Inches(1);
        var mm = Dimension.Millimeters(25.4);

        // Act
        var result = inches + mm;

        // Assert
        result.Value.Should().BeApproximately(2, 0.001);
        result.Unit.Should().Be(UnitSystem.Inches);
    }

    [Fact]
    public void Equality_SameDimensions_ShouldBeEqual()
    {
        // Arrange
        var a = Dimension.Inches(1);
        var b = Dimension.Millimeters(25.4);

        // Act & Assert
        a.Should().Be(b);
    }

    [Fact]
    public void Multiplication_ByScalar_ShouldMultiplyCorrectly()
    {
        // Arrange
        var dimension = Dimension.Inches(10);

        // Act
        var result = dimension * 2;

        // Assert
        result.Value.Should().Be(20);
        result.Unit.Should().Be(UnitSystem.Inches);
    }
}
