using SWAI.Core.Commands;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Features;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Services;

/// <summary>
/// Maintains context for conversational interactions
/// </summary>
public class ConversationContext
{
    /// <summary>
    /// The current active part
    /// </summary>
    public PartDocument? CurrentPart { get; set; }

    /// <summary>
    /// The last created feature
    /// </summary>
    public Feature? LastFeature { get; set; }

    /// <summary>
    /// The last executed command
    /// </summary>
    public ISwaiCommand? LastCommand { get; set; }

    /// <summary>
    /// Stack of dimensions mentioned in conversation
    /// </summary>
    public Stack<Dimension> RecentDimensions { get; } = new();

    /// <summary>
    /// Named references in current session (e.g., "the first hole")
    /// </summary>
    public Dictionary<string, object> NamedReferences { get; } = new();

    /// <summary>
    /// Default unit system for this conversation
    /// </summary>
    public UnitSystem DefaultUnits { get; set; } = UnitSystem.Inches;

    /// <summary>
    /// Last used plane
    /// </summary>
    public Models.Geometry.ReferencePlane LastPlane { get; set; } = Models.Geometry.ReferencePlane.Top;

    /// <summary>
    /// Pending clarification context
    /// </summary>
    public ClarificationContext? PendingClarification { get; set; }

    /// <summary>
    /// Track what the user might be referring to with "it", "that", etc.
    /// </summary>
    public object? ImplicitReference { get; set; }

    /// <summary>
    /// Add a dimension to recent history
    /// </summary>
    public void PushDimension(Dimension dim)
    {
        RecentDimensions.Push(dim);
        
        // Keep only last 10 dimensions
        while (RecentDimensions.Count > 10)
        {
            var temp = new Stack<Dimension>();
            for (int i = 0; i < 10; i++)
            {
                temp.Push(RecentDimensions.Pop());
            }
            RecentDimensions.Clear();
            while (temp.Count > 0)
            {
                RecentDimensions.Push(temp.Pop());
            }
        }
    }

    /// <summary>
    /// Get the last dimension of a specific type context
    /// </summary>
    public Dimension? GetLastDimensionLike(string context)
    {
        var lower = context.ToLowerInvariant();
        
        // Return most recent dimension if context is vague
        if (RecentDimensions.Count > 0)
        {
            return RecentDimensions.Peek();
        }

        return null;
    }

    /// <summary>
    /// Set a named reference
    /// </summary>
    public void SetReference(string name, object reference)
    {
        NamedReferences[name.ToLowerInvariant()] = reference;
        ImplicitReference = reference;
    }

    /// <summary>
    /// Get a named reference
    /// </summary>
    public T? GetReference<T>(string name) where T : class
    {
        if (NamedReferences.TryGetValue(name.ToLowerInvariant(), out var value))
        {
            return value as T;
        }
        return null;
    }

    /// <summary>
    /// Clear the context for a new session
    /// </summary>
    public void Clear()
    {
        CurrentPart = null;
        LastFeature = null;
        LastCommand = null;
        RecentDimensions.Clear();
        NamedReferences.Clear();
        PendingClarification = null;
        ImplicitReference = null;
    }

    /// <summary>
    /// Update context after a command is executed
    /// </summary>
    public void OnCommandExecuted(ISwaiCommand command, object? result)
    {
        LastCommand = command;

        if (result is Feature feature)
        {
            LastFeature = feature;
            ImplicitReference = feature;
        }

        if (result is PartDocument part)
        {
            CurrentPart = part;
            ImplicitReference = part;
        }

        // Extract dimensions from command and push to history
        ExtractDimensionsFromCommand(command);
    }

    private void ExtractDimensionsFromCommand(ISwaiCommand command)
    {
        switch (command)
        {
            case CreateBoxCommand box:
                PushDimension(box.Width);
                PushDimension(box.Length);
                PushDimension(box.Height);
                break;

            case CreateCylinderCommand cyl:
                PushDimension(cyl.Diameter);
                PushDimension(cyl.Height);
                break;

            case AddFilletCommand fillet:
                PushDimension(fillet.Radius);
                break;

            case AddChamferCommand chamfer:
                PushDimension(chamfer.Distance);
                break;

            case AddHoleCommand hole:
                PushDimension(hole.Diameter);
                if (hole.Depth != null)
                    PushDimension(hole.Depth.Value);
                break;

            case AddExtrusionCommand ext:
                PushDimension(ext.Depth);
                break;
        }
    }
}

/// <summary>
/// Context for pending clarification
/// </summary>
public class ClarificationContext
{
    /// <summary>
    /// The original user input that needed clarification
    /// </summary>
    public string OriginalInput { get; set; } = string.Empty;

    /// <summary>
    /// The partial command being built
    /// </summary>
    public ISwaiCommand? PartialCommand { get; set; }

    /// <summary>
    /// What we're waiting for
    /// </summary>
    public string WaitingFor { get; set; } = string.Empty;

    /// <summary>
    /// Expected type of response
    /// </summary>
    public ClarificationResponseType ExpectedType { get; set; }

    /// <summary>
    /// Timestamp when clarification was requested
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

public enum ClarificationResponseType
{
    Dimension,
    Confirmation,
    Selection,
    Number,
    Text
}
