using SWAI.Core.Commands;
using SWAI.Core.Models.Preview;

namespace SWAI.Core.Interfaces;

/// <summary>
/// Preview display modes
/// </summary>
public enum PreviewMode
{
    /// <summary>
    /// One-line summary
    /// </summary>
    Compact,

    /// <summary>
    /// Full action list with parameters
    /// </summary>
    Detailed,

    /// <summary>
    /// Full details including API calls and prompts
    /// </summary>
    Verbose
}

/// <summary>
/// User's decision on a preview
/// </summary>
public enum PreviewDecision
{
    Execute,
    Edit,
    Cancel
}

/// <summary>
/// Service for generating and managing command previews
/// </summary>
public interface ICommandPreviewService
{
    /// <summary>
    /// Current preview mode
    /// </summary>
    PreviewMode CurrentMode { get; set; }

    /// <summary>
    /// Whether to auto-execute low-risk commands
    /// </summary>
    bool AutoExecuteLowRisk { get; set; }

    /// <summary>
    /// Whether to always show detailed preview
    /// </summary>
    bool AlwaysShowDetailed { get; set; }

    /// <summary>
    /// History of recent previews
    /// </summary>
    IReadOnlyList<CommandPreviewResult> PreviewHistory { get; }

    /// <summary>
    /// Maximum number of previews to keep in history
    /// </summary>
    int MaxHistoryCount { get; set; }

    /// <summary>
    /// Generate a preview for the given input
    /// </summary>
    Task<CommandPreviewResult> GeneratePreviewAsync(string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a preview from an already-parsed command
    /// </summary>
    Task<CommandPreviewResult> GeneratePreviewFromCommandAsync(ISwaiCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Format preview for display
    /// </summary>
    string FormatPreview(CommandPreviewResult preview, PreviewMode? modeOverride = null);

    /// <summary>
    /// Get formatted action list for UI binding
    /// </summary>
    IEnumerable<FormattedPreviewAction> GetFormattedActions(CommandPreviewResult preview);

    /// <summary>
    /// Mark a preview as executed
    /// </summary>
    void MarkExecuted(Guid previewId);

    /// <summary>
    /// Mark a preview as cancelled
    /// </summary>
    void MarkCancelled(Guid previewId);

    /// <summary>
    /// Clear preview history
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Get a preview by ID
    /// </summary>
    CommandPreviewResult? GetPreview(Guid previewId);

    /// <summary>
    /// Event raised when a new preview is generated
    /// </summary>
    event EventHandler<CommandPreviewResult>? PreviewGenerated;
}

/// <summary>
/// Formatted action for UI display
/// </summary>
public class FormattedPreviewAction
{
    /// <summary>
    /// Sequence number
    /// </summary>
    public int Sequence { get; init; }

    /// <summary>
    /// Icon/symbol for the action type
    /// </summary>
    public string Icon { get; init; } = "●";

    /// <summary>
    /// Action type display name
    /// </summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    /// Formatted description with highlights
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Target entity (highlighted)
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Parameter summary
    /// </summary>
    public string? Parameters { get; init; }

    /// <summary>
    /// Color code for the action type
    /// </summary>
    public string ColorCode { get; init; } = "#FFFFFF";

    /// <summary>
    /// Confidence display (e.g., "95%")
    /// </summary>
    public string ConfidenceDisplay { get; init; } = "100%";

    /// <summary>
    /// Whether this action has warnings
    /// </summary>
    public bool HasWarnings { get; init; }

    /// <summary>
    /// API details (for verbose mode)
    /// </summary>
    public string? ApiDetails { get; init; }
}
