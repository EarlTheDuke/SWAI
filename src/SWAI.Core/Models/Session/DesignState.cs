using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Features;

namespace SWAI.Core.Models.Session;

/// <summary>
/// Represents the current state of the design being worked on
/// </summary>
public class DesignState
{
    /// <summary>
    /// Unique identifier for this state
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when this state was captured
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Description of what triggered this snapshot
    /// </summary>
    public string Trigger { get; init; } = string.Empty;

    /// <summary>
    /// Active document type
    /// </summary>
    public DocumentType ActiveDocumentType { get; set; }

    /// <summary>
    /// Active document name
    /// </summary>
    public string? ActiveDocumentName { get; set; }

    /// <summary>
    /// Active document path
    /// </summary>
    public string? ActiveDocumentPath { get; set; }

    /// <summary>
    /// List of open parts
    /// </summary>
    public List<PartSummary> OpenParts { get; init; } = new();

    /// <summary>
    /// List of open assemblies
    /// </summary>
    public List<AssemblySummary> OpenAssemblies { get; init; } = new();

    /// <summary>
    /// Recently created features
    /// </summary>
    public List<FeatureSummary> RecentFeatures { get; init; } = new();

    /// <summary>
    /// Named references (e.g., "the top face", "the first hole")
    /// </summary>
    public Dictionary<string, EntityReference> NamedReferences { get; init; } = new();

    /// <summary>
    /// Custom properties that have been set
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; init; } = new();

    /// <summary>
    /// Current selection state
    /// </summary>
    public SelectionState? CurrentSelection { get; set; }

    /// <summary>
    /// Create a summary for prompt injection
    /// </summary>
    public string ToPromptSummary()
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(ActiveDocumentName))
        {
            parts.Add($"Active: {ActiveDocumentName} ({ActiveDocumentType})");
        }

        if (OpenParts.Count > 0)
        {
            parts.Add($"Parts: {string.Join(", ", OpenParts.Select(p => p.Name))}");
        }

        if (OpenAssemblies.Count > 0)
        {
            parts.Add($"Assemblies: {string.Join(", ", OpenAssemblies.Select(a => a.Name))}");
        }

        if (RecentFeatures.Count > 0)
        {
            var last3 = RecentFeatures.TakeLast(3);
            parts.Add($"Recent features: {string.Join(", ", last3.Select(f => f.Name))}");
        }

        if (NamedReferences.Count > 0)
        {
            parts.Add($"Named refs: {string.Join(", ", NamedReferences.Keys)}");
        }

        if (CurrentSelection != null && CurrentSelection.SelectedCount > 0)
        {
            parts.Add($"Selected: {CurrentSelection.Summary}");
        }

        return string.Join(" | ", parts);
    }
}

/// <summary>
/// Summary of a part document
/// </summary>
public class PartSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public int FeatureCount { get; init; }
    public int SketchCount { get; init; }
    public List<string> FeatureNames { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public bool IsDirty { get; init; }

    public static PartSummary FromDocument(PartDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        FilePath = doc.FilePath,
        FeatureCount = doc.Features.Count,
        SketchCount = doc.Sketches.Count,
        FeatureNames = doc.Features.Select(f => f.Name).ToList(),
        CreatedAt = doc.CreatedAt,
        ModifiedAt = doc.ModifiedAt,
        IsDirty = doc.IsDirty
    };
}

/// <summary>
/// Summary of an assembly document
/// </summary>
public class AssemblySummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public int ComponentCount { get; init; }
    public int MateCount { get; init; }
    public List<string> ComponentNames { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public bool IsDirty { get; init; }

    public static AssemblySummary FromDocument(AssemblyDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        FilePath = doc.FilePath,
        ComponentCount = doc.Components.Count,
        MateCount = doc.Mates.Count,
        ComponentNames = doc.Components.Select(c => c.InstanceName).ToList(),
        CreatedAt = doc.CreatedAt,
        ModifiedAt = doc.ModifiedAt,
        IsDirty = doc.IsDirty
    };
}

/// <summary>
/// Summary of a feature
/// </summary>
public class FeatureSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> Parameters { get; init; } = new();

    public static FeatureSummary FromFeature(Feature feature) => new()
    {
        Id = feature.Id,
        Name = feature.Name,
        Type = feature.FeatureType,
        CreatedAt = DateTime.UtcNow // Feature doesn't track creation time
    };
}

/// <summary>
/// Reference to a named entity
/// </summary>
public class EntityReference
{
    public string EntityType { get; init; } = string.Empty; // Face, Edge, Feature, etc.
    public string EntityName { get; init; } = string.Empty;
    public string DocumentName { get; init; } = string.Empty;
    public string? ComponentName { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Current selection state
/// </summary>
public class SelectionState
{
    public int SelectedCount { get; init; }
    public List<SelectedEntity> Entities { get; init; } = new();

    public string Summary => SelectedCount == 0 
        ? "Nothing selected" 
        : $"{SelectedCount} entities: {string.Join(", ", Entities.Take(3).Select(e => e.Type))}";
}

/// <summary>
/// A selected entity
/// </summary>
public class SelectedEntity
{
    public string Type { get; init; } = string.Empty; // Face, Edge, Vertex, Feature
    public string Name { get; init; } = string.Empty;
    public int SelectionMark { get; init; }
}
