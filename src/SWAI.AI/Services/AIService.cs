using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Units;
using System.Text.Json;

namespace SWAI.AI.Services;

/// <summary>
/// AI service implementation using Semantic Kernel
/// </summary>
public class AIService : IAIService
{
    private readonly ILogger<AIService> _logger;
    private readonly AIConfiguration _config;
    private readonly Kernel? _kernel;
    private readonly IChatCompletionService? _chatService;

    public bool IsConfigured => _kernel != null && !string.IsNullOrEmpty(_config.ApiKey);

    public AIService(AIConfiguration config, ILogger<AIService> logger)
    {
        _config = config;
        _logger = logger;

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
                _logger.LogInformation("AI Service initialized with {Provider} / {Model}", config.Provider, config.Model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize AI service");
            }
        }
        else
        {
            _logger.LogWarning("AI Service not configured - API key missing");
        }
    }

    public async Task<AIResponse> ProcessInputAsync(string userInput, IReadOnlyList<ChatMessage>? history = null)
    {
        if (!IsConfigured || _chatService == null)
        {
            // Return a helpful response even without AI
            return ProcessInputOffline(userInput);
        }

        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetSystemPrompt());

            // Add conversation history
            if (history != null)
            {
                foreach (var msg in history.TakeLast(10))
                {
                    if (msg.Role == "user")
                        chatHistory.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant")
                        chatHistory.AddAssistantMessage(msg.Content);
                }
            }

            chatHistory.AddUserMessage(userInput);

            var response = await _chatService.GetChatMessageContentAsync(chatHistory);
            var responseText = response.Content ?? "";

            // Parse the response for commands
            var commands = ParseCommandsFromResponse(responseText, userInput);

            return new AIResponse
            {
                Success = true,
                Message = ExtractMessageFromResponse(responseText),
                Commands = commands,
                NeedsClarification = responseText.Contains("?") && commands.Count == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI request");
            return new AIResponse
            {
                Success = false,
                Error = ex.Message,
                Message = "I encountered an error processing your request. Please try again."
            };
        }
    }

    public async Task<IntentResult> DetectIntentAsync(string userInput)
    {
        // Use pattern matching for offline detection
        var intent = DetectIntentOffline(userInput);

        if (IsConfigured && _chatService != null && intent.Intent == IntentType.Unknown)
        {
            try
            {
                var prompt = $@"Analyze this user input for a CAD modeling application and identify the intent.
User input: ""{userInput}""

Respond with ONLY one of these intents:
- CREATE_BOX: User wants to create a rectangular/box part
- CREATE_CYLINDER: User wants to create a cylindrical part
- CREATE_PART: User wants to create a new part
- ADD_EXTRUSION: User wants to extrude a sketch
- ADD_CUT: User wants to cut/remove material
- ADD_FILLET: User wants to add rounded edges
- ADD_CHAMFER: User wants to add angled edges
- ADD_HOLE: User wants to add a hole
- SAVE_PART: User wants to save
- EXPORT_PART: User wants to export to different format
- HELP: User needs help
- UNKNOWN: Cannot determine intent

Intent:";

                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage(prompt);
                var response = await _chatService.GetChatMessageContentAsync(chatHistory);

                intent = ParseIntentFromAI(response.Content ?? "", userInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting intent via AI");
            }
        }

        return intent;
    }

    public Task<string> DescribeCommandsAsync(IReadOnlyList<ISwaiCommand> commands)
    {
        if (commands.Count == 0)
            return Task.FromResult("No commands to execute.");

        var descriptions = commands.Select(c => $"• {c.Description}");
        return Task.FromResult($"I will execute the following operations:\n{string.Join("\n", descriptions)}");
    }

    public Task<List<string>> GetSuggestionsAsync(string partialInput, IReadOnlyList<ChatMessage>? history = null)
    {
        // Return common suggestions
        return Task.FromResult(new List<string>
        {
            "Create a box 10 x 20 x 5 inches",
            "Create a cylinder 2 inch diameter, 6 inches tall",
            "Add a 0.25 inch fillet to all edges",
            "Cut a 1 inch hole in the center",
            "Save as STEP file",
            "Show current part info"
        });
    }

    private string GetSystemPrompt()
    {
        return @"You are SWAI, a SolidWorks AI assistant that helps users create 3D CAD models through natural language.

Your role is to:
1. Understand what the user wants to create or modify
2. Extract dimensions and parameters from their description
3. Respond with clear confirmation of what you will create

When users describe parts, extract:
- Dimensions (width, length, height, diameter, depth)
- Units (inches, mm, cm - default to inches if not specified)
- Shape type (box, cylinder, plate, etc.)
- Features (fillets, chamfers, holes, cuts)

Always respond in this format:
1. Brief confirmation of what you understood
2. The specific dimensions and parameters
3. Any clarifying questions if needed

Example:
User: ""Make a part 36 inches wide, 96 inches long, and 3/4 inch thick""
Response: ""I'll create a rectangular plate with these dimensions:
- Width: 36 inches
- Length: 96 inches  
- Thickness: 0.75 inches

Creating the part now...""

Be concise and professional. Focus on CAD operations.";
    }

    private AIResponse ProcessInputOffline(string userInput)
    {
        var intent = DetectIntentOffline(userInput);
        var commands = new List<ISwaiCommand>();
        string message;

        switch (intent.Intent)
        {
            case IntentType.CreateBox:
                var boxCmd = CreateBoxCommandFromInput(userInput, intent.Parameters);
                if (boxCmd != null)
                {
                    commands.Add(boxCmd);
                    message = $"Creating box: {boxCmd.Description}";
                }
                else
                {
                    message = "I detected you want to create a box. Please specify dimensions like: 'Create a box 10 x 20 x 5 inches'";
                }
                break;

            case IntentType.CreateCylinder:
                var cylCmd = CreateCylinderCommandFromInput(userInput, intent.Parameters);
                if (cylCmd != null)
                {
                    commands.Add(cylCmd);
                    message = $"Creating cylinder: {cylCmd.Description}";
                }
                else
                {
                    message = "I detected you want to create a cylinder. Please specify like: 'Create a cylinder 2 inch diameter, 6 inches tall'";
                }
                break;

            case IntentType.Help:
                message = GetHelpMessage();
                break;

            case IntentType.SavePart:
                commands.Add(new SavePartCommand());
                message = "Saving the current part...";
                break;

            default:
                message = "I'm not sure what you'd like to do. Try commands like:\n" +
                         "• 'Create a box 10 x 20 x 5 inches'\n" +
                         "• 'Create a cylinder 2 inch diameter, 6 inches tall'\n" +
                         "• 'Save the part'";
                break;
        }

        return new AIResponse
        {
            Success = true,
            Message = message,
            Commands = commands,
            NeedsClarification = commands.Count == 0 && intent.Intent != IntentType.Help
        };
    }

    private IntentResult DetectIntentOffline(string input)
    {
        var lower = input.ToLowerInvariant();
        var parameters = new Dictionary<string, string>();

        // Detect box/plate creation
        if (lower.Contains("box") || lower.Contains("plate") || lower.Contains("rectangular") ||
            (lower.Contains("create") && lower.Contains("part") && ContainsDimensions(lower)))
        {
            ExtractDimensionsFromInput(input, parameters);
            return new IntentResult
            {
                Intent = IntentType.CreateBox,
                Confidence = 0.8,
                Parameters = parameters,
                OriginalInput = input
            };
        }

        // Detect cylinder creation
        if (lower.Contains("cylinder") || lower.Contains("circular") || lower.Contains("round"))
        {
            ExtractDimensionsFromInput(input, parameters);
            return new IntentResult
            {
                Intent = IntentType.CreateCylinder,
                Confidence = 0.8,
                Parameters = parameters,
                OriginalInput = input
            };
        }

        // Detect fillet
        if (lower.Contains("fillet") || lower.Contains("round edge"))
        {
            return new IntentResult { Intent = IntentType.AddFillet, Confidence = 0.8, OriginalInput = input };
        }

        // Detect chamfer
        if (lower.Contains("chamfer") || lower.Contains("bevel"))
        {
            return new IntentResult { Intent = IntentType.AddChamfer, Confidence = 0.8, OriginalInput = input };
        }

        // Detect hole
        if (lower.Contains("hole") || lower.Contains("drill"))
        {
            return new IntentResult { Intent = IntentType.AddHole, Confidence = 0.8, OriginalInput = input };
        }

        // Detect save
        if (lower.Contains("save"))
        {
            return new IntentResult { Intent = IntentType.SavePart, Confidence = 0.9, OriginalInput = input };
        }

        // Detect export
        if (lower.Contains("export") || lower.Contains("step") || lower.Contains("stl"))
        {
            return new IntentResult { Intent = IntentType.ExportPart, Confidence = 0.8, OriginalInput = input };
        }

        // Detect help
        if (lower.Contains("help") || lower.Contains("how") || lower == "?")
        {
            return new IntentResult { Intent = IntentType.Help, Confidence = 0.9, OriginalInput = input };
        }

        return new IntentResult { Intent = IntentType.Unknown, Confidence = 0.0, OriginalInput = input };
    }

    private bool ContainsDimensions(string input)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(input, @"\d+\.?\d*\s*(inch|in|mm|cm|""|\'|x)");
    }

    private void ExtractDimensionsFromInput(string input, Dictionary<string, string> parameters)
    {
        // Pattern for "X x Y x Z" format
        var xyzMatch = System.Text.RegularExpressions.Regex.Match(
            input,
            @"(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?\s*[xX×]\s*(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?\s*[xX×]\s*(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (xyzMatch.Success)
        {
            parameters["width"] = xyzMatch.Groups[1].Value;
            parameters["length"] = xyzMatch.Groups[3].Value;
            parameters["height"] = xyzMatch.Groups[5].Value;
            parameters["unit"] = !string.IsNullOrEmpty(xyzMatch.Groups[6].Value)
                ? xyzMatch.Groups[6].Value
                : !string.IsNullOrEmpty(xyzMatch.Groups[2].Value)
                    ? xyzMatch.Groups[2].Value
                    : "inches";
            return;
        }

        // Pattern for "width W, length L, height H" format
        var widthMatch = System.Text.RegularExpressions.Regex.Match(input, @"(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?\s*(wide|width)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var lengthMatch = System.Text.RegularExpressions.Regex.Match(input, @"(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?\s*(long|length)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var heightMatch = System.Text.RegularExpressions.Regex.Match(input, @"(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?\s*(tall|height|thick|deep)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (widthMatch.Success) parameters["width"] = widthMatch.Groups[1].Value;
        if (lengthMatch.Success) parameters["length"] = lengthMatch.Groups[1].Value;
        if (heightMatch.Success) parameters["height"] = heightMatch.Groups[1].Value;

        // Extract diameter for cylinders
        var diameterMatch = System.Text.RegularExpressions.Regex.Match(input, @"(\d+\.?\d*)\s*(inches?|in|""|mm|cm)?\s*(diameter|dia)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (diameterMatch.Success) parameters["diameter"] = diameterMatch.Groups[1].Value;
    }

    private CreateBoxCommand? CreateBoxCommandFromInput(string input, Dictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("width", out var widthStr) ||
            !parameters.TryGetValue("length", out var lengthStr) ||
            !parameters.TryGetValue("height", out var heightStr))
        {
            return null;
        }

        var unit = UnitSystem.Inches;
        if (parameters.TryGetValue("unit", out var unitStr))
        {
            unit = UnitConverter.ParseUnit(unitStr) ?? UnitSystem.Inches;
        }

        return new CreateBoxCommand(
            "Box",
            new Dimension(double.Parse(widthStr), unit),
            new Dimension(double.Parse(lengthStr), unit),
            new Dimension(double.Parse(heightStr), unit)
        );
    }

    private CreateCylinderCommand? CreateCylinderCommandFromInput(string input, Dictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("diameter", out var diameterStr) ||
            !parameters.TryGetValue("height", out var heightStr))
        {
            return null;
        }

        var unit = UnitSystem.Inches;
        return new CreateCylinderCommand(
            "Cylinder",
            new Dimension(double.Parse(diameterStr), unit),
            new Dimension(double.Parse(heightStr), unit)
        );
    }

    private List<ISwaiCommand> ParseCommandsFromResponse(string response, string originalInput)
    {
        // For now, fall back to offline parsing
        var intent = DetectIntentOffline(originalInput);
        var commands = new List<ISwaiCommand>();

        if (intent.Intent == IntentType.CreateBox)
        {
            var cmd = CreateBoxCommandFromInput(originalInput, intent.Parameters);
            if (cmd != null) commands.Add(cmd);
        }
        else if (intent.Intent == IntentType.CreateCylinder)
        {
            var cmd = CreateCylinderCommandFromInput(originalInput, intent.Parameters);
            if (cmd != null) commands.Add(cmd);
        }

        return commands;
    }

    private string ExtractMessageFromResponse(string response)
    {
        // Clean up the response for display
        return response.Trim();
    }

    private IntentResult ParseIntentFromAI(string aiResponse, string originalInput)
    {
        var response = aiResponse.Trim().ToUpperInvariant();

        var intent = response switch
        {
            var r when r.Contains("CREATE_BOX") => IntentType.CreateBox,
            var r when r.Contains("CREATE_CYLINDER") => IntentType.CreateCylinder,
            var r when r.Contains("CREATE_PART") => IntentType.CreatePart,
            var r when r.Contains("ADD_EXTRUSION") => IntentType.AddExtrusion,
            var r when r.Contains("ADD_CUT") => IntentType.AddCut,
            var r when r.Contains("ADD_FILLET") => IntentType.AddFillet,
            var r when r.Contains("ADD_CHAMFER") => IntentType.AddChamfer,
            var r when r.Contains("ADD_HOLE") => IntentType.AddHole,
            var r when r.Contains("SAVE") => IntentType.SavePart,
            var r when r.Contains("EXPORT") => IntentType.ExportPart,
            var r when r.Contains("HELP") => IntentType.Help,
            _ => IntentType.Unknown
        };

        var parameters = new Dictionary<string, string>();
        ExtractDimensionsFromInput(originalInput, parameters);

        return new IntentResult
        {
            Intent = intent,
            Confidence = 0.9,
            Parameters = parameters,
            OriginalInput = originalInput
        };
    }

    private string GetHelpMessage()
    {
        return @"**SWAI - SolidWorks AI Assistant**

I can help you create 3D parts using natural language. Here's what I can do:

**Create Parts:**
• 'Create a box 10 x 20 x 5 inches'
• 'Make a plate 36"" wide, 96"" long, 0.75"" thick'
• 'Create a cylinder 2 inch diameter, 6 inches tall'

**Add Features:**
• 'Add a 0.25 inch fillet to all edges'
• 'Add a 0.5 inch chamfer'
• 'Cut a 1 inch diameter hole in the center'

**Save & Export:**
• 'Save the part'
• 'Export as STEP file'
• 'Save as STL'

**Tips:**
• I understand inches, mm, cm, and fractional dimensions (3/4"")
• Specify width x length x height for boxes
• Use 'diameter' and 'height' for cylinders

What would you like to create?";
    }
}
