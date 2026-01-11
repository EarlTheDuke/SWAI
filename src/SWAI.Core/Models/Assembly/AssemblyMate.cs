using SWAI.Core.Models.Units;

namespace SWAI.Core.Models.Assembly;

/// <summary>
/// Types of assembly mates
/// </summary>
public enum MateType
{
    /// <summary>
    /// Coincident - faces/planes touch
    /// </summary>
    Coincident,

    /// <summary>
    /// Concentric - circular edges/faces share center axis
    /// </summary>
    Concentric,

    /// <summary>
    /// Distance - maintain specific distance between entities
    /// </summary>
    Distance,

    /// <summary>
    /// Angle - maintain specific angle between entities
    /// </summary>
    Angle,

    /// <summary>
    /// Parallel - faces/planes are parallel
    /// </summary>
    Parallel,

    /// <summary>
    /// Perpendicular - faces/planes are at 90 degrees
    /// </summary>
    Perpendicular,

    /// <summary>
    /// Tangent - surfaces touch tangentially
    /// </summary>
    Tangent,

    /// <summary>
    /// Lock - lock all degrees of freedom
    /// </summary>
    Lock,

    /// <summary>
    /// Width - center between two faces
    /// </summary>
    Width,

    /// <summary>
    /// Symmetric - symmetric about a plane
    /// </summary>
    Symmetric,

    /// <summary>
    /// Cam - cam follower relationship
    /// </summary>
    Cam,

    /// <summary>
    /// Gear - gear ratio relationship
    /// </summary>
    Gear,

    /// <summary>
    /// Rack and Pinion
    /// </summary>
    RackPinion,

    /// <summary>
    /// Screw - helical motion
    /// </summary>
    Screw,

    /// <summary>
    /// Path - follow a path
    /// </summary>
    Path
}

/// <summary>
/// Mate alignment
/// </summary>
public enum MateAlignment
{
    /// <summary>
    /// Aligned - normals point same direction
    /// </summary>
    Aligned,

    /// <summary>
    /// Anti-aligned - normals point opposite directions
    /// </summary>
    AntiAligned,

    /// <summary>
    /// Closest - system chooses based on geometry
    /// </summary>
    Closest
}

/// <summary>
/// Reference to a selectable entity for mating
/// </summary>
public class MateReference
{
    /// <summary>
    /// Component instance name
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Entity type (Face, Edge, Plane, Axis, Point, etc.)
    /// </summary>
    public string EntityType { get; set; } = "Face";

    /// <summary>
    /// Entity identifier or name
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Selection mark for the entity
    /// </summary>
    public int SelectionMark { get; set; }

    /// <summary>
    /// Create a face reference
    /// </summary>
    public static MateReference Face(string componentName, string faceName) => new()
    {
        ComponentName = componentName,
        EntityType = "Face",
        EntityName = faceName
    };

    /// <summary>
    /// Create an edge reference
    /// </summary>
    public static MateReference Edge(string componentName, string edgeName) => new()
    {
        ComponentName = componentName,
        EntityType = "Edge",
        EntityName = edgeName
    };

    /// <summary>
    /// Create a plane reference
    /// </summary>
    public static MateReference Plane(string componentName, string planeName) => new()
    {
        ComponentName = componentName,
        EntityType = "Plane",
        EntityName = planeName
    };

    /// <summary>
    /// Create an axis reference
    /// </summary>
    public static MateReference Axis(string componentName, string axisName) => new()
    {
        ComponentName = componentName,
        EntityType = "Axis",
        EntityName = axisName
    };

    /// <summary>
    /// Create an origin reference
    /// </summary>
    public static MateReference Origin(string componentName) => new()
    {
        ComponentName = componentName,
        EntityType = "Origin",
        EntityName = "Origin"
    };

    public override string ToString() => $"{ComponentName}@{EntityType}:{EntityName}";
}

/// <summary>
/// Represents a mate constraint in an assembly
/// </summary>
public class AssemblyMate
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Mate name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of mate
    /// </summary>
    public MateType Type { get; set; }

    /// <summary>
    /// First entity reference
    /// </summary>
    public MateReference Entity1 { get; set; } = new();

    /// <summary>
    /// Second entity reference
    /// </summary>
    public MateReference Entity2 { get; set; } = new();

    /// <summary>
    /// Alignment (for applicable mate types)
    /// </summary>
    public MateAlignment Alignment { get; set; } = MateAlignment.Closest;

    /// <summary>
    /// Distance value (for Distance mate)
    /// </summary>
    public Dimension? Distance { get; set; }

    /// <summary>
    /// Angle value in degrees (for Angle mate)
    /// </summary>
    public double? Angle { get; set; }

    /// <summary>
    /// Whether this mate is suppressed
    /// </summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// Whether the mate flips the direction
    /// </summary>
    public bool FlipDirection { get; set; }

    /// <summary>
    /// Gear/screw ratio (for mechanical mates)
    /// </summary>
    public double? Ratio { get; set; }

    public AssemblyMate(string name, MateType type)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Create a coincident mate
    /// </summary>
    public static AssemblyMate Coincident(string name, MateReference entity1, MateReference entity2)
    {
        return new AssemblyMate(name, MateType.Coincident)
        {
            Entity1 = entity1,
            Entity2 = entity2
        };
    }

    /// <summary>
    /// Create a concentric mate
    /// </summary>
    public static AssemblyMate Concentric(string name, MateReference entity1, MateReference entity2)
    {
        return new AssemblyMate(name, MateType.Concentric)
        {
            Entity1 = entity1,
            Entity2 = entity2
        };
    }

    /// <summary>
    /// Create a distance mate
    /// </summary>
    public static AssemblyMate DistanceMate(string name, MateReference entity1, MateReference entity2, Dimension distance)
    {
        return new AssemblyMate(name, MateType.Distance)
        {
            Entity1 = entity1,
            Entity2 = entity2,
            Distance = distance
        };
    }

    /// <summary>
    /// Create an angle mate
    /// </summary>
    public static AssemblyMate AngleMate(string name, MateReference entity1, MateReference entity2, double angleDegrees)
    {
        return new AssemblyMate(name, MateType.Angle)
        {
            Entity1 = entity1,
            Entity2 = entity2,
            Angle = angleDegrees
        };
    }

    /// <summary>
    /// Create a parallel mate
    /// </summary>
    public static AssemblyMate Parallel(string name, MateReference entity1, MateReference entity2)
    {
        return new AssemblyMate(name, MateType.Parallel)
        {
            Entity1 = entity1,
            Entity2 = entity2
        };
    }

    public override string ToString() => $"{Name}: {Type} ({Entity1} - {Entity2})";
}
