using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Preview;
using SWAI.Core.Services;
using System.Text;
using System.Text.Json;

namespace SWAI.AI.Services;

/// <summary>
/// Service for generating and managing command previews
/// </summary>
public class CommandPreviewService : ICommandPreviewService
{
    private readonly ILogger<CommandPreviewService> _logger;
    private readonly Kernel? _kernel;
    private readonly AIConfiguration _config;
    private readonly ConversationContext _context;
    private readonly List<CommandPreviewResult> _history = new();
    private readonly object _historyLock = new();

    private const string PreviewSystemPrompt = """
        You are a CAD command analyzer for SWAI (SolidWorks AI). Your job is to analyze natural language commands
        and generate a structured preview of what actions will be taken.

        For each user input, respond with ONLY a JSON object in this exact format:
        {
          "actions": [
            {
              "sequence": 1,
              "type": "Create|Modify|Delete|Move|Mate|Export|Save|Query|Undo|Redo",
              "description": "Human-readable description of the action",
              "targetEntity": "Entity name or null",
              "secondaryEntity": "Second entity for mates or null",
              "parameters": { "key": "value" },
              "commandType": "CreateBoxCommand|AddFilletCommand|etc",
              "apiDetails": "SolidWorks API method that will be called",
              "isReversible": true,
              "confidence": 0.95
            }
          ],
          "overallConfidence": 0.95,
          "riskLevel": "Low|Medium|High|Critical",
          "warnings": [
            {
              "severity": "Info|Warning|Error",
              "message": "Warning text",
              "actionSequence": 1,
              "resolution": "How to fix"
            }
          ],
          "suggestions": ["Optional improvement suggestions"],
          "estimatedSeconds": 2
        }

        Risk levels:
        - Low: Creating parts, adding features, queries - easily reversible
        - Medium: Modifying existing geometry, moving components
        - High: Deleting features, overwriting files
        - Critical: Deleting entire documents, irreversible operations

        Always include relevant parameters like dimensions, positions, angles.
        For dimensions, include both value and unit (e.g., "2 inches", "50mm").
        """;

    public PreviewMode CurrentMode { get; set; } = PreviewMode.Detailed;
    public bool AutoExecuteLowRisk { get; set; } = false;
    public bool AlwaysShowDetailed { get; set; } = true;
    public int MaxHistoryCount { get; set; } = 10;

    public IReadOnlyList<CommandPreviewResult> PreviewHistory
    {
        get
        {
            lock (_historyLock)
            {
                return _history.AsReadOnly();
            }
        }
    }

    public event EventHandler<CommandPreviewResult>? PreviewGenerated;

    public CommandPreviewService(
        Kernel? kernel,
        AIConfiguration config,
        ConversationContext context,
        ILogger<CommandPreviewService> logger)
    {
        _kernel = kernel;
        _config = config;
        _context = context;
        _logger = logger;
    }

    public async Task<CommandPreviewResult> GeneratePreviewAsync(string input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating preview for: {Input}", input);

        CommandPreviewResult preview;

        if (_kernel != null && !string.IsNullOrEmpty(_config.ApiKey))
        {
            preview = await GenerateAIPreviewAsync(input, cancellationToken);
        }
        else
        {
            // Fallback to basic preview without AI
            preview = GenerateBasicPreview(input);
        }

        AddToHistory(preview);
        PreviewGenerated?.Invoke(this, preview);

        return preview;
    }

    public Task<CommandPreviewResult> GeneratePreviewFromCommandAsync(ISwaiCommand command, CancellationToken cancellationToken = default)
    {
        var preview = new CommandPreviewResult
        {
            OriginalInput = command.Description,
            Confidence = 1.0,
            RiskLevel = DetermineRiskLevel(command),
            Actions = new List<PreviewAction>
            {
                new PreviewAction
                {
                    Sequence = 1,
                    Type = DetermineActionType(command),
                    Description = command.Description,
                    CommandType = command.CommandType,
                    IsReversible = command.CanUndo,
                    Confidence = 1.0,
                    Parameters = ExtractCommandParameters(command)
                }
            }
        };

        AddToHistory(preview);
        PreviewGenerated?.Invoke(this, preview);

        return Task.FromResult(preview);
    }

    public string FormatPreview(CommandPreviewResult preview, PreviewMode? modeOverride = null)
    {
        var mode = modeOverride ?? CurrentMode;
        var sb = new StringBuilder();

        switch (mode)
        {
            case PreviewMode.Compact:
                sb.AppendLine($"📋 {preview.Summary}");
                if (preview.Warnings.Count > 0)
                    sb.AppendLine($"   ⚠️ {preview.Warnings.Count} warning(s)");
                break;

            case PreviewMode.Detailed:
                sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
                sb.AppendLine("║                      COMMAND PREVIEW                          ║");
                sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
                sb.AppendLine();
                sb.AppendLine($"Input: \"{preview.OriginalInput}\"");
                sb.AppendLine($"Confidence: {preview.Confidence:P0} | Risk: {preview.RiskLevel}");
                sb.AppendLine();
                sb.AppendLine("PLANNED ACTIONS:");
                sb.AppendLine("─────────────────────────────────────────────────────────────────");
                
                foreach (var action in preview.Actions)
                {
                    var icon = GetActionIcon(action.Type);
                    sb.AppendLine($"  {action.Sequence}. {icon} [{action.Type}] {action.Description}");
                    
                    if (!string.IsNullOrEmpty(action.TargetEntity))
                        sb.AppendLine($"       Target: {action.TargetEntity}");
                    
                    if (action.Parameters.Count > 0)
                    {
                        var paramStr = string.Join(", ", action.Parameters.Select(p => $"{p.Key}={p.Value}"));
                        sb.AppendLine($"       Parameters: {paramStr}");
                    }
                }

                if (preview.Warnings.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("WARNINGS:");
                    sb.AppendLine("─────────────────────────────────────────────────────────────────");
                    foreach (var warning in preview.Warnings)
                    {
                        var icon = warning.Severity == WarningSeverity.Error ? "❌" :
                                   warning.Severity == WarningSeverity.Warning ? "⚠️" : "ℹ️";
                        sb.AppendLine($"  {icon} {warning.Message}");
                        if (!string.IsNullOrEmpty(warning.Resolution))
                            sb.AppendLine($"     → {warning.Resolution}");
                    }
                }
                break;

            case PreviewMode.Verbose:
                sb.AppendLine(FormatPreview(preview, PreviewMode.Detailed));
                sb.AppendLine();
                sb.AppendLine("API DETAILS:");
                sb.AppendLine("─────────────────────────────────────────────────────────────────");
                foreach (var action in preview.Actions)
                {
                    sb.AppendLine($"  {action.Sequence}. Command: {action.CommandType}");
                    if (!string.IsNullOrEmpty(action.ApiDetails))
                        sb.AppendLine($"     API: {action.ApiDetails}");
                }
                break;
        }

        return sb.ToString();
    }

    public IEnumerable<FormattedPreviewAction> GetFormattedActions(CommandPreviewResult preview)
    {
        return preview.Actions.Select(a => new FormattedPreviewAction
        {
            Sequence = a.Sequence,
            Icon = GetActionIcon(a.Type),
            TypeName = a.Type.ToString(),
            Description = a.Description,
            Target = a.TargetEntity,
            Parameters = a.Parameters.Count > 0
                ? string.Join(", ", a.Parameters.Select(p => $"{p.Key}: {p.Value}"))
                : null,
            ColorCode = GetActionColor(a.Type),
            ConfidenceDisplay = $"{a.Confidence:P0}",
            HasWarnings = preview.Warnings.Any(w => w.ActionSequence == a.Sequence),
            ApiDetails = a.ApiDetails
        });
    }

    public void MarkExecuted(Guid previewId)
    {
        lock (_historyLock)
        {
            var preview = _history.FirstOrDefault(p => p.Id == previewId);
            if (preview != null)
            {
                preview.IsExecuted = true;
            }
        }
    }

    public void MarkCancelled(Guid previewId)
    {
        lock (_historyLock)
        {
            var preview = _history.FirstOrDefault(p => p.Id == previewId);
            if (preview != null)
            {
                preview.IsCancelled = true;
            }
        }
    }

    public void ClearHistory()
    {
        lock (_historyLock)
        {
            _history.Clear();
        }
    }

    public CommandPreviewResult? GetPreview(Guid previewId)
    {
        lock (_historyLock)
        {
            return _history.FirstOrDefault(p => p.Id == previewId);
        }
    }

    #region Private Methods

    private async Task<CommandPreviewResult> GenerateAIPreviewAsync(string input, CancellationToken cancellationToken)
    {
        try
        {
            var chatService = _kernel!.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
            history.AddSystemMessage(PreviewSystemPrompt);

            // Add context about current state
            if (_context.CurrentPart != null)
            {
                history.AddSystemMessage($"Current part: {_context.CurrentPart.Name}, Features: {_context.CurrentPart.Features.Count}");
            }

            history.AddUserMessage(input);

            var response = await chatService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
            var content = response.Content ?? "";

            return ParsePreviewResponse(content, input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI preview, falling back to basic");
            return GenerateBasicPreview(input);
        }
    }

    private CommandPreviewResult ParsePreviewResponse(string json, string originalInput)
    {
        try
        {
            // Extract JSON from response (may be wrapped in markdown)
            var jsonStart = json.IndexOf('{');
            var jsonEnd = json.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                json = json.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var preview = new CommandPreviewResult
            {
                OriginalInput = originalInput,
                Confidence = root.TryGetProperty("overallConfidence", out var conf) ? conf.GetDouble() : 0.8,
                RiskLevel = root.TryGetProperty("riskLevel", out var risk)
                    ? Enum.Parse<RiskLevel>(risk.GetString() ?? "Low", true)
                    : RiskLevel.Low,
                EstimatedDuration = TimeSpan.FromSeconds(
                    root.TryGetProperty("estimatedSeconds", out var est) ? est.GetDouble() : 1)
            };

            // Parse actions
            if (root.TryGetProperty("actions", out var actionsArray))
            {
                foreach (var actionEl in actionsArray.EnumerateArray())
                {
                    var action = new PreviewAction
                    {
                        Sequence = actionEl.TryGetProperty("sequence", out var seq) ? seq.GetInt32() : 1,
                        Type = actionEl.TryGetProperty("type", out var type)
                            ? Enum.Parse<ActionType>(type.GetString() ?? "Create", true)
                            : ActionType.Create,
                        Description = actionEl.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        TargetEntity = actionEl.TryGetProperty("targetEntity", out var target) ? target.GetString() : null,
                        SecondaryEntity = actionEl.TryGetProperty("secondaryEntity", out var sec) ? sec.GetString() : null,
                        CommandType = actionEl.TryGetProperty("commandType", out var cmd) ? cmd.GetString() ?? "" : "",
                        ApiDetails = actionEl.TryGetProperty("apiDetails", out var api) ? api.GetString() : null,
                        IsReversible = actionEl.TryGetProperty("isReversible", out var rev) && rev.GetBoolean(),
                        Confidence = actionEl.TryGetProperty("confidence", out var aconf) ? aconf.GetDouble() : 0.9
                    };

                    // Parse parameters
                    if (actionEl.TryGetProperty("parameters", out var paramsEl))
                    {
                        foreach (var param in paramsEl.EnumerateObject())
                        {
                            action.Parameters[param.Name] = param.Value.ToString();
                        }
                    }

                    preview.Actions.Add(action);
                }
            }

            // Parse warnings
            if (root.TryGetProperty("warnings", out var warningsArray))
            {
                foreach (var warnEl in warningsArray.EnumerateArray())
                {
                    preview.Warnings.Add(new PreviewWarning
                    {
                        Severity = warnEl.TryGetProperty("severity", out var sev)
                            ? Enum.Parse<WarningSeverity>(sev.GetString() ?? "Warning", true)
                            : WarningSeverity.Warning,
                        Message = warnEl.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "",
                        ActionSequence = warnEl.TryGetProperty("actionSequence", out var aseq) ? aseq.GetInt32() : null,
                        Resolution = warnEl.TryGetProperty("resolution", out var res) ? res.GetString() : null
                    });
                }
            }

            // Parse suggestions
            if (root.TryGetProperty("suggestions", out var suggestionsArray))
            {
                foreach (var suggEl in suggestionsArray.EnumerateArray())
                {
                    preview.Suggestions.Add(suggEl.GetString() ?? "");
                }
            }

            return preview;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI preview response, using basic preview");
            return GenerateBasicPreview(originalInput);
        }
    }

    private CommandPreviewResult GenerateBasicPreview(string input)
    {
        var lower = input.ToLowerInvariant();
        var actions = new List<PreviewAction>();
        var riskLevel = RiskLevel.Low;

        // Basic pattern matching for preview
        if (lower.Contains("create") || lower.Contains("make"))
        {
            if (lower.Contains("box") || lower.Contains("rectangular"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Create,
                    Description = "Create a rectangular box/part",
                    CommandType = "CreateBoxCommand",
                    IsReversible = true
                });
            }
            else if (lower.Contains("cylinder"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Create,
                    Description = "Create a cylindrical part",
                    CommandType = "CreateCylinderCommand",
                    IsReversible = true
                });
            }
            else if (lower.Contains("assembly"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Create,
                    Description = "Create a new assembly",
                    CommandType = "CreateAssemblyCommand",
                    IsReversible = true
                });
            }
            else if (lower.Contains("part"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Create,
                    Description = "Create a new part",
                    CommandType = "CreatePartCommand",
                    IsReversible = true
                });
            }
        }
        else if (lower.Contains("add"))
        {
            if (lower.Contains("fillet"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Modify,
                    Description = "Add fillet to edges",
                    CommandType = "AddFilletCommand",
                    IsReversible = true
                });
            }
            else if (lower.Contains("chamfer"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Modify,
                    Description = "Add chamfer to edges",
                    CommandType = "AddChamferCommand",
                    IsReversible = true
                });
            }
            else if (lower.Contains("hole"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Modify,
                    Description = "Add hole feature",
                    CommandType = "AddHoleCommand",
                    IsReversible = true
                });
            }
            else if (lower.Contains("mate"))
            {
                actions.Add(new PreviewAction
                {
                    Sequence = 1,
                    Type = ActionType.Mate,
                    Description = "Add assembly mate",
                    CommandType = "AddMateCommand",
                    IsReversible = true
                });
            }
        }
        else if (lower.Contains("delete") || lower.Contains("remove"))
        {
            riskLevel = RiskLevel.High;
            actions.Add(new PreviewAction
            {
                Sequence = 1,
                Type = ActionType.Delete,
                Description = "Delete element",
                CommandType = "DeleteCommand",
                IsReversible = false
            });
        }
        else if (lower.Contains("save"))
        {
            actions.Add(new PreviewAction
            {
                Sequence = 1,
                Type = ActionType.Save,
                Description = "Save document",
                CommandType = "SaveCommand",
                IsReversible = false
            });
        }
        else if (lower.Contains("export"))
        {
            actions.Add(new PreviewAction
            {
                Sequence = 1,
                Type = ActionType.Export,
                Description = "Export document",
                CommandType = "ExportCommand",
                IsReversible = false
            });
        }

        if (actions.Count == 0)
        {
            actions.Add(new PreviewAction
            {
                Sequence = 1,
                Type = ActionType.Query,
                Description = "Process command",
                CommandType = "Unknown",
                Confidence = 0.5
            });
        }

        return new CommandPreviewResult
        {
            OriginalInput = input,
            Actions = actions,
            Confidence = actions.Any(a => a.Confidence < 1) ? 0.7 : 0.9,
            RiskLevel = riskLevel
        };
    }

    private void AddToHistory(CommandPreviewResult preview)
    {
        lock (_historyLock)
        {
            _history.Insert(0, preview);
            while (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    private static string GetActionIcon(ActionType type) => type switch
    {
        ActionType.Create => "➕",
        ActionType.Modify => "✏️",
        ActionType.Delete => "🗑️",
        ActionType.Move => "↔️",
        ActionType.Mate => "🔗",
        ActionType.Export => "📤",
        ActionType.Save => "💾",
        ActionType.Query => "❓",
        ActionType.Undo => "↩️",
        ActionType.Redo => "↪️",
        _ => "●"
    };

    private static string GetActionColor(ActionType type) => type switch
    {
        ActionType.Create => "#4CAF50",  // Green
        ActionType.Modify => "#2196F3",  // Blue
        ActionType.Delete => "#F44336",  // Red
        ActionType.Move => "#9C27B0",    // Purple
        ActionType.Mate => "#FF9800",    // Orange
        ActionType.Export => "#00BCD4",  // Cyan
        ActionType.Save => "#8BC34A",    // Light Green
        ActionType.Query => "#607D8B",   // Blue Grey
        ActionType.Undo => "#795548",    // Brown
        ActionType.Redo => "#795548",    // Brown
        _ => "#FFFFFF"
    };

    private static RiskLevel DetermineRiskLevel(ISwaiCommand command) => command.CommandType switch
    {
        "DeleteCommand" or "DeleteFeatureCommand" => RiskLevel.High,
        "SavePart" or "SaveAssembly" or "ExportPart" => RiskLevel.Low,
        _ when !command.CanUndo => RiskLevel.Medium,
        _ => RiskLevel.Low
    };

    private static ActionType DetermineActionType(ISwaiCommand command) => command.CommandType switch
    {
        var t when t.StartsWith("Create") => ActionType.Create,
        var t when t.Contains("Delete") => ActionType.Delete,
        var t when t.Contains("Move") => ActionType.Move,
        var t when t.Contains("Mate") => ActionType.Mate,
        var t when t.Contains("Save") => ActionType.Save,
        var t when t.Contains("Export") => ActionType.Export,
        "UndoCommand" => ActionType.Undo,
        "RedoCommand" => ActionType.Redo,
        _ => ActionType.Modify
    };

    private static Dictionary<string, object> ExtractCommandParameters(ISwaiCommand command)
    {
        var parameters = new Dictionary<string, object>();

        // Use reflection to get public properties
        var properties = command.GetType().GetProperties()
            .Where(p => p.CanRead && p.Name != "CommandType" && p.Name != "Description" && p.Name != "CanUndo");

        foreach (var prop in properties)
        {
            var value = prop.GetValue(command);
            if (value != null)
            {
                parameters[prop.Name] = value;
            }
        }

        return parameters;
    }

    #endregion
}
