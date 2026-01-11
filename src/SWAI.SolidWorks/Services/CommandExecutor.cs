using Microsoft.Extensions.Logging;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Sketch;
using System.Diagnostics;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Executes SWAI commands against SolidWorks
/// </summary>
public class CommandExecutor : Core.Interfaces.ICommandExecutor
{
    private readonly ILogger<CommandExecutor> _logger;
    private readonly IPartService _partService;
    private readonly ISketchService _sketchService;
    private readonly IFeatureService _featureService;
    private readonly SolidWorksConfiguration _config;

    private readonly List<ISwaiCommand> _history = new();
    private readonly Stack<ISwaiCommand> _undoStack = new();
    private readonly Stack<ISwaiCommand> _redoStack = new();

    public IReadOnlyList<ISwaiCommand> History => _history.AsReadOnly();
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public CommandExecutor(
        IPartService partService,
        ISketchService sketchService,
        IFeatureService featureService,
        SolidWorksConfiguration config,
        ILogger<CommandExecutor> logger)
    {
        _partService = partService;
        _sketchService = sketchService;
        _featureService = featureService;
        _config = config;
        _logger = logger;
    }

    public async Task<CommandResult> ExecuteAsync(ISwaiCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing command: {Type} - {Description}", command.CommandType, command.Description);

        try
        {
            CommandResult result = command switch
            {
                CreatePartCommand cmd => await ExecuteCreatePartAsync(cmd),
                CreateBoxCommand cmd => await ExecuteCreateBoxAsync(cmd),
                CreateCylinderCommand cmd => await ExecuteCreateCylinderAsync(cmd),
                SavePartCommand cmd => await ExecuteSavePartAsync(cmd),
                ExportPartCommand cmd => await ExecuteExportPartAsync(cmd),
                ClosePartCommand cmd => await ExecuteClosePartAsync(cmd),
                AddExtrusionCommand cmd => await ExecuteAddExtrusionAsync(cmd),
                AddFilletCommand cmd => await ExecuteAddFilletAsync(cmd),
                AddChamferCommand cmd => await ExecuteAddChamferAsync(cmd),
                AddHoleCommand cmd => await ExecuteAddHoleAsync(cmd),
                _ => CommandResult.Failed($"Unknown command type: {command.CommandType}")
            };

            stopwatch.Stop();
            result = result with { ExecutionTime = stopwatch.Elapsed };

            if (result.Success)
            {
                _history.Add(command);
                if (command.CanUndo)
                {
                    _undoStack.Push(command);
                    _redoStack.Clear();
                }
            }

            _logger.LogInformation("Command completed in {Elapsed}ms: {Success}",
                stopwatch.ElapsedMilliseconds, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Command execution failed: {Type}", command.CommandType);
            return CommandResult.Failed($"Command failed: {ex.Message}", ex.ToString());
        }
    }

    private async Task<CommandResult> ExecuteCreatePartAsync(CreatePartCommand cmd)
    {
        var part = await _partService.CreatePartAsync(cmd.PartName, cmd.Units);
        return CommandResult.Succeeded($"Created part: {part.Name}", part);
    }

    private async Task<CommandResult> ExecuteCreateBoxAsync(CreateBoxCommand cmd)
    {
        // Create part if needed
        if (_partService.ActivePart == null)
        {
            await _partService.CreatePartAsync(cmd.Name, cmd.Width.Unit);
        }

        // Create sketch on the specified plane
        var sketch = await _sketchService.CreateSketchAsync("Base Sketch", cmd.SketchPlane);

        // Add rectangle
        if (cmd.Centered)
        {
            await _sketchService.AddCenteredRectangleAsync(Point3D.Origin, cmd.Width, cmd.Length);
        }
        else
        {
            await _sketchService.AddRectangleAsync(
                Point3D.Origin,
                new Point3D(cmd.Width.Value, cmd.Length.Value, 0, cmd.Width.Unit)
            );
        }

        // Close sketch and extrude
        await _sketchService.CloseSketchAsync();
        var extrusion = await _featureService.CreateExtrusionAsync(
            "Base Extrusion",
            cmd.Height,
            Core.Models.Features.ExtrusionDirection.SingleDirection
        );

        return CommandResult.Succeeded(
            $"Created box: {cmd.Width} x {cmd.Length} x {cmd.Height}",
            new { Part = _partService.ActivePart, Extrusion = extrusion }
        );
    }

    private async Task<CommandResult> ExecuteCreateCylinderAsync(CreateCylinderCommand cmd)
    {
        // Create part if needed
        if (_partService.ActivePart == null)
        {
            await _partService.CreatePartAsync(cmd.Name, cmd.Diameter.Unit);
        }

        // Create sketch
        var sketch = await _sketchService.CreateSketchAsync("Base Sketch", cmd.SketchPlane);

        // Add circle
        var radius = cmd.Diameter / 2;
        await _sketchService.AddCircleAsync(Point3D.Origin, radius);

        // Close sketch and extrude
        await _sketchService.CloseSketchAsync();
        var extrusion = await _featureService.CreateExtrusionAsync(
            "Base Extrusion",
            cmd.Height,
            Core.Models.Features.ExtrusionDirection.SingleDirection
        );

        return CommandResult.Succeeded(
            $"Created cylinder: D={cmd.Diameter}, H={cmd.Height}",
            new { Part = _partService.ActivePart, Extrusion = extrusion }
        );
    }

    private async Task<CommandResult> ExecuteSavePartAsync(SavePartCommand cmd)
    {
        var success = await _partService.SavePartAsync(cmd.FilePath);
        return success
            ? CommandResult.Succeeded("Part saved successfully")
            : CommandResult.Failed("Failed to save part");
    }

    private async Task<CommandResult> ExecuteExportPartAsync(ExportPartCommand cmd)
    {
        var success = await _partService.ExportPartAsync(cmd.FilePath, cmd.Format);
        return success
            ? CommandResult.Succeeded($"Exported to: {cmd.FilePath}")
            : CommandResult.Failed("Failed to export part");
    }

    private async Task<CommandResult> ExecuteClosePartAsync(ClosePartCommand cmd)
    {
        var success = await _partService.ClosePartAsync(cmd.SaveFirst);
        return success
            ? CommandResult.Succeeded("Part closed")
            : CommandResult.Failed("Failed to close part");
    }

    private async Task<CommandResult> ExecuteAddExtrusionAsync(AddExtrusionCommand cmd)
    {
        var feature = cmd.IsCut
            ? await _featureService.CreateCutExtrusionAsync(cmd.FeatureName, cmd.Depth)
            : (Core.Models.Features.Feature)await _featureService.CreateExtrusionAsync(cmd.FeatureName, cmd.Depth);

        return CommandResult.Succeeded($"Created {(cmd.IsCut ? "cut" : "extrusion")}: {cmd.Depth}", feature);
    }

    private async Task<CommandResult> ExecuteAddFilletAsync(AddFilletCommand cmd)
    {
        var feature = await _featureService.CreateFilletAsync(cmd.FeatureName, cmd.Radius, cmd.AllEdges);
        return CommandResult.Succeeded($"Created fillet: R={cmd.Radius}", feature);
    }

    private async Task<CommandResult> ExecuteAddChamferAsync(AddChamferCommand cmd)
    {
        var feature = await _featureService.CreateChamferAsync(cmd.FeatureName, cmd.Distance, cmd.AllEdges);
        return CommandResult.Succeeded($"Created chamfer: {cmd.Distance}", feature);
    }

    private async Task<CommandResult> ExecuteAddHoleAsync(AddHoleCommand cmd)
    {
        var location = cmd.Location ?? Point3D.Origin;
        var depth = cmd.Depth ?? Core.Models.Units.Dimension.Inches(1); // Default 1 inch

        var feature = await _featureService.CreateHoleAsync(cmd.FeatureName, location, cmd.Diameter, depth);
        return CommandResult.Succeeded($"Created hole: D={cmd.Diameter}", feature);
    }

    public async Task<bool> UndoAsync()
    {
        if (!CanUndo) return false;

        // In a real implementation, this would use SolidWorks' undo
        _logger.LogInformation("Undo requested - feature not fully implemented");

        var cmd = _undoStack.Pop();
        _redoStack.Push(cmd);

        return await Task.FromResult(true);
    }

    public async Task<bool> RedoAsync()
    {
        if (!CanRedo) return false;

        _logger.LogInformation("Redo requested - feature not fully implemented");

        var cmd = _redoStack.Pop();
        _undoStack.Push(cmd);

        return await Task.FromResult(true);
    }
}
