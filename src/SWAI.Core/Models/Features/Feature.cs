using SWAI.Core.Models.Sketch;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Models.Features;

/// <summary>
/// Base class for all SolidWorks features
/// </summary>
public abstract class Feature
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of this feature
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether this feature suppresses previous features
    /// </summary>
    public bool IsSuppressed { get; set; }

    protected Feature(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Get the SolidWorks feature type name
    /// </summary>
    public abstract string FeatureType { get; }
}

/// <summary>
/// Extrusion direction
/// </summary>
public enum ExtrusionDirection
{
    /// <summary>
    /// Extrude in one direction from sketch plane
    /// </summary>
    SingleDirection,

    /// <summary>
    /// Extrude equally in both directions (mid-plane)
    /// </summary>
    MidPlane,

    /// <summary>
    /// Extrude in both directions with different depths
    /// </summary>
    BothDirections
}

/// <summary>
/// End condition for extrusion
/// </summary>
public enum ExtrusionEndCondition
{
    Blind,
    ThroughAll,
    UpToSurface,
    UpToVertex,
    UpToBody
}

/// <summary>
/// Extrusion feature (Boss-Extrude)
/// </summary>
public class ExtrusionFeature : Feature
{
    /// <summary>
    /// The sketch profile to extrude
    /// </summary>
    public SketchProfile Profile { get; }

    /// <summary>
    /// Depth of extrusion (primary direction)
    /// </summary>
    public Dimension Depth { get; }

    /// <summary>
    /// Depth in second direction (for BothDirections)
    /// </summary>
    public Dimension? Depth2 { get; set; }

    /// <summary>
    /// Extrusion direction mode
    /// </summary>
    public ExtrusionDirection Direction { get; set; } = ExtrusionDirection.SingleDirection;

    /// <summary>
    /// End condition
    /// </summary>
    public ExtrusionEndCondition EndCondition { get; set; } = ExtrusionEndCondition.Blind;

    /// <summary>
    /// Draft angle in degrees (0 for no draft)
    /// </summary>
    public double DraftAngle { get; set; } = 0;

    /// <summary>
    /// Whether draft is outward
    /// </summary>
    public bool DraftOutward { get; set; } = false;

    public ExtrusionFeature(string name, SketchProfile profile, Dimension depth) : base(name)
    {
        Profile = profile;
        Depth = depth;
    }

    public override string FeatureType => "Boss-Extrude";

    public override string ToString() => $"Extrude '{Name}': {Depth} deep";
}

/// <summary>
/// Cut-Extrude feature
/// </summary>
public class CutExtrusionFeature : Feature
{
    public SketchProfile Profile { get; }
    public Dimension Depth { get; }
    public ExtrusionDirection Direction { get; set; } = ExtrusionDirection.SingleDirection;
    public ExtrusionEndCondition EndCondition { get; set; } = ExtrusionEndCondition.Blind;

    public CutExtrusionFeature(string name, SketchProfile profile, Dimension depth) : base(name)
    {
        Profile = profile;
        Depth = depth;
    }

    public override string FeatureType => "Cut-Extrude";

    public override string ToString() => $"Cut '{Name}': {Depth} deep";
}

/// <summary>
/// Fillet feature
/// </summary>
public class FilletFeature : Feature
{
    /// <summary>
    /// Fillet radius
    /// </summary>
    public Dimension Radius { get; }

    /// <summary>
    /// Whether to apply to all edges
    /// </summary>
    public bool ApplyToAllEdges { get; set; } = false;

    /// <summary>
    /// Specific edge indices to fillet (if not all)
    /// </summary>
    public List<int> EdgeIndices { get; } = new();

    public FilletFeature(string name, Dimension radius) : base(name)
    {
        Radius = radius;
    }

    public override string FeatureType => "Fillet";

    public override string ToString() => $"Fillet '{Name}': R={Radius}";
}

/// <summary>
/// Chamfer feature
/// </summary>
public class ChamferFeature : Feature
{
    /// <summary>
    /// Chamfer distance
    /// </summary>
    public Dimension Distance { get; }

    /// <summary>
    /// Second distance for asymmetric chamfer
    /// </summary>
    public Dimension? Distance2 { get; set; }

    /// <summary>
    /// Chamfer angle (alternative to Distance2)
    /// </summary>
    public double? Angle { get; set; }

    public ChamferFeature(string name, Dimension distance) : base(name)
    {
        Distance = distance;
    }

    public override string FeatureType => "Chamfer";

    public override string ToString() => $"Chamfer '{Name}': {Distance}";
}

/// <summary>
/// Hole feature (simple hole)
/// </summary>
public class HoleFeature : Feature
{
    /// <summary>
    /// Diameter of the hole
    /// </summary>
    public Dimension Diameter { get; }

    /// <summary>
    /// Depth of the hole
    /// </summary>
    public Dimension Depth { get; }

    /// <summary>
    /// Whether this is a through hole
    /// </summary>
    public bool ThroughAll { get; set; } = false;

    public HoleFeature(string name, Dimension diameter, Dimension depth) : base(name)
    {
        Diameter = diameter;
        Depth = depth;
    }

    public override string FeatureType => "Hole";

    public override string ToString() => $"Hole '{Name}': D={Diameter}, Depth={Depth}";
}
