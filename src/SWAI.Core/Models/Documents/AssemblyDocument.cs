using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Models.Documents;

/// <summary>
/// Represents a SolidWorks assembly document
/// </summary>
public class AssemblyDocument
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
    /// Components in this assembly
    /// </summary>
    public List<AssemblyComponent> Components { get; } = new();

    /// <summary>
    /// Mates in this assembly
    /// </summary>
    public List<AssemblyMate> Mates { get; } = new();

    /// <summary>
    /// Sub-assemblies
    /// </summary>
    public List<AssemblyDocument> SubAssemblies { get; } = new();

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

    public AssemblyDocument(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Add a component to the assembly
    /// </summary>
    public AssemblyDocument AddComponent(AssemblyComponent component)
    {
        Components.Add(component);
        MarkModified();
        return this;
    }

    /// <summary>
    /// Add a mate to the assembly
    /// </summary>
    public AssemblyDocument AddMate(AssemblyMate mate)
    {
        Mates.Add(mate);
        MarkModified();
        return this;
    }

    /// <summary>
    /// Find a component by name
    /// </summary>
    public AssemblyComponent? FindComponent(string name)
    {
        return Components.FirstOrDefault(c => 
            c.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            c.InstanceName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all components of a specific part
    /// </summary>
    public IEnumerable<AssemblyComponent> GetComponentsByPart(string partName)
    {
        return Components.Where(c => 
            c.PartPath.Contains(partName, StringComparison.OrdinalIgnoreCase));
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

    public override string ToString() => $"Assembly: {Name} ({Components.Count} components, {Mates.Count} mates)";
}

/// <summary>
/// Represents a component instance in an assembly
/// </summary>
public class AssemblyComponent
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Component name (from part file)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Instance name in assembly (e.g., "Part1-1", "Part1-2")
    /// </summary>
    public string InstanceName { get; set; }

    /// <summary>
    /// Path to the part/assembly file
    /// </summary>
    public string PartPath { get; set; }

    /// <summary>
    /// Whether this is a sub-assembly
    /// </summary>
    public bool IsSubAssembly { get; set; }

    /// <summary>
    /// Whether this component is suppressed
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Whether this component is fixed (grounded)
    /// </summary>
    public bool IsFixed { get; set; }

    /// <summary>
    /// Transform matrix for position/orientation
    /// </summary>
    public ComponentTransform Transform { get; set; } = new();

    /// <summary>
    /// Configuration name being used
    /// </summary>
    public string? ConfigurationName { get; set; }

    /// <summary>
    /// Instance number (for multiple instances of same part)
    /// </summary>
    public int InstanceNumber { get; set; } = 1;

    public AssemblyComponent(string name, string partPath)
    {
        Name = name;
        PartPath = partPath;
        InstanceName = $"{name}-1";
    }

    public override string ToString() => InstanceName;
}

/// <summary>
/// Transform for component position and orientation
/// </summary>
public class ComponentTransform
{
    /// <summary>
    /// X position in meters
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y position in meters
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Z position in meters
    /// </summary>
    public double Z { get; set; }

    /// <summary>
    /// Rotation about X axis (radians)
    /// </summary>
    public double RotationX { get; set; }

    /// <summary>
    /// Rotation about Y axis (radians)
    /// </summary>
    public double RotationY { get; set; }

    /// <summary>
    /// Rotation about Z axis (radians)
    /// </summary>
    public double RotationZ { get; set; }

    /// <summary>
    /// Create identity transform (origin, no rotation)
    /// </summary>
    public static ComponentTransform Identity => new();

    /// <summary>
    /// Create transform at position
    /// </summary>
    public static ComponentTransform AtPosition(double x, double y, double z) => new()
    {
        X = x,
        Y = y,
        Z = z
    };

    /// <summary>
    /// Get as 4x4 transformation matrix array (row-major)
    /// </summary>
    public double[] ToMatrix()
    {
        // Simplified - just translation for now
        return new double[]
        {
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            X, Y, Z, 1
        };
    }
}
