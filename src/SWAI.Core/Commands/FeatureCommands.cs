using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Sketch;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Commands;

/// <summary>
/// Command to add an extrusion feature
/// </summary>
public class AddExtrusionCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    public SketchProfile? Profile { get; init; }
    public Dimension Depth { get; init; }
    public bool IsCut { get; init; } = false;
    public bool MidPlane { get; init; } = false;

    public AddExtrusionCommand(string featureName, Dimension depth, bool isCut = false)
    {
        FeatureName = featureName;
        Depth = depth;
        IsCut = isCut;
    }

    public override string CommandType => IsCut ? "CutExtrude" : "BossExtrude";
    public override string Description =>
        IsCut ? $"Cut extrude: {Depth} deep" : $"Boss extrude: {Depth} deep";
}

/// <summary>
/// Command to add a fillet
/// </summary>
public class AddFilletCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    public Dimension Radius { get; init; }
    public bool AllEdges { get; init; } = false;
    public List<string> EdgeSelections { get; init; } = new();

    public AddFilletCommand(string featureName, Dimension radius)
    {
        FeatureName = featureName;
        Radius = radius;
    }

    public override string CommandType => "Fillet";
    public override string Description =>
        AllEdges ? $"Fillet all edges: R={Radius}" : $"Fillet: R={Radius}";
}

/// <summary>
/// Command to add a chamfer
/// </summary>
public class AddChamferCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    public Dimension Distance { get; init; }
    public Dimension? Distance2 { get; init; }
    public double? Angle { get; init; }
    public bool AllEdges { get; init; } = false;

    public AddChamferCommand(string featureName, Dimension distance)
    {
        FeatureName = featureName;
        Distance = distance;
    }

    public override string CommandType => "Chamfer";
    public override string Description => $"Chamfer: {Distance}";
}

/// <summary>
/// Command to add a hole
/// </summary>
public class AddHoleCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    public Dimension Diameter { get; init; }
    public Dimension? Depth { get; init; }
    public bool ThroughAll { get; init; } = false;
    public Point3D? Location { get; init; }

    public AddHoleCommand(string featureName, Dimension diameter)
    {
        FeatureName = featureName;
        Diameter = diameter;
    }

    public override string CommandType => "Hole";
    public override string Description =>
        ThroughAll ? $"Through hole: D={Diameter}" : $"Hole: D={Diameter}, Depth={Depth}";
}

/// <summary>
/// Command to create a sketch
/// </summary>
public class CreateSketchCommand : SwaiCommandBase
{
    public string SketchName { get; init; }
    public ReferencePlane Plane { get; init; } = ReferencePlane.Front;
    public List<SketchEntity> Entities { get; init; } = new();

    public CreateSketchCommand(string sketchName, ReferencePlane plane = ReferencePlane.Front)
    {
        SketchName = sketchName;
        Plane = plane;
    }

    public override string CommandType => "CreateSketch";
    public override string Description => $"Create sketch '{SketchName}' on {Plane}";
}

/// <summary>
/// Command to add a sketch rectangle
/// </summary>
public class AddSketchRectangleCommand : SwaiCommandBase
{
    public Dimension Width { get; init; }
    public Dimension Height { get; init; }
    public bool Centered { get; init; } = true;
    public Point3D? Corner { get; init; }

    public AddSketchRectangleCommand(Dimension width, Dimension height)
    {
        Width = width;
        Height = height;
    }

    public override string CommandType => "SketchRectangle";
    public override string Description => $"Rectangle: {Width} x {Height}";
}

/// <summary>
/// Command to add a sketch circle
/// </summary>
public class AddSketchCircleCommand : SwaiCommandBase
{
    public Dimension Diameter { get; init; }
    public Point3D? Center { get; init; }

    public AddSketchCircleCommand(Dimension diameter)
    {
        Diameter = diameter;
    }

    public override string CommandType => "SketchCircle";
    public override string Description => $"Circle: D={Diameter}";
}
