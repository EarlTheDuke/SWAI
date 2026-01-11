using SWAI.Core.Models;

namespace SWAI.Core.Commands;

/// <summary>
/// Result of executing a command
/// </summary>
public record CommandResult
{
    /// <summary>
    /// Whether the command succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Human-readable message about the result
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Error details if failed
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Any data returned by the command
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Time taken to execute
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Preview of SolidWorks API calls that would be made
    /// </summary>
    public ApiCallSequence? ApiPreview { get; init; }

    public static CommandResult Succeeded(string message, object? data = null, ApiCallSequence? apiPreview = null) => new()
    {
        Success = true,
        Message = message,
        Data = data,
        ApiPreview = apiPreview
    };

    public static CommandResult Failed(string message, string? error = null) => new()
    {
        Success = false,
        Message = message,
        Error = error
    };
}

/// <summary>
/// Base interface for all SWAI commands
/// </summary>
public interface ISwaiCommand
{
    /// <summary>
    /// Unique identifier for this command instance
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// The type of command
    /// </summary>
    string CommandType { get; }

    /// <summary>
    /// Human-readable description of what this command does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Whether this command can be undone
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Timestamp when this command was created
    /// </summary>
    DateTime CreatedAt { get; }
}

/// <summary>
/// Base class for SWAI commands
/// </summary>
public abstract class SwaiCommandBase : ISwaiCommand
{
    public Guid Id { get; } = Guid.NewGuid();
    public abstract string CommandType { get; }
    public abstract string Description { get; }
    public virtual bool CanUndo => true;
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
