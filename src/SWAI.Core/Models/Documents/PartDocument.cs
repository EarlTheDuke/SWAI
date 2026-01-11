using SWAI.Core.Models.Features;
using SWAI.Core.Models.Sketch;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Models.Documents;

/// <summary>
/// Document types in SolidWorks
/// </summary>
public enum DocumentType
{
    Part,
    Assembly,
    Drawing
}

/// <summary>
/// Export formats supported
/// </summary>
public enum ExportFormat
{
    SolidWorksPart,   // .sldprt
    STEP,             // .step / .stp
    IGES,             // .iges / .igs
    STL,              // .stl
    Parasolid,        // .x_t
    DXF,              // .dxf
    DWG,              // .dwg
    PDF               // .pdf (for drawings)
}

/// <summary>
/// Represents a SolidWorks part document
/// </summary>
public class PartDocument
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Document name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Full file path (if saved)
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Whether this document has been modified since last save
    /// </summary>
    public bool IsDirty { get; set; } = true;

    /// <summary>
    /// The unit system for this document
    /// </summary>
    public UnitSystem Units { get; set; } = UnitSystem.Inches;

    /// <summary>
    /// Sketches in this document
    /// </summary>
    public List<SketchProfile> Sketches { get; } = new();

    /// <summary>
    /// Features in this document (in order)
    /// </summary>
    public List<Feature> Features { get; } = new();

    /// <summary>
    /// Custom properties
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; } = new();

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public PartDocument(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Add a sketch to the document
    /// </summary>
    public PartDocument AddSketch(SketchProfile sketch)
    {
        Sketches.Add(sketch);
        MarkModified();
        return this;
    }

    /// <summary>
    /// Add a feature to the document
    /// </summary>
    public PartDocument AddFeature(Feature feature)
    {
        Features.Add(feature);
        MarkModified();
        return this;
    }

    /// <summary>
    /// Set a custom property
    /// </summary>
    public PartDocument SetProperty(string name, string value)
    {
        CustomProperties[name] = value;
        MarkModified();
        return this;
    }

    /// <summary>
    /// Mark the document as modified
    /// </summary>
    public void MarkModified()
    {
        IsDirty = true;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark as saved
    /// </summary>
    public void MarkSaved(string filePath)
    {
        FilePath = filePath;
        IsDirty = false;
    }

    /// <summary>
    /// Get the file extension for an export format
    /// </summary>
    public static string GetExtension(ExportFormat format) => format switch
    {
        ExportFormat.SolidWorksPart => ".sldprt",
        ExportFormat.STEP => ".step",
        ExportFormat.IGES => ".igs",
        ExportFormat.STL => ".stl",
        ExportFormat.Parasolid => ".x_t",
        ExportFormat.DXF => ".dxf",
        ExportFormat.DWG => ".dwg",
        ExportFormat.PDF => ".pdf",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    public override string ToString() => $"Part: {Name} ({Features.Count} features)";
}
