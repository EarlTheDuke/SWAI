namespace SWAI.Core.Models.Preview;

/// <summary>
/// Result of command preview generation
/// </summary>
public class CommandPreviewResult
{
    /// <summary>
    /// Unique identifier for this preview
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Original natural language input
    /// </summary>
    public string OriginalInput { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when preview was generated
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// List of planned actions
    /// </summary>
    public List<PreviewAction> Actions { get; init; } = new();

    /// <summary>
    /// Overall confidence score (0-1)
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Risk level assessment
    /// </summary>
    public RiskLevel RiskLevel { get; init; } = RiskLevel.Low;

    /// <summary>
    /// Warnings or potential issues
    /// </summary>
    public List<PreviewWarning> Warnings { get; init; } = new();

    /// <summary>
    /// Suggestions for improvement
    /// </summary>
    public List<string> Suggestions { get; init; } = new();

    /// <summary>
    /// Whether this preview can be auto-executed (low risk, high confidence)
    /// </summary>
    public bool CanAutoExecute => RiskLevel == RiskLevel.Low && Confidence >= 0.9 && Warnings.Count == 0;

    /// <summary>
    /// Summary text for compact display
    /// </summary>
    public string Summary => Actions.Count switch
    {
        0 => "No actions planned",
        1 => Actions[0].Description,
        _ => $"{Actions.Count} actions: {Actions[0].Description} and {Actions.Count - 1} more"
    };

    /// <summary>
    /// Estimated execution time
    /// </summary>
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether this preview has been executed
    /// </summary>
    public bool IsExecuted { get; set; }

    /// <summary>
    /// Whether this preview was cancelled
    /// </summary>
    public bool IsCancelled { get; set; }
}

/// <summary>
/// A single planned action in the preview
/// </summary>
public class PreviewAction
{
    /// <summary>
    /// Action sequence number
    /// </summary>
    public int Sequence { get; init; }

    /// <summary>
    /// Type of action (Create, Modify, Delete, etc.)
    /// </summary>
    public ActionType Type { get; init; }

    /// <summary>
    /// Human-readable description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Target entity (part name, feature name, etc.)
    /// </summary>
    public string? TargetEntity { get; init; }

    /// <summary>
    /// Secondary entity for mates, etc.
    /// </summary>
    public string? SecondaryEntity { get; init; }

    /// <summary>
    /// Parameters for this action
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();

    /// <summary>
    /// The underlying command that will execute this action
    /// </summary>
    public string CommandType { get; init; } = string.Empty;

    /// <summary>
    /// API call details (for verbose mode)
    /// </summary>
    public string? ApiDetails { get; init; }

    /// <summary>
    /// Whether this action can be undone
    /// </summary>
    public bool IsReversible { get; init; } = true;

    /// <summary>
    /// Individual confidence for this action
    /// </summary>
    public double Confidence { get; init; } = 1.0;
}

/// <summary>
/// Warning about potential issues
/// </summary>
public class PreviewWarning
{
    /// <summary>
    /// Warning severity
    /// </summary>
    public WarningSeverity Severity { get; init; }

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Which action this warning relates to (null = overall)
    /// </summary>
    public int? ActionSequence { get; init; }

    /// <summary>
    /// Suggested resolution
    /// </summary>
    public string? Resolution { get; init; }
}

/// <summary>
/// Types of preview actions
/// </summary>
public enum ActionType
{
    Create,
    Modify,
    Delete,
    Move,
    Mate,
    Export,
    Save,
    Query,
    Undo,
    Redo
}

/// <summary>
/// Risk levels for preview
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Safe to execute, easily reversible
    /// </summary>
    Low,

    /// <summary>
    /// Some caution needed, may have side effects
    /// </summary>
    Medium,

    /// <summary>
    /// Potentially destructive, requires confirmation
    /// </summary>
    High,

    /// <summary>
    /// Critical operation, double confirmation recommended
    /// </summary>
    Critical
}

/// <summary>
/// Warning severity levels
/// </summary>
public enum WarningSeverity
{
    Info,
    Warning,
    Error
}
