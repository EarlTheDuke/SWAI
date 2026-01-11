using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWAI.AI.Models;

/// <summary>
/// JSON schema for structured command output from LLM
/// </summary>
public class CommandSchema
{
    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("parameters")]
    public CommandParameters Parameters { get; set; } = new();

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("needsClarification")]
    public bool NeedsClarification { get; set; }

    [JsonPropertyName("clarificationQuestion")]
    public string? ClarificationQuestion { get; set; }
}

public class CommandParameters
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("width")]
    public DimensionValue? Width { get; set; }

    [JsonPropertyName("length")]
    public DimensionValue? Length { get; set; }

    [JsonPropertyName("height")]
    public DimensionValue? Height { get; set; }

    [JsonPropertyName("depth")]
    public DimensionValue? Depth { get; set; }

    [JsonPropertyName("diameter")]
    public DimensionValue? Diameter { get; set; }

    [JsonPropertyName("radius")]
    public DimensionValue? Radius { get; set; }

    [JsonPropertyName("thickness")]
    public DimensionValue? Thickness { get; set; }

    [JsonPropertyName("angle")]
    public double? Angle { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("spacing")]
    public DimensionValue? Spacing { get; set; }

    [JsonPropertyName("allEdges")]
    public bool? AllEdges { get; set; }

    [JsonPropertyName("throughAll")]
    public bool? ThroughAll { get; set; }

    [JsonPropertyName("centered")]
    public bool? Centered { get; set; }

    [JsonPropertyName("plane")]
    public string? Plane { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("location")]
    [JsonConverter(typeof(FlexibleLocationConverter))]
    public LocationValue? Location { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }

    [JsonPropertyName("patternType")]
    public string? PatternType { get; set; }

    // Pattern-specific parameters
    [JsonPropertyName("rows")]
    public int? Rows { get; set; }

    [JsonPropertyName("columns")]
    public int? Columns { get; set; }

    [JsonPropertyName("featureType")]
    public string? FeatureType { get; set; }

    [JsonPropertyName("spacingX")]
    public DimensionValue? SpacingX { get; set; }

    [JsonPropertyName("spacingY")]
    public DimensionValue? SpacingY { get; set; }

    [JsonPropertyName("spacingDescription")]
    public string? SpacingDescription { get; set; }
}

public class DimensionValue
{
    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "inches";

    /// <summary>
    /// Original string representation
    /// </summary>
    [JsonPropertyName("original")]
    public string? Original { get; set; }
}

public class LocationValue
{
    [JsonPropertyName("x")]
    public DimensionValue? X { get; set; }

    [JsonPropertyName("y")]
    public DimensionValue? Y { get; set; }

    [JsonPropertyName("z")]
    public DimensionValue? Z { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; } // "center", "corner", "edge", "top face", etc.
}

/// <summary>
/// Flexible JSON converter that handles location as either a string or object
/// </summary>
public class FlexibleLocationConverter : JsonConverter<LocationValue?>
{
    public override LocationValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // If it's a string, convert to LocationValue with Reference
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return new LocationValue { Reference = stringValue };
        }

        // If it's an object, deserialize normally
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var location = new LocationValue();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()?.ToLowerInvariant();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "x":
                            location.X = JsonSerializer.Deserialize<DimensionValue>(ref reader, options);
                            break;
                        case "y":
                            location.Y = JsonSerializer.Deserialize<DimensionValue>(ref reader, options);
                            break;
                        case "z":
                            location.Z = JsonSerializer.Deserialize<DimensionValue>(ref reader, options);
                            break;
                        case "reference":
                            location.Reference = reader.GetString();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }
            
            return location;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, LocationValue? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, options);
    }
}

/// <summary>
/// Supported intent types for structured output
/// </summary>
public static class IntentTypes
{
    public const string CreateBox = "CREATE_BOX";
    public const string CreateCylinder = "CREATE_CYLINDER";
    public const string CreatePlate = "CREATE_PLATE";
    public const string CreatePart = "CREATE_PART";
    public const string AddExtrusion = "ADD_EXTRUSION";
    public const string AddCut = "ADD_CUT";
    public const string AddFillet = "ADD_FILLET";
    public const string AddChamfer = "ADD_CHAMFER";
    public const string AddHole = "ADD_HOLE";
    public const string AddPattern = "ADD_PATTERN";
    public const string ModifyDimension = "MODIFY_DIMENSION";
    public const string SavePart = "SAVE_PART";
    public const string ExportPart = "EXPORT_PART";
    public const string ClosePart = "CLOSE_PART";
    public const string Undo = "UNDO";
    public const string Redo = "REDO";
    public const string Help = "HELP";
    public const string ShowInfo = "SHOW_INFO";
    public const string Unknown = "UNKNOWN";
}
