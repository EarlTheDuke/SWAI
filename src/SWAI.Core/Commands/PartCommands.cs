using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Commands;

/// <summary>
/// Command to create a new part document
/// </summary>
public class CreatePartCommand : SwaiCommandBase
{
    public string PartName { get; init; }
    public UnitSystem Units { get; init; } = UnitSystem.Inches;

    public CreatePartCommand(string partName)
    {
        PartName = partName;
    }

    public override string CommandType => "CreatePart";
    public override string Description => $"Create new part: {PartName}";
}

/// <summary>
/// Command to create a rectangular box/plate
/// </summary>
public class CreateBoxCommand : SwaiCommandBase
{
    public string Name { get; init; }
    public Dimension Width { get; init; }
    public Dimension Length { get; init; }
    public Dimension Height { get; init; }
    public ReferencePlane SketchPlane { get; init; } = ReferencePlane.Top;
    public bool Centered { get; init; } = true;

    public CreateBoxCommand(string name, Dimension width, Dimension length, Dimension height)
    {
        Name = name;
        Width = width;
        Length = length;
        Height = height;
    }

    public override string CommandType => "CreateBox";
    public override string Description => 
        $"Create box '{Name}': {Width} x {Length} x {Height}";
}

/// <summary>
/// Command to create a cylinder
/// </summary>
public class CreateCylinderCommand : SwaiCommandBase
{
    public string Name { get; init; }
    public Dimension Diameter { get; init; }
    public Dimension Height { get; init; }
    public ReferencePlane SketchPlane { get; init; } = ReferencePlane.Top;
    public bool Centered { get; init; } = true;

    public CreateCylinderCommand(string name, Dimension diameter, Dimension height)
    {
        Name = name;
        Diameter = diameter;
        Height = height;
    }

    public override string CommandType => "CreateCylinder";
    public override string Description => 
        $"Create cylinder '{Name}': D={Diameter}, H={Height}";
}

/// <summary>
/// Command to save the current document
/// </summary>
public class SavePartCommand : SwaiCommandBase
{
    public string? FilePath { get; init; }
    public ExportFormat Format { get; init; } = ExportFormat.SolidWorksPart;

    public override string CommandType => "SavePart";
    public override string Description => 
        $"Save as {Format}" + (FilePath != null ? $" to {FilePath}" : "");
    public override bool CanUndo => false;
}

/// <summary>
/// Command to export the current document
/// </summary>
public class ExportPartCommand : SwaiCommandBase
{
    public string FilePath { get; init; }
    public ExportFormat Format { get; init; }

    public ExportPartCommand(string filePath, ExportFormat format)
    {
        FilePath = filePath;
        Format = format;
    }

    public override string CommandType => "ExportPart";
    public override string Description => $"Export as {Format} to {FilePath}";
    public override bool CanUndo => false;
}

/// <summary>
/// Command to close the current document
/// </summary>
public class ClosePartCommand : SwaiCommandBase
{
    public bool SaveFirst { get; init; } = false;

    public override string CommandType => "ClosePart";
    public override string Description => SaveFirst ? "Save and close part" : "Close part";
    public override bool CanUndo => false;
}
