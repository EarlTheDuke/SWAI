using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SWAI.AI.Models;
using SWAI.AI.Parsing;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;
using System.Text.Json;

namespace SWAI.AI.Services;

/// <summary>
/// Enhanced AI service with structured JSON output
/// </summary>
public class StructuredAIService : IAIService
{
    private readonly ILogger<StructuredAIService> _logger;
    private readonly AIConfiguration _config;
    private readonly Kernel? _kernel;
    private readonly IChatCompletionService? _chatService;
    private readonly CommandParser _parser;
    private readonly string _structuredPrompt;

    public bool IsConfigured => _kernel != null && !string.IsNullOrEmpty(_config.ApiKey);

    public StructuredAIService(AIConfiguration config, ILogger<StructuredAIService> logger)
    {
        _config = config;
        _logger = logger;
        _parser = new CommandParser();

        // Load structured prompt
        _structuredPrompt = LoadStructuredPrompt();

        if (!string.IsNullOrEmpty(config.ApiKey) && config.ApiKey != "your-openai-api-key-here")
        {
            try
            {
                var builder = Kernel.CreateBuilder();

                if (config.Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AddAzureOpenAIChatCompletion(
                        config.Model,
                        config.Endpoint ?? throw new InvalidOperationException("Azure endpoint required"),
                        config.ApiKey
                    );
                }
                else
                {
                    builder.AddOpenAIChatCompletion(config.Model, config.ApiKey);
                }

                _kernel = builder.Build();
                _chatService = _kernel.GetRequiredService<IChatCompletionService>();
                _logger.LogInformation("Structured AI Service initialized with {Provider} / {Model}", config.Provider, config.Model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AI service");
            }
        }
        else
        {
            _logger.LogWarning("AI Service not configured - using offline parsing");
        }
    }

    public async Task<AIResponse> ProcessInputAsync(string userInput, IReadOnlyList<ChatMessage>? history = null)
    {
        // Try structured AI parsing first
        if (IsConfigured && _chatService != null)
        {
            try
            {
                var schema = await GetStructuredResponseAsync(userInput, history);
                if (schema != null)
                {
                    return ConvertSchemaToResponse(schema, userInput);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Structured parsing failed, falling back to offline");
            }
        }

        // Fall back to offline parsing
        return ProcessInputOffline(userInput);
    }

    private async Task<CommandSchema?> GetStructuredResponseAsync(string userInput, IReadOnlyList<ChatMessage>? history)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(_structuredPrompt);

        // Add context from history
        if (history != null && history.Count > 0)
        {
            var contextSummary = BuildContextSummary(history);
            if (!string.IsNullOrEmpty(contextSummary))
            {
                chatHistory.AddSystemMessage($"Current context:\n{contextSummary}");
            }
        }

        chatHistory.AddUserMessage(userInput);

        var response = await _chatService!.GetChatMessageContentAsync(chatHistory);
        var jsonResponse = response.Content ?? "";

        // Clean up response (remove markdown code blocks if present)
        jsonResponse = CleanJsonResponse(jsonResponse);

        try
        {
            return JsonSerializer.Deserialize<CommandSchema>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON response: {Response}", jsonResponse);
            return null;
        }
    }

    private AIResponse ConvertSchemaToResponse(CommandSchema schema, string originalInput)
    {
        var commands = new List<ISwaiCommand>();
        var message = schema.Message;

        if (!schema.NeedsClarification)
        {
            var command = CreateCommandFromSchema(schema, originalInput);
            if (command != null)
            {
                commands.Add(command);
            }
        }
        else if (!string.IsNullOrEmpty(schema.ClarificationQuestion))
        {
            message = $"{schema.Message}\n\n{schema.ClarificationQuestion}";
        }

        return new AIResponse
        {
            Success = true,
            Message = message,
            Commands = commands,
            NeedsClarification = schema.NeedsClarification,
            Suggestions = GetSuggestionsForIntent(schema.Intent)
        };
    }

    private ISwaiCommand? CreateCommandFromSchema(CommandSchema schema, string originalInput)
    {
        return schema.Intent switch
        {
            IntentTypes.CreateBox or IntentTypes.CreatePlate => CreateBoxFromSchema(schema),
            IntentTypes.CreateCylinder => CreateCylinderFromSchema(schema),
            IntentTypes.AddFillet => CreateFilletFromSchema(schema),
            IntentTypes.AddChamfer => CreateChamferFromSchema(schema),
            IntentTypes.AddHole => CreateHoleFromSchema(schema),
            IntentTypes.AddExtrusion => CreateExtrusionFromSchema(schema),
            IntentTypes.AddCut => CreateCutFromSchema(schema),
            IntentTypes.SavePart => new SavePartCommand(),
            IntentTypes.ExportPart => CreateExportFromSchema(schema),
            IntentTypes.ClosePart => new ClosePartCommand(),
            _ => null
        };
    }

    private CreateBoxCommand? CreateBoxFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        
        var width = GetDimension(p.Width) ?? GetDimension(p.Thickness);
        var length = GetDimension(p.Length);
        var height = GetDimension(p.Height) ?? GetDimension(p.Thickness) ?? GetDimension(p.Depth);

        // For plates, thickness might be the height
        if (schema.Intent == IntentTypes.CreatePlate && p.Thickness != null)
        {
            if (height == null) height = GetDimension(p.Thickness);
            if (width == null && p.Width == null) width = GetDimension(p.Thickness);
        }

        if (width == null || length == null || height == null)
        {
            _logger.LogWarning("Missing dimensions for box: W={W}, L={L}, H={H}", 
                width?.ToString(), length?.ToString(), height?.ToString());
            return null;
        }

        var plane = ParsePlane(p.Plane);

        return new CreateBoxCommand(p.Name ?? "Box", width.Value, length.Value, height.Value)
        {
            Centered = p.Centered ?? true,
            SketchPlane = plane
        };
    }

    private CreateCylinderCommand? CreateCylinderFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        
        var diameter = GetDimension(p.Diameter);
        if (diameter == null && p.Radius != null)
        {
            var radius = GetDimension(p.Radius);
            if (radius != null)
            {
                diameter = radius.Value * 2;
            }
        }

        var height = GetDimension(p.Height) ?? GetDimension(p.Depth);

        if (diameter == null || height == null)
        {
            _logger.LogWarning("Missing dimensions for cylinder");
            return null;
        }

        return new CreateCylinderCommand(p.Name ?? "Cylinder", diameter.Value, height.Value);
    }

    private AddFilletCommand? CreateFilletFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        var radius = GetDimension(p.Radius);

        if (radius == null)
        {
            _logger.LogWarning("Missing radius for fillet");
            return null;
        }

        return new AddFilletCommand("Fillet", radius.Value)
        {
            AllEdges = p.AllEdges ?? false
        };
    }

    private AddChamferCommand? CreateChamferFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        var distance = GetDimension(p.Width) ?? GetDimension(p.Depth);

        if (distance == null)
        {
            _logger.LogWarning("Missing distance for chamfer");
            return null;
        }

        return new AddChamferCommand("Chamfer", distance.Value)
        {
            AllEdges = p.AllEdges ?? false,
            Angle = p.Angle
        };
    }

    private AddHoleCommand? CreateHoleFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        var diameter = GetDimension(p.Diameter);

        if (diameter == null)
        {
            _logger.LogWarning("Missing diameter for hole");
            return null;
        }

        var depth = GetDimension(p.Depth);
        var throughAll = p.ThroughAll ?? (depth == null);

        Point3D? location = null;
        if (p.Location != null)
        {
            var x = GetDimension(p.Location.X) ?? Dimension.Zero;
            var y = GetDimension(p.Location.Y) ?? Dimension.Zero;
            var z = GetDimension(p.Location.Z) ?? Dimension.Zero;
            location = new Point3D(x, y, z);
        }

        return new AddHoleCommand("Hole", diameter.Value)
        {
            Depth = depth,
            ThroughAll = throughAll,
            Location = location
        };
    }

    private AddExtrusionCommand? CreateExtrusionFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        var depth = GetDimension(p.Depth) ?? GetDimension(p.Height);

        if (depth == null)
        {
            _logger.LogWarning("Missing depth for extrusion");
            return null;
        }

        return new AddExtrusionCommand("Extrusion", depth.Value);
    }

    private AddExtrusionCommand? CreateCutFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        var depth = GetDimension(p.Depth) ?? GetDimension(p.Height);

        if (depth == null)
        {
            _logger.LogWarning("Missing depth for cut");
            return null;
        }

        return new AddExtrusionCommand("Cut", depth.Value, isCut: true);
    }

    private ExportPartCommand? CreateExportFromSchema(CommandSchema schema)
    {
        var p = schema.Parameters;
        var format = ParseExportFormat(p.Format);
        var filename = p.Filename ?? $"export{PartDocument.GetExtension(format)}";

        return new ExportPartCommand(filename, format);
    }

    private Dimension? GetDimension(DimensionValue? dv)
    {
        if (dv == null) return null;
        var unit = UnitConverter.ParseUnit(dv.Unit) ?? UnitSystem.Inches;
        return new Dimension(dv.Value, unit);
    }

    private ReferencePlane ParsePlane(string? plane)
    {
        if (string.IsNullOrEmpty(plane)) return ReferencePlane.Top;
        
        return plane.ToLowerInvariant() switch
        {
            "front" => ReferencePlane.Front,
            "right" => ReferencePlane.Right,
            "top" or _ => ReferencePlane.Top
        };
    }

    private ExportFormat ParseExportFormat(string? format)
    {
        if (string.IsNullOrEmpty(format)) return ExportFormat.STEP;

        return format.ToUpperInvariant() switch
        {
            "STEP" or "STP" => ExportFormat.STEP,
            "STL" => ExportFormat.STL,
            "IGES" or "IGS" => ExportFormat.IGES,
            "DXF" => ExportFormat.DXF,
            "DWG" => ExportFormat.DWG,
            "PARASOLID" or "X_T" => ExportFormat.Parasolid,
            _ => ExportFormat.STEP
        };
    }

    private string BuildContextSummary(IReadOnlyList<ChatMessage> history)
    {
        var recent = history.TakeLast(5).ToList();
        if (recent.Count == 0) return string.Empty;

        var parts = new List<string>();
        
        // Find any part creation in history
        foreach (var msg in recent)
        {
            if (msg.AssociatedCommand != null)
            {
                parts.Add($"- Previous command: {msg.AssociatedCommand.Description}");
            }
        }

        return string.Join("\n", parts);
    }

    private string CleanJsonResponse(string response)
    {
        response = response.Trim();
        
        // Remove markdown code blocks
        if (response.StartsWith("```json"))
        {
            response = response[7..];
        }
        else if (response.StartsWith("```"))
        {
            response = response[3..];
        }

        if (response.EndsWith("```"))
        {
            response = response[..^3];
        }

        return response.Trim();
    }

    private List<string> GetSuggestionsForIntent(string intent)
    {
        return intent switch
        {
            IntentTypes.CreateBox => new List<string>
            {
                "Add fillets to the edges",
                "Cut a hole in the center",
                "Save the part"
            },
            IntentTypes.CreateCylinder => new List<string>
            {
                "Add a chamfer to the top edge",
                "Cut a hole through the center",
                "Export as STL"
            },
            IntentTypes.AddFillet => new List<string>
            {
                "Add more fillets",
                "Save the part",
                "Export as STEP"
            },
            _ => new List<string>
            {
                "Create a box 10 x 20 x 5 inches",
                "Create a cylinder 2 inch diameter, 6 inches tall",
                "Help"
            }
        };
    }

    private AIResponse ProcessInputOffline(string userInput)
    {
        // Use the existing command parser for offline mode
        var commands = new List<ISwaiCommand>();
        string message;

        // Try to parse box
        var boxCmd = _parser.ParseCreateBox(userInput);
        if (boxCmd != null)
        {
            commands.Add(boxCmd);
            message = $"Creating box: {boxCmd.Description}";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Try to parse cylinder
        var cylCmd = _parser.ParseCreateCylinder(userInput);
        if (cylCmd != null)
        {
            commands.Add(cylCmd);
            message = $"Creating cylinder: {cylCmd.Description}";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Try to parse fillet
        var filletCmd = _parser.ParseFillet(userInput);
        if (filletCmd != null)
        {
            commands.Add(filletCmd);
            message = $"Adding fillet: {filletCmd.Description}";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Try to parse chamfer
        var chamferCmd = _parser.ParseChamfer(userInput);
        if (chamferCmd != null)
        {
            commands.Add(chamferCmd);
            message = $"Adding chamfer: {chamferCmd.Description}";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Try to parse hole
        var holeCmd = _parser.ParseHole(userInput);
        if (holeCmd != null)
        {
            commands.Add(holeCmd);
            message = $"Adding hole: {holeCmd.Description}";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Try to parse export
        var exportCmd = _parser.ParseExport(userInput);
        if (exportCmd != null && (userInput.Contains("export", StringComparison.OrdinalIgnoreCase) ||
                                   userInput.Contains("save as", StringComparison.OrdinalIgnoreCase)))
        {
            commands.Add(exportCmd);
            message = $"Exporting: {exportCmd.Description}";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Check for save
        if (userInput.Contains("save", StringComparison.OrdinalIgnoreCase))
        {
            commands.Add(new SavePartCommand());
            message = "Saving the part...";
            return new AIResponse { Success = true, Message = message, Commands = commands };
        }

        // Check for help
        if (userInput.Contains("help", StringComparison.OrdinalIgnoreCase) || userInput == "?")
        {
            return new AIResponse
            {
                Success = true,
                Message = GetHelpMessage(),
                Commands = new List<ISwaiCommand>()
            };
        }

        // Unknown command
        return new AIResponse
        {
            Success = true,
            Message = "I'm not sure what you'd like to do. Try:\n" +
                     "• 'Create a box 10 x 20 x 5 inches'\n" +
                     "• 'Create a cylinder 2 inch diameter, 6 inches tall'\n" +
                     "• 'Add a 0.25 inch fillet to all edges'\n" +
                     "• Type 'help' for more options",
            Commands = new List<ISwaiCommand>(),
            NeedsClarification = true
        };
    }

    public async Task<IntentResult> DetectIntentAsync(string userInput)
    {
        if (IsConfigured && _chatService != null)
        {
            try
            {
                var schema = await GetStructuredResponseAsync(userInput, null);
                if (schema != null)
                {
                    return new IntentResult
                    {
                        Intent = MapIntentType(schema.Intent),
                        Confidence = schema.Confidence,
                        OriginalInput = userInput
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Intent detection failed");
            }
        }

        // Offline intent detection
        return DetectIntentOffline(userInput);
    }

    private IntentType MapIntentType(string intent)
    {
        return intent switch
        {
            IntentTypes.CreateBox or IntentTypes.CreatePlate => IntentType.CreateBox,
            IntentTypes.CreateCylinder => IntentType.CreateCylinder,
            IntentTypes.CreatePart => IntentType.CreatePart,
            IntentTypes.AddExtrusion => IntentType.AddExtrusion,
            IntentTypes.AddCut => IntentType.AddCut,
            IntentTypes.AddFillet => IntentType.AddFillet,
            IntentTypes.AddChamfer => IntentType.AddChamfer,
            IntentTypes.AddHole => IntentType.AddHole,
            IntentTypes.SavePart => IntentType.SavePart,
            IntentTypes.ExportPart => IntentType.ExportPart,
            IntentTypes.Help => IntentType.Help,
            _ => IntentType.Unknown
        };
    }

    private IntentResult DetectIntentOffline(string input)
    {
        var lower = input.ToLowerInvariant();

        if (lower.Contains("box") || lower.Contains("plate") || lower.Contains("rectangular"))
            return new IntentResult { Intent = IntentType.CreateBox, Confidence = 0.8, OriginalInput = input };

        if (lower.Contains("cylinder") || lower.Contains("circular") || lower.Contains("round"))
            return new IntentResult { Intent = IntentType.CreateCylinder, Confidence = 0.8, OriginalInput = input };

        if (lower.Contains("fillet"))
            return new IntentResult { Intent = IntentType.AddFillet, Confidence = 0.8, OriginalInput = input };

        if (lower.Contains("chamfer"))
            return new IntentResult { Intent = IntentType.AddChamfer, Confidence = 0.8, OriginalInput = input };

        if (lower.Contains("hole"))
            return new IntentResult { Intent = IntentType.AddHole, Confidence = 0.8, OriginalInput = input };

        if (lower.Contains("save"))
            return new IntentResult { Intent = IntentType.SavePart, Confidence = 0.9, OriginalInput = input };

        if (lower.Contains("export"))
            return new IntentResult { Intent = IntentType.ExportPart, Confidence = 0.8, OriginalInput = input };

        if (lower.Contains("help"))
            return new IntentResult { Intent = IntentType.Help, Confidence = 0.9, OriginalInput = input };

        return new IntentResult { Intent = IntentType.Unknown, Confidence = 0.0, OriginalInput = input };
    }

    public Task<string> DescribeCommandsAsync(IReadOnlyList<ISwaiCommand> commands)
    {
        if (commands.Count == 0)
            return Task.FromResult("No commands to execute.");

        var descriptions = commands.Select(c => $"• {c.Description}");
        return Task.FromResult($"I will execute:\n{string.Join("\n", descriptions)}");
    }

    public Task<List<string>> GetSuggestionsAsync(string partialInput, IReadOnlyList<ChatMessage>? history = null)
    {
        return Task.FromResult(new List<string>
        {
            "Create a box 10 x 20 x 5 inches",
            "Create a cylinder 2 inch diameter, 6 inches tall",
            "Add a 0.25 inch fillet to all edges",
            "Cut a 1 inch hole in the center",
            "Save as STEP file"
        });
    }

    private string LoadStructuredPrompt()
    {
        try
        {
            var assemblyPath = Path.GetDirectoryName(typeof(StructuredAIService).Assembly.Location);
            var promptPath = Path.Combine(assemblyPath!, "Prompts", "StructuredOutputPrompt.txt");
            if (File.Exists(promptPath))
            {
                return File.ReadAllText(promptPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load structured prompt file");
        }

        // Return embedded default prompt
        return GetDefaultStructuredPrompt();
    }

    private string GetDefaultStructuredPrompt()
    {
        return @"You are SWAI, a SolidWorks AI assistant. Parse user input and return JSON with intent and parameters.
Return format: {""intent"": ""INTENT_TYPE"", ""confidence"": 0.0-1.0, ""parameters"": {...}, ""message"": ""..."", ""needsClarification"": false}
Intents: CREATE_BOX, CREATE_CYLINDER, ADD_FILLET, ADD_CHAMFER, ADD_HOLE, SAVE_PART, EXPORT_PART, HELP, UNKNOWN
Extract dimensions with units. Default to inches. Convert fractions to decimals.";
    }

    private string GetHelpMessage()
    {
        return @"**SWAI - SolidWorks AI Assistant**

I can help you create 3D parts using natural language:

**Create Parts:**
• 'Create a box 10 x 20 x 5 inches'
• 'Make a plate 36"" wide, 96"" long, 3/4"" thick'
• 'Create a cylinder 2 inch diameter, 6 inches tall'

**Add Features:**
• 'Add a 0.25 inch fillet to all edges'
• 'Add a 0.5 inch chamfer'
• 'Cut a 1 inch hole in the center'
• 'Add 4 holes 2 inches apart'

**Modify:**
• 'Make it thicker' (I'll ask how much)
• 'Add another hole'

**Save & Export:**
• 'Save the part'
• 'Export as STEP'
• 'Save as STL'

What would you like to create?";
    }
}
