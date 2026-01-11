using SWAI.Core.Interfaces;

namespace SWAI.Core.Models.Session;

/// <summary>
/// Serializable session data for persistence
/// </summary>
public class SessionData
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public Guid SessionId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Session name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project/workspace path this session is associated with
    /// </summary>
    public string? ProjectPath { get; set; }

    /// <summary>
    /// When the session was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the session was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Chat history
    /// </summary>
    public List<ChatMessage> ChatHistory { get; init; } = new();

    /// <summary>
    /// Design state snapshots (most recent last)
    /// </summary>
    public List<DesignState> StateSnapshots { get; init; } = new();

    /// <summary>
    /// Command execution history
    /// </summary>
    public List<CommandHistoryEntry> CommandHistory { get; init; } = new();

    /// <summary>
    /// Session metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Maximum number of state snapshots to keep
    /// </summary>
    public int MaxSnapshots { get; set; } = 50;

    /// <summary>
    /// Add a new state snapshot
    /// </summary>
    public void AddSnapshot(DesignState state)
    {
        StateSnapshots.Add(state);
        ModifiedAt = DateTime.UtcNow;

        // Trim old snapshots
        while (StateSnapshots.Count > MaxSnapshots)
        {
            StateSnapshots.RemoveAt(0);
        }
    }

    /// <summary>
    /// Get the most recent state
    /// </summary>
    public DesignState? GetLatestState() => StateSnapshots.LastOrDefault();

    /// <summary>
    /// Get summary for display
    /// </summary>
    public string Summary => $"{Name} ({ChatHistory.Count} messages, {CommandHistory.Count} commands)";
}

/// <summary>
/// Entry in command execution history
/// </summary>
public class CommandHistoryEntry
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the command was executed
    /// </summary>
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The original user input
    /// </summary>
    public string UserInput { get; init; } = string.Empty;

    /// <summary>
    /// Command type that was executed
    /// </summary>
    public string CommandType { get; init; } = string.Empty;

    /// <summary>
    /// Command description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Result message
    /// </summary>
    public string? ResultMessage { get; init; }

    /// <summary>
    /// Execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Whether this command can be undone
    /// </summary>
    public bool CanUndo { get; init; }

    /// <summary>
    /// Whether this command has been undone
    /// </summary>
    public bool IsUndone { get; set; }

    /// <summary>
    /// Parameters used (serialized)
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}
