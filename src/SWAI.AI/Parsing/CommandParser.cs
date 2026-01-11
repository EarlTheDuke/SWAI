using SWAI.Core.Commands;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;
using System.Text.RegularExpressions;

namespace SWAI.AI.Parsing;

/// <summary>
/// Parses natural language input into SWAI commands
/// </summary>
public class CommandParser
{
    private readonly UnitSystem _defaultUnit;

    public CommandParser(UnitSystem defaultUnit = UnitSystem.Inches)
    {
        _defaultUnit = defaultUnit;
    }

    /// <summary>
    /// Parse a create box command from natural language
    /// </summary>
    public CreateBoxCommand? ParseCreateBox(string input)
    {
        var dimensions = ExtractBoxDimensions(input);
        if (dimensions == null) return null;

        var name = ExtractName(input) ?? "Box";

        return new CreateBoxCommand(name, dimensions.Value.Width, dimensions.Value.Length, dimensions.Value.Height)
        {
            Centered = !input.Contains("corner", StringComparison.OrdinalIgnoreCase),
            SketchPlane = ExtractPlane(input)
        };
    }

    /// <summary>
    /// Parse a create cylinder command from natural language
    /// </summary>
    public CreateCylinderCommand? ParseCreateCylinder(string input)
    {
        var diameter = ExtractDimension(input, "diameter", "dia", "d");
        var height = ExtractDimension(input, "height", "tall", "high", "h");

        // Try to get radius if diameter not found
        if (diameter == null)
        {
            var radius = ExtractDimension(input, "radius", "r");
            if (radius != null)
            {
                diameter = radius.Value * 2;
            }
        }

        if (diameter == null || height == null) return null;

        var name = ExtractName(input) ?? "Cylinder";

        return new CreateCylinderCommand(name, diameter.Value, height.Value);
    }

    /// <summary>
    /// Parse a fillet command
    /// </summary>
    public AddFilletCommand? ParseFillet(string input)
    {
        var radius = ExtractDimension(input, "radius", "r", "fillet");

        if (radius == null)
        {
            // Try to find any dimension mentioned
            radius = ExtractFirstDimension(input);
        }

        if (radius == null) return null;

        var allEdges = input.Contains("all", StringComparison.OrdinalIgnoreCase) ||
                       input.Contains("every", StringComparison.OrdinalIgnoreCase);

        return new AddFilletCommand("Fillet", radius.Value)
        {
            AllEdges = allEdges
        };
    }

    /// <summary>
    /// Parse a chamfer command
    /// </summary>
    public AddChamferCommand? ParseChamfer(string input)
    {
        var distance = ExtractDimension(input, "distance", "chamfer", "size");

        if (distance == null)
        {
            distance = ExtractFirstDimension(input);
        }

        if (distance == null) return null;

        var allEdges = input.Contains("all", StringComparison.OrdinalIgnoreCase);

        return new AddChamferCommand("Chamfer", distance.Value)
        {
            AllEdges = allEdges
        };
    }

    /// <summary>
    /// Parse a hole command
    /// </summary>
    public AddHoleCommand? ParseHole(string input)
    {
        var diameter = ExtractDimension(input, "diameter", "dia", "d", "hole");
        var depth = ExtractDimension(input, "depth", "deep");

        if (diameter == null)
        {
            diameter = ExtractFirstDimension(input);
        }

        if (diameter == null) return null;

        var throughAll = input.Contains("through", StringComparison.OrdinalIgnoreCase) ||
                         input.Contains("thru", StringComparison.OrdinalIgnoreCase);

        return new AddHoleCommand("Hole", diameter.Value)
        {
            Depth = depth,
            ThroughAll = throughAll || depth == null
        };
    }

    /// <summary>
    /// Parse an export command
    /// </summary>
    public ExportPartCommand? ParseExport(string input)
    {
        var format = ExportFormat.STEP;

        if (input.Contains("stl", StringComparison.OrdinalIgnoreCase))
            format = ExportFormat.STL;
        else if (input.Contains("iges", StringComparison.OrdinalIgnoreCase) ||
                 input.Contains("igs", StringComparison.OrdinalIgnoreCase))
            format = ExportFormat.IGES;
        else if (input.Contains("dxf", StringComparison.OrdinalIgnoreCase))
            format = ExportFormat.DXF;
        else if (input.Contains("parasolid", StringComparison.OrdinalIgnoreCase) ||
                 input.Contains("x_t", StringComparison.OrdinalIgnoreCase))
            format = ExportFormat.Parasolid;

        // Try to extract a filename
        var fileMatch = Regex.Match(input, @"(?:as|to|named?)\s+[""']?(\w+)[""']?", RegexOptions.IgnoreCase);
        var filename = fileMatch.Success ? fileMatch.Groups[1].Value : "export";

        var extension = PartDocument.GetExtension(format);
        var filePath = $"{filename}{extension}";

        return new ExportPartCommand(filePath, format);
    }

    #region Dimension Extraction

    private (Dimension Width, Dimension Length, Dimension Height)? ExtractBoxDimensions(string input)
    {
        // Try "W x L x H" format first
        var xyzMatch = Regex.Match(
            input,
            @"(\d+\.?\d*)\s*([""']|inches?|in|mm|cm)?\s*[xX×by]\s*(\d+\.?\d*)\s*([""']|inches?|in|mm|cm)?\s*[xX×by]\s*(\d+\.?\d*)\s*([""']|inches?|in|mm|cm)?",
            RegexOptions.IgnoreCase
        );

        if (xyzMatch.Success)
        {
            var unit = ParseUnitFromMatch(xyzMatch.Groups[6].Value) ??
                       ParseUnitFromMatch(xyzMatch.Groups[4].Value) ??
                       ParseUnitFromMatch(xyzMatch.Groups[2].Value) ??
                       _defaultUnit;

            return (
                new Dimension(double.Parse(xyzMatch.Groups[1].Value), unit),
                new Dimension(double.Parse(xyzMatch.Groups[3].Value), unit),
                new Dimension(double.Parse(xyzMatch.Groups[5].Value), unit)
            );
        }

        // Try individual dimension keywords
        var width = ExtractDimension(input, "wide", "width", "w");
        var length = ExtractDimension(input, "long", "length", "l");
        var height = ExtractDimension(input, "thick", "thickness", "height", "tall", "deep", "h", "t");

        if (width != null && length != null && height != null)
        {
            return (width.Value, length.Value, height.Value);
        }

        return null;
    }

    private Dimension? ExtractDimension(string input, params string[] keywords)
    {
        foreach (var keyword in keywords)
        {
            // Pattern: "keyword value unit" or "value unit keyword"
            var patterns = new[]
            {
                $@"(\d+\.?\d*|\d+\s*/\s*\d+)\s*([""']|inches?|in|mm|cm|feet|ft)?\s*{keyword}",
                $@"{keyword}\s*[:\s=]*\s*(\d+\.?\d*|\d+\s*/\s*\d+)\s*([""']|inches?|in|mm|cm|feet|ft)?"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var value = ParseNumericValue(match.Groups[1].Value);
                    var unit = ParseUnitFromMatch(match.Groups[2].Value) ?? _defaultUnit;
                    return new Dimension(value, unit);
                }
            }
        }

        return null;
    }

    private Dimension? ExtractFirstDimension(string input)
    {
        var match = Regex.Match(
            input,
            @"(\d+\.?\d*|\d+\s*/\s*\d+)\s*([""']|inches?|in|mm|cm)?",
            RegexOptions.IgnoreCase
        );

        if (match.Success)
        {
            var value = ParseNumericValue(match.Groups[1].Value);
            var unit = ParseUnitFromMatch(match.Groups[2].Value) ?? _defaultUnit;
            return new Dimension(value, unit);
        }

        return null;
    }

    private double ParseNumericValue(string input)
    {
        input = input.Trim();

        // Check for fraction
        var fractionMatch = Regex.Match(input, @"(\d+)\s*/\s*(\d+)");
        if (fractionMatch.Success)
        {
            var numerator = double.Parse(fractionMatch.Groups[1].Value);
            var denominator = double.Parse(fractionMatch.Groups[2].Value);
            return numerator / denominator;
        }

        // Check for mixed number (e.g., "1 1/2")
        var mixedMatch = Regex.Match(input, @"(\d+)\s+(\d+)\s*/\s*(\d+)");
        if (mixedMatch.Success)
        {
            var whole = double.Parse(mixedMatch.Groups[1].Value);
            var numerator = double.Parse(mixedMatch.Groups[2].Value);
            var denominator = double.Parse(mixedMatch.Groups[3].Value);
            return whole + (numerator / denominator);
        }

        return double.Parse(input);
    }

    private UnitSystem? ParseUnitFromMatch(string unitStr)
    {
        if (string.IsNullOrWhiteSpace(unitStr)) return null;
        return UnitConverter.ParseUnit(unitStr);
    }

    #endregion

    #region Helpers

    private string? ExtractName(string input)
    {
        var match = Regex.Match(input, @"(?:named?|called?)\s+[""']?(\w+)[""']?", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private ReferencePlane ExtractPlane(string input)
    {
        if (input.Contains("front", StringComparison.OrdinalIgnoreCase))
            return ReferencePlane.Front;
        if (input.Contains("right", StringComparison.OrdinalIgnoreCase))
            return ReferencePlane.Right;
        return ReferencePlane.Top; // Default
    }

    #endregion
}
