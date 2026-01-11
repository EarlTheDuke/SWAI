using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Commands;

/// <summary>
/// Command to create a new assembly
/// </summary>
public class CreateAssemblyCommand : SwaiCommandBase
{
    public string AssemblyName { get; init; }
    public UnitSystem Units { get; init; } = UnitSystem.Inches;

    public CreateAssemblyCommand(string assemblyName)
    {
        AssemblyName = assemblyName;
    }

    public override string CommandType => "CreateAssembly";
    public override string Description => $"Create new assembly: {AssemblyName}";
}

/// <summary>
/// Command to insert a component into an assembly
/// </summary>
public class InsertComponentCommand : SwaiCommandBase
{
    /// <summary>
    /// Path to the part or assembly file to insert
    /// </summary>
    public string ComponentPath { get; init; }

    /// <summary>
    /// Optional name for the instance
    /// </summary>
    public string? InstanceName { get; init; }

    /// <summary>
    /// Position to insert at (null = origin)
    /// </summary>
    public Point3D? Position { get; init; }

    /// <summary>
    /// Whether to fix/ground the component
    /// </summary>
    public bool Fixed { get; init; } = false;

    /// <summary>
    /// Configuration to use
    /// </summary>
    public string? Configuration { get; init; }

    public InsertComponentCommand(string componentPath)
    {
        ComponentPath = componentPath;
    }

    public override string CommandType => "InsertComponent";
    public override string Description => $"Insert component: {Path.GetFileNameWithoutExtension(ComponentPath)}";
}

/// <summary>
/// Command to add a mate between components
/// </summary>
public class AddMateCommand : SwaiCommandBase
{
    /// <summary>
    /// Name for the mate
    /// </summary>
    public string MateName { get; init; }

    /// <summary>
    /// Type of mate
    /// </summary>
    public MateType MateType { get; init; }

    /// <summary>
    /// First entity reference
    /// </summary>
    public MateReference Entity1 { get; init; }

    /// <summary>
    /// Second entity reference
    /// </summary>
    public MateReference Entity2 { get; init; }

    /// <summary>
    /// Alignment option
    /// </summary>
    public MateAlignment Alignment { get; init; } = MateAlignment.Closest;

    /// <summary>
    /// Distance (for distance mates)
    /// </summary>
    public Dimension? Distance { get; init; }

    /// <summary>
    /// Angle in degrees (for angle mates)
    /// </summary>
    public double? Angle { get; init; }

    /// <summary>
    /// Whether to flip the mate direction
    /// </summary>
    public bool FlipDirection { get; init; }

    public AddMateCommand(string mateName, MateType type, MateReference entity1, MateReference entity2)
    {
        MateName = mateName;
        MateType = type;
        Entity1 = entity1;
        Entity2 = entity2;
    }

    public override string CommandType => "AddMate";
    public override string Description => $"Add {MateType} mate: {MateName}";
}

/// <summary>
/// Command to add a coincident mate (simplified)
/// </summary>
public class AddCoincidentMateCommand : SwaiCommandBase
{
    public string Component1 { get; init; }
    public string Face1 { get; init; }
    public string Component2 { get; init; }
    public string Face2 { get; init; }
    public MateAlignment Alignment { get; init; } = MateAlignment.Closest;

    public AddCoincidentMateCommand(string comp1, string face1, string comp2, string face2)
    {
        Component1 = comp1;
        Face1 = face1;
        Component2 = comp2;
        Face2 = face2;
    }

    public override string CommandType => "CoincidentMate";
    public override string Description => $"Coincident: {Component1}.{Face1} to {Component2}.{Face2}";
}

/// <summary>
/// Command to add a concentric mate (simplified)
/// </summary>
public class AddConcentricMateCommand : SwaiCommandBase
{
    public string Component1 { get; init; }
    public string Cylinder1 { get; init; }
    public string Component2 { get; init; }
    public string Cylinder2 { get; init; }

    public AddConcentricMateCommand(string comp1, string cyl1, string comp2, string cyl2)
    {
        Component1 = comp1;
        Cylinder1 = cyl1;
        Component2 = comp2;
        Cylinder2 = cyl2;
    }

    public override string CommandType => "ConcentricMate";
    public override string Description => $"Concentric: {Component1}.{Cylinder1} to {Component2}.{Cylinder2}";
}

/// <summary>
/// Command to add a distance mate (simplified)
/// </summary>
public class AddDistanceMateCommand : SwaiCommandBase
{
    public string Component1 { get; init; }
    public string Face1 { get; init; }
    public string Component2 { get; init; }
    public string Face2 { get; init; }
    public Dimension Distance { get; init; }

    public AddDistanceMateCommand(string comp1, string face1, string comp2, string face2, Dimension distance)
    {
        Component1 = comp1;
        Face1 = face1;
        Component2 = comp2;
        Face2 = face2;
        Distance = distance;
    }

    public override string CommandType => "DistanceMate";
    public override string Description => $"Distance mate: {Distance} between {Component1} and {Component2}";
}

/// <summary>
/// Command to move a component
/// </summary>
public class MoveComponentCommand : SwaiCommandBase
{
    public string ComponentName { get; init; }
    public Point3D? NewPosition { get; init; }
    public Vector3D? Offset { get; init; }

    public MoveComponentCommand(string componentName)
    {
        ComponentName = componentName;
    }

    public override string CommandType => "MoveComponent";
    public override string Description => NewPosition.HasValue
        ? $"Move {ComponentName} to {NewPosition.Value}"
        : $"Move {ComponentName} by {Offset}";
}

/// <summary>
/// Command to rotate a component
/// </summary>
public class RotateComponentCommand : SwaiCommandBase
{
    public string ComponentName { get; init; }

    /// <summary>
    /// Rotation angles in degrees
    /// </summary>
    public double AngleX { get; init; }
    public double AngleY { get; init; }
    public double AngleZ { get; init; }

    public RotateComponentCommand(string componentName, double angleX = 0, double angleY = 0, double angleZ = 0)
    {
        ComponentName = componentName;
        AngleX = angleX;
        AngleY = angleY;
        AngleZ = angleZ;
    }

    public override string CommandType => "RotateComponent";
    public override string Description => $"Rotate {ComponentName}";
}

/// <summary>
/// Command to create a component pattern in assembly
/// </summary>
public class AssemblyPatternCommand : SwaiCommandBase
{
    public string ComponentName { get; init; }
    public PatternType PatternType { get; init; }
    public int Count { get; init; }
    public Dimension? Spacing { get; init; }
    public double? TotalAngle { get; init; }

    public AssemblyPatternCommand(string componentName, PatternType type, int count)
    {
        ComponentName = componentName;
        PatternType = type;
        Count = count;
    }

    public override string CommandType => "AssemblyPattern";
    public override string Description => $"{PatternType} pattern of {ComponentName}: {Count} instances";
}

/// <summary>
/// Command to suppress/unsuppress a component
/// </summary>
public class SuppressComponentCommand : SwaiCommandBase
{
    public string ComponentName { get; init; }
    public bool Suppress { get; init; }

    public SuppressComponentCommand(string componentName, bool suppress = true)
    {
        ComponentName = componentName;
        Suppress = suppress;
    }

    public override string CommandType => Suppress ? "SuppressComponent" : "UnsuppressComponent";
    public override string Description => Suppress
        ? $"Suppress {ComponentName}"
        : $"Unsuppress {ComponentName}";
}

/// <summary>
/// Command to fix/float a component
/// </summary>
public class FixComponentCommand : SwaiCommandBase
{
    public string ComponentName { get; init; }
    public bool Fix { get; init; }

    public FixComponentCommand(string componentName, bool fix = true)
    {
        ComponentName = componentName;
        Fix = fix;
    }

    public override string CommandType => Fix ? "FixComponent" : "FloatComponent";
    public override string Description => Fix
        ? $"Fix {ComponentName} in place"
        : $"Float {ComponentName}";
}

/// <summary>
/// Command to save the assembly
/// </summary>
public class SaveAssemblyCommand : SwaiCommandBase
{
    public string? FilePath { get; init; }
    public bool SaveComponents { get; init; } = false;

    public override string CommandType => "SaveAssembly";
    public override string Description => SaveComponents
        ? "Save assembly and all components"
        : "Save assembly";
    public override bool CanUndo => false;
}

/// <summary>
/// Command to show assembly information
/// </summary>
public class ShowAssemblyInfoCommand : SwaiCommandBase
{
    public AssemblyInfoType InfoType { get; init; } = AssemblyInfoType.All;

    public override string CommandType => "ShowAssemblyInfo";
    public override string Description => $"Show assembly {InfoType} info";
    public override bool CanUndo => false;
}

public enum AssemblyInfoType
{
    All,
    Components,
    Mates,
    BillOfMaterials,
    MassProperties
}
