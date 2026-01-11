using SWAI.Core.Commands;

namespace SWAI.Core.Interfaces;

/// <summary>
/// The type of user intent detected
/// </summary>
public enum IntentType
{
    Unknown,
    CreatePart,
    CreateBox,
    CreateCylinder,
    CreateSketch,
    AddExtrusion,
    AddCut,
    AddFillet,
    AddChamfer,
    AddHole,
    SavePart,
    ExportPart,
    ClosePart,
    Help,
    Undo,
    Redo,
    ShowStatus,
    ListFeatures
}

/// <summary>
/// Result of intent detection
/// </summary>
public class IntentResult
{
    /// <summary>
    /// The detected intent type
    /// </summary>
    public IntentType Intent { get; init; }

    /// <summary>
    /// Confidence score (0-1)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Extracted parameters as key-value pairs
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// The original user input
    /// </summary>
    public string OriginalInput { get; init; } = string.Empty;

    /// <summary>
    /// Any clarification needed from user
    /// </summary>
    public string? ClarificationNeeded { get; init; }
}

/// <summary>
/// Chat message in conversation history
/// </summary>
public class ChatMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Role { get; init; } = "user";  // "user", "assistant", "system"
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public ISwaiCommand? AssociatedCommand { get; init; }
    public bool IsError { get; init; } = false;
}

/// <summary>
/// AI response from processing user input
/// </summary>
public class AIResponse
{
    /// <summary>
    /// The response message to show user
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Commands to execute (if any)
    /// </summary>
    public List<ISwaiCommand> Commands { get; init; } = new();

    /// <summary>
    /// Whether the AI needs clarification
    /// </summary>
    public bool NeedsClarification { get; init; }

    /// <summary>
    /// Suggestions for the user
    /// </summary>
    public List<string> Suggestions { get; init; } = new();

    /// <summary>
    /// Whether this was a successful parse
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if parsing failed
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether offline/fallback parsing was used instead of AI
    /// </summary>
    public bool UsedOfflineFallback { get; init; }

    /// <summary>
    /// Details about why fallback was used (for debugging)
    /// </summary>
    public string? FallbackReason { get; init; }
}

/// <summary>
/// Interface for AI/LLM service
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Whether the AI service is properly configured
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Process user input and return a response with commands
    /// </summary>
    Task<AIResponse> ProcessInputAsync(string userInput, IReadOnlyList<ChatMessage>? history = null);

    /// <summary>
    /// Detect the intent from user input
    /// </summary>
    Task<IntentResult> DetectIntentAsync(string userInput);

    /// <summary>
    /// Generate a description of what commands will do
    /// </summary>
    Task<string> DescribeCommandsAsync(IReadOnlyList<ISwaiCommand> commands);

    /// <summary>
    /// Get suggestions based on current context
    /// </summary>
    Task<List<string>> GetSuggestionsAsync(string partialInput, IReadOnlyList<ChatMessage>? history = null);
}

/// <summary>
/// Interface for conversation session management
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Current session ID
    /// </summary>
    Guid SessionId { get; }

    /// <summary>
    /// Chat history for current session
    /// </summary>
    IReadOnlyList<ChatMessage> History { get; }

    /// <summary>
    /// Add a message to history
    /// </summary>
    void AddMessage(ChatMessage message);

    /// <summary>
    /// Clear history
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Start a new session
    /// </summary>
    void NewSession();

    /// <summary>
    /// Export session history
    /// </summary>
    string ExportHistory();
}
