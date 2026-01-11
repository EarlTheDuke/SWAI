using SWAI.Core.Commands;
using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Units;
using System.Text.RegularExpressions;

namespace SWAI.AI.Parsing;

/// <summary>
/// Parser for assembly-related commands
/// </summary>
public class AssemblyParser
{
    private readonly UnitSystem _defaultUnit;

    public AssemblyParser(UnitSystem defaultUnit = UnitSystem.Inches)
    {
        _defaultUnit = defaultUnit;
    }

    /// <summary>
    /// Try to parse an assembly command from natural language
    /// </summary>
    public ISwaiCommand? TryParse(string input)
    {
        var lower = input.ToLowerInvariant().Trim();

        // Create assembly
        if (lower.Contains("create") && lower.Contains("assembly"))
        {
            return ParseCreateAssembly(input);
        }

        // Mate commands - check before insert to handle "add a mate" vs "add a component"
        if (lower.Contains("mate") || lower.Contains("coincident") || 
            lower.Contains("concentric") || lower.Contains("parallel") ||
            lower.Contains("perpendicular") || lower.Contains("distance mate") ||
            lower.Contains("angle mate"))
        {
            return ParseMateCommand(input);
        }

        // Insert component
        if ((lower.Contains("insert") || lower.Contains("add")) && 
            (lower.Contains("component") || lower.Contains("part")))
        {
            return ParseInsertComponent(input);
        }

        // Fix/Float component
        if (lower.Contains("fix") || lower.Contains("ground") || lower.Contains("float"))
        {
            return ParseFixCommand(input);
        }

        // Move component
        if (lower.Contains("move") && lower.Contains("component"))
        {
            return ParseMoveComponent(input);
        }

        // Rotate component
        if (lower.Contains("rotate") && lower.Contains("component"))
        {
            return ParseRotateComponent(input);
        }

        // Save assembly
        if (lower.Contains("save") && lower.Contains("assembly"))
        {
            return new SaveAssemblyCommand();
        }

        return null;
    }

    private CreateAssemblyCommand? ParseCreateAssembly(string input)
    {
        // Try to extract name
        var nameMatch = Regex.Match(input, @"(?:called?|named?)\s+[""']?(\w+)[""']?", RegexOptions.IgnoreCase);
        var name = nameMatch.Success ? nameMatch.Groups[1].Value : "Assembly1";

        return new CreateAssemblyCommand(name);
    }

    private InsertComponentCommand? ParseInsertComponent(string input)
    {
        // Try to find a file path or part name
        var pathMatch = Regex.Match(input, @"[""']?([A-Za-z]:\\[^""']+\.sldprt)[""']?", RegexOptions.IgnoreCase);
        if (pathMatch.Success)
        {
            return new InsertComponentCommand(pathMatch.Groups[1].Value);
        }

        // Try to find just a part name
        var nameMatch = Regex.Match(input, @"(?:insert|add)\s+(?:the\s+)?(?:component|part)?\s*[""']?(\w+)[""']?", RegexOptions.IgnoreCase);
        if (nameMatch.Success)
        {
            var partName = nameMatch.Groups[1].Value;
            // Assume it's a filename in current directory
            return new InsertComponentCommand($"{partName}.sldprt");
        }

        return null;
    }

    private ISwaiCommand? ParseMateCommand(string input)
    {
        var lower = input.ToLowerInvariant();

        // Determine mate type
        MateType mateType;
        if (lower.Contains("coincident"))
            mateType = MateType.Coincident;
        else if (lower.Contains("concentric"))
            mateType = MateType.Concentric;
        else if (lower.Contains("distance"))
            mateType = MateType.Distance;
        else if (lower.Contains("angle"))
            mateType = MateType.Angle;
        else if (lower.Contains("parallel"))
            mateType = MateType.Parallel;
        else if (lower.Contains("perpendicular"))
            mateType = MateType.Perpendicular;
        else
            mateType = MateType.Coincident; // default

        // Try to extract component names
        // Pattern: "mate Part1 to Part2" or "mate Part1's face to Part2's face"
        var componentPattern = @"(\w+(?:-\d+)?)\s*(?:'s\s*)?(\w+)?\s+(?:to|and|with)\s+(\w+(?:-\d+)?)\s*(?:'s\s*)?(\w+)?";
        var match = Regex.Match(input, componentPattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var comp1 = match.Groups[1].Value;
            var face1 = match.Groups[2].Success ? match.Groups[2].Value : "Face1";
            var comp2 = match.Groups[3].Value;
            var face2 = match.Groups[4].Success ? match.Groups[4].Value : "Face1";

            // Extract distance if present
            Dimension? distance = null;
            if (mateType == MateType.Distance)
            {
                var dimMatch = Regex.Match(input, @"(\d+\.?\d*)\s*(inch|inches|in|""|mm)?", RegexOptions.IgnoreCase);
                if (dimMatch.Success)
                {
                    var value = double.Parse(dimMatch.Groups[1].Value);
                    var unit = UnitConverter.ParseUnit(dimMatch.Groups[2].Value) ?? _defaultUnit;
                    distance = new Dimension(value, unit);
                }
            }

            // Extract angle if present
            double? angle = null;
            if (mateType == MateType.Angle)
            {
                var angleMatch = Regex.Match(input, @"(\d+\.?\d*)\s*(?:degrees?|°)?");
                if (angleMatch.Success)
                {
                    angle = double.Parse(angleMatch.Groups[1].Value);
                }
            }

            var entity1 = new MateReference
            {
                ComponentName = comp1,
                EntityType = "Face",
                EntityName = face1
            };

            var entity2 = new MateReference
            {
                ComponentName = comp2,
                EntityType = "Face",
                EntityName = face2
            };

            return new AddMateCommand($"{mateType}Mate", mateType, entity1, entity2)
            {
                Distance = distance,
                Angle = angle
            };
        }

        return null;
    }

    private ISwaiCommand? ParseFixCommand(string input)
    {
        var lower = input.ToLowerInvariant();
        var fix = lower.Contains("fix") || lower.Contains("ground");

        // Find component name
        var nameMatch = Regex.Match(input, @"(?:fix|ground|float)\s+(?:the\s+)?(?:component\s+)?[""']?(\w+(?:-\d+)?)[""']?", RegexOptions.IgnoreCase);
        if (nameMatch.Success)
        {
            return new FixComponentCommand(nameMatch.Groups[1].Value, fix);
        }

        return null;
    }

    private ISwaiCommand? ParseMoveComponent(string input)
    {
        var nameMatch = Regex.Match(input, @"move\s+(?:the\s+)?(?:component\s+)?[""']?(\w+(?:-\d+)?)[""']?", RegexOptions.IgnoreCase);
        if (!nameMatch.Success) return null;

        var componentName = nameMatch.Groups[1].Value;

        // Try to extract position or offset
        // Pattern: "move X by 2 inches in Y direction"
        var offsetMatch = Regex.Match(input, @"by\s+(\d+\.?\d*)\s*(inch|inches|in|""|mm)?\s*(?:in\s+)?([xyz])?", RegexOptions.IgnoreCase);
        if (offsetMatch.Success)
        {
            var value = double.Parse(offsetMatch.Groups[1].Value);
            var unit = UnitConverter.ParseUnit(offsetMatch.Groups[2].Value) ?? _defaultUnit;
            var direction = offsetMatch.Groups[3].Value.ToUpperInvariant();

            var offset = direction switch
            {
                "X" => new Core.Models.Geometry.Vector3D(value, 0, 0, unit),
                "Y" => new Core.Models.Geometry.Vector3D(0, value, 0, unit),
                "Z" => new Core.Models.Geometry.Vector3D(0, 0, value, unit),
                _ => new Core.Models.Geometry.Vector3D(value, 0, 0, unit)
            };

            return new MoveComponentCommand(componentName) { Offset = offset };
        }

        return new MoveComponentCommand(componentName);
    }

    private ISwaiCommand? ParseRotateComponent(string input)
    {
        var nameMatch = Regex.Match(input, @"rotate\s+(?:the\s+)?(?:component\s+)?[""']?(\w+(?:-\d+)?)[""']?", RegexOptions.IgnoreCase);
        if (!nameMatch.Success) return null;

        var componentName = nameMatch.Groups[1].Value;

        // Try to extract angle
        var angleMatch = Regex.Match(input, @"(\d+\.?\d*)\s*(?:degrees?|°)?\s*(?:about|around)?\s*([xyz])?", RegexOptions.IgnoreCase);
        if (angleMatch.Success)
        {
            var angle = double.Parse(angleMatch.Groups[1].Value);
            var axis = angleMatch.Groups[2].Value.ToUpperInvariant();

            return axis switch
            {
                "X" => new RotateComponentCommand(componentName, angleX: angle),
                "Y" => new RotateComponentCommand(componentName, angleY: angle),
                "Z" => new RotateComponentCommand(componentName, angleZ: angle),
                _ => new RotateComponentCommand(componentName, angleZ: angle) // default to Z
            };
        }

        return new RotateComponentCommand(componentName);
    }

    /// <summary>
    /// Check if input appears to be assembly-related
    /// </summary>
    public static bool IsAssemblyRelated(string input)
    {
        var lower = input.ToLowerInvariant();
        var keywords = new[]
        {
            "assembly", "component", "insert", "mate", "coincident", "concentric",
            "fix", "ground", "float", "suppress", "unsuppress"
        };

        return keywords.Any(k => lower.Contains(k));
    }
}
