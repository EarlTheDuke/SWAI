using SWAI.Core.Commands;
using SWAI.Core.Models.Units;
using SWAI.Core.Services;
using System.Text.RegularExpressions;

namespace SWAI.AI.Parsing;

/// <summary>
/// Parses incremental/contextual commands like "make it thicker", "add another one"
/// </summary>
public class IncrementalParser
{
    private readonly ConversationContext _context;
    private readonly CommandParser _baseParser;

    public IncrementalParser(ConversationContext context)
    {
        _context = context;
        _baseParser = new CommandParser();
    }

    /// <summary>
    /// Try to parse an incremental command
    /// </summary>
    public ISwaiCommand? TryParseIncremental(string input)
    {
        var lower = input.ToLowerInvariant().Trim();

        // Pattern: "make it [bigger/smaller/thicker/etc]"
        if (lower.StartsWith("make it") || lower.StartsWith("make the"))
        {
            return ParseMakeItCommand(lower);
        }

        // Pattern: "add another [feature]"
        if (lower.Contains("another") || lower.Contains("one more"))
        {
            return ParseAddAnotherCommand(lower);
        }

        // Pattern: "increase/decrease [dimension]"
        if (lower.StartsWith("increase") || lower.StartsWith("decrease"))
        {
            return ParseIncreaseDecreaseCommand(lower);
        }

        // Pattern: "double/halve [dimension]"
        if (lower.Contains("double") || lower.Contains("halve") || lower.Contains("half"))
        {
            return ParseScaleCommand(lower);
        }

        // Pattern: "same as before" / "repeat"
        if (lower.Contains("same") || lower.Contains("repeat") || lower == "again")
        {
            return RepeatLastCommand();
        }

        return null;
    }

    private ISwaiCommand? ParseMakeItCommand(string input)
    {
        // Extract the modifier
        var modifiers = new Dictionary<string, (DimensionType, ModificationType)>
        {
            { "thicker", (DimensionType.Thickness, ModificationType.IncreaseBy) },
            { "thinner", (DimensionType.Thickness, ModificationType.DecreaseBy) },
            { "wider", (DimensionType.Width, ModificationType.IncreaseBy) },
            { "narrower", (DimensionType.Width, ModificationType.DecreaseBy) },
            { "longer", (DimensionType.Length, ModificationType.IncreaseBy) },
            { "shorter", (DimensionType.Length, ModificationType.DecreaseBy) },
            { "taller", (DimensionType.Height, ModificationType.IncreaseBy) },
            { "deeper", (DimensionType.Depth, ModificationType.IncreaseBy) },
            { "bigger", (DimensionType.Width, ModificationType.IncreaseBy) }, // General resize
            { "smaller", (DimensionType.Width, ModificationType.DecreaseBy) },
            { "larger", (DimensionType.Width, ModificationType.IncreaseBy) }
        };

        foreach (var (keyword, (dimType, modType)) in modifiers)
        {
            if (input.Contains(keyword))
            {
                // Try to extract amount
                var amount = ExtractDimensionFromInput(input);
                
                if (amount == null)
                {
                    // Use a default increment (10% or 0.5 inches)
                    var lastDim = _context.GetLastDimensionLike(dimType.ToString());
                    amount = lastDim != null 
                        ? new Dimension(lastDim.Value.Value * 0.1, lastDim.Value.Unit)
                        : Dimension.Inches(0.5);
                }

                return new ModifyDimensionCommand(dimType, modType, amount.Value);
            }
        }

        return null;
    }

    private ISwaiCommand? ParseAddAnotherCommand(string input)
    {
        // Check what the last command was and try to repeat it
        if (_context.LastCommand == null)
            return null;

        // Clone the last command with potential modifications
        return _context.LastCommand switch
        {
            AddHoleCommand hole => new AddHoleCommand(hole.FeatureName + "_2", hole.Diameter)
            {
                Depth = hole.Depth,
                ThroughAll = hole.ThroughAll
            },
            AddFilletCommand fillet => new AddFilletCommand(fillet.FeatureName + "_2", fillet.Radius)
            {
                AllEdges = fillet.AllEdges
            },
            _ => null
        };
    }

    private ISwaiCommand? ParseIncreaseDecreaseCommand(string input)
    {
        var isIncrease = input.StartsWith("increase");
        var modType = isIncrease ? ModificationType.IncreaseBy : ModificationType.DecreaseBy;

        // Determine which dimension
        var dimType = DimensionType.Width; // default
        
        if (input.Contains("width")) dimType = DimensionType.Width;
        else if (input.Contains("length")) dimType = DimensionType.Length;
        else if (input.Contains("height") || input.Contains("tall")) dimType = DimensionType.Height;
        else if (input.Contains("thick")) dimType = DimensionType.Thickness;
        else if (input.Contains("depth") || input.Contains("deep")) dimType = DimensionType.Depth;
        else if (input.Contains("diameter") || input.Contains("dia")) dimType = DimensionType.Diameter;
        else if (input.Contains("radius")) dimType = DimensionType.Radius;

        // Extract amount
        var amount = ExtractDimensionFromInput(input);
        if (amount == null)
        {
            // Need clarification
            return null;
        }

        return new ModifyDimensionCommand(dimType, modType, amount.Value);
    }

    private ISwaiCommand? ParseScaleCommand(string input)
    {
        var dimType = DimensionType.Width;
        ModificationType modType;
        Dimension value;

        if (input.Contains("double"))
        {
            modType = ModificationType.MultiplyBy;
            value = new Dimension(2, UnitSystem.Inches); // Scalar
        }
        else if (input.Contains("halve") || input.Contains("half"))
        {
            modType = ModificationType.DivideBy;
            value = new Dimension(2, UnitSystem.Inches);
        }
        else
        {
            return null;
        }

        // Try to find which dimension
        if (input.Contains("width")) dimType = DimensionType.Width;
        else if (input.Contains("length")) dimType = DimensionType.Length;
        else if (input.Contains("height")) dimType = DimensionType.Height;
        else if (input.Contains("thick")) dimType = DimensionType.Thickness;

        return new ModifyDimensionCommand(dimType, modType, value);
    }

    private ISwaiCommand? RepeatLastCommand()
    {
        // Just return the last command to repeat it
        return _context.LastCommand;
    }

    private Dimension? ExtractDimensionFromInput(string input)
    {
        // Pattern: number + optional unit
        var match = Regex.Match(input, @"(\d+\.?\d*|\d+/\d+)\s*(inch|inches|in|""|mm|cm)?", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            var valueStr = match.Groups[1].Value;
            var unitStr = match.Groups[2].Value;

            double value;
            if (valueStr.Contains('/'))
            {
                var parts = valueStr.Split('/');
                value = double.Parse(parts[0]) / double.Parse(parts[1]);
            }
            else
            {
                value = double.Parse(valueStr);
            }

            var unit = UnitConverter.ParseUnit(unitStr) ?? _context.DefaultUnits;
            return new Dimension(value, unit);
        }

        // Check for word numbers
        var wordNumbers = new Dictionary<string, double>
        {
            { "quarter", 0.25 }, { "half", 0.5 }, { "one", 1 }, { "two", 2 },
            { "three", 3 }, { "four", 4 }, { "five", 5 }, { "ten", 10 }
        };

        foreach (var (word, num) in wordNumbers)
        {
            if (input.Contains(word))
            {
                return new Dimension(num, _context.DefaultUnits);
            }
        }

        return null;
    }
}

/// <summary>
/// Extension to CommandParser for pattern parsing
/// </summary>
public static class PatternParserExtensions
{
    /// <summary>
    /// Parse a linear pattern command
    /// </summary>
    public static AddLinearPatternCommand? ParseLinearPattern(this CommandParser parser, string input)
    {
        var lower = input.ToLowerInvariant();
        
        // Pattern: "X [features] [spacing] apart"
        var countMatch = Regex.Match(lower, @"(\d+)\s*(holes?|instances?|copies?|features?)?");
        var spacingMatch = Regex.Match(input, @"(\d+\.?\d*)\s*(inch|inches|in|""|mm)?\s*(apart|spacing|between)", RegexOptions.IgnoreCase);

        if (!countMatch.Success)
            return null;

        var count = int.Parse(countMatch.Groups[1].Value);
        
        Dimension spacing;
        if (spacingMatch.Success)
        {
            var value = double.Parse(spacingMatch.Groups[1].Value);
            var unit = UnitConverter.ParseUnit(spacingMatch.Groups[2].Value) ?? UnitSystem.Inches;
            spacing = new Dimension(value, unit);
        }
        else
        {
            spacing = Dimension.Inches(1); // default
        }

        return new AddLinearPatternCommand("Pattern", count, spacing);
    }

    /// <summary>
    /// Parse a circular pattern command
    /// </summary>
    public static AddCircularPatternCommand? ParseCircularPattern(this CommandParser parser, string input)
    {
        var lower = input.ToLowerInvariant();

        if (!lower.Contains("circular") && !lower.Contains("around") && !lower.Contains("radial"))
            return null;

        var countMatch = Regex.Match(lower, @"(\d+)\s*(holes?|instances?|copies?)?");
        if (!countMatch.Success)
            return null;

        var count = int.Parse(countMatch.Groups[1].Value);

        // Check for partial circle
        var angleMatch = Regex.Match(lower, @"(\d+)\s*(degrees?|°)");
        var totalAngle = 360.0;
        if (angleMatch.Success)
        {
            totalAngle = double.Parse(angleMatch.Groups[1].Value);
        }

        return new AddCircularPatternCommand("CircularPattern", count)
        {
            TotalAngle = totalAngle
        };
    }
}
