using SWAI.Core.Models.Units;

namespace SWAI.Core.Commands;

/// <summary>
/// The type of dimension to modify
/// </summary>
public enum DimensionType
{
    Width,
    Length,
    Height,
    Depth,
    Thickness,
    Diameter,
    Radius
}

/// <summary>
/// How to apply the modification
/// </summary>
public enum ModificationType
{
    SetTo,      // Set to absolute value
    IncreaseBy, // Add to current value
    DecreaseBy, // Subtract from current value
    MultiplyBy, // Multiply current value
    DivideBy    // Divide current value
}

/// <summary>
/// Command to modify an existing dimension
/// </summary>
public class ModifyDimensionCommand : SwaiCommandBase
{
    /// <summary>
    /// The type of dimension to modify
    /// </summary>
    public DimensionType DimensionType { get; init; }
    
    /// <summary>
    /// How to apply the modification
    /// </summary>
    public ModificationType ModificationType { get; init; }
    
    /// <summary>
    /// The value to apply
    /// </summary>
    public Dimension Value { get; init; }
    
    /// <summary>
    /// Feature name to modify (null = last feature)
    /// </summary>
    public string? FeatureName { get; init; }

    public ModifyDimensionCommand(DimensionType dimType, ModificationType modType, Dimension value)
    {
        DimensionType = dimType;
        ModificationType = modType;
        Value = value;
    }

    public override string CommandType => "ModifyDimension";
    public override string Description => ModificationType switch
    {
        ModificationType.SetTo => $"Set {DimensionType} to {Value}",
        ModificationType.IncreaseBy => $"Increase {DimensionType} by {Value}",
        ModificationType.DecreaseBy => $"Decrease {DimensionType} by {Value}",
        ModificationType.MultiplyBy => $"Multiply {DimensionType} by {Value.Value}",
        ModificationType.DivideBy => $"Divide {DimensionType} by {Value.Value}",
        _ => $"Modify {DimensionType}"
    };
}

/// <summary>
/// Command to undo the last operation
/// </summary>
public class UndoCommand : SwaiCommandBase
{
    /// <summary>
    /// Number of operations to undo (default 1)
    /// </summary>
    public int Count { get; init; } = 1;

    public override string CommandType => "Undo";
    public override string Description => Count > 1 ? $"Undo last {Count} operations" : "Undo last operation";
    public override bool CanUndo => false;
}

/// <summary>
/// Command to redo a previously undone operation
/// </summary>
public class RedoCommand : SwaiCommandBase
{
    /// <summary>
    /// Number of operations to redo (default 1)
    /// </summary>
    public int Count { get; init; } = 1;

    public override string CommandType => "Redo";
    public override string Description => Count > 1 ? $"Redo {Count} operations" : "Redo last undone operation";
    public override bool CanUndo => false;
}

/// <summary>
/// Command to show information about the current part
/// </summary>
public class ShowInfoCommand : SwaiCommandBase
{
    /// <summary>
    /// What information to show
    /// </summary>
    public InfoType Type { get; init; } = InfoType.All;

    public override string CommandType => "ShowInfo";
    public override string Description => $"Show {Type} information";
    public override bool CanUndo => false;
}

public enum InfoType
{
    All,
    Dimensions,
    Features,
    Properties,
    Mass
}
