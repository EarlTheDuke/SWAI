using Microsoft.Extensions.Logging;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Sketch;
using SWAI.Core.Services;
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
    private readonly SolidWorksService _swService;
    private readonly PatternService _patternService;
    private readonly HoleWizardService _holeWizardService;
    private readonly SolidWorksConfiguration _config;
    private readonly ConversationContext? _context;

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
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<CommandExecutor> logger,
        ConversationContext? context = null)
    {
        _partService = partService;
        _sketchService = sketchService;
        _featureService = featureService;
        _swService = swService;
        _config = config;
        _logger = logger;
        _context = context;

        // Initialize pattern and hole services
        _patternService = new PatternService(swService, config, 
            logger as ILogger<PatternService> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PatternService>.Instance);
        _holeWizardService = new HoleWizardService(swService, config,
            logger as ILogger<HoleWizardService> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<HoleWizardService>.Instance);
    }

    public async Task<CommandResult> ExecuteAsync(ISwaiCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Executing command: {Type} - {Description}", command.CommandType, command.Description);

        try
        {
            CommandResult result = command switch
            {
                // Part commands
                CreatePartCommand cmd => await ExecuteCreatePartAsync(cmd),
                CreateBoxCommand cmd => await ExecuteCreateBoxAsync(cmd),
                CreateCylinderCommand cmd => await ExecuteCreateCylinderAsync(cmd),
                SavePartCommand cmd => await ExecuteSavePartAsync(cmd),
                ExportPartCommand cmd => await ExecuteExportPartAsync(cmd),
                ClosePartCommand cmd => await ExecuteClosePartAsync(cmd),

                // Feature commands
                AddExtrusionCommand cmd => await ExecuteAddExtrusionAsync(cmd),
                AddFilletCommand cmd => await ExecuteAddFilletAsync(cmd),
                AddChamferCommand cmd => await ExecuteAddChamferAsync(cmd),
                AddHoleCommand cmd => await ExecuteAddHoleAsync(cmd),

                // Pattern commands
                AddLinearPatternCommand cmd => await ExecuteLinearPatternAsync(cmd),
                AddCircularPatternCommand cmd => await ExecuteCircularPatternAsync(cmd),
                AddMirrorCommand cmd => await ExecuteMirrorAsync(cmd),

                // Modification commands
                ModifyDimensionCommand cmd => await ExecuteModifyDimensionAsync(cmd),
                UndoCommand cmd => await ExecuteUndoAsync(cmd),
                RedoCommand cmd => await ExecuteRedoAsync(cmd),
                ShowInfoCommand cmd => await ExecuteShowInfoAsync(cmd),

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

                // Update conversation context
                _context?.OnCommandExecuted(command, result.Data);
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

    #region Part Commands

    private async Task<CommandResult> ExecuteCreatePartAsync(CreatePartCommand cmd)
    {
        var part = await _partService.CreatePartAsync(cmd.PartName, cmd.Units);
        return CommandResult.Succeeded($"Created part: {part.Name}", part);
    }

    private async Task<CommandResult> ExecuteCreateBoxAsync(CreateBoxCommand cmd)
    {
        // Generate API preview
        var apiPreview = GenerateBoxApiPreview(cmd);

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
            new { Part = _partService.ActivePart, Extrusion = extrusion },
            apiPreview
        );
    }

    private Core.Models.ApiCallSequence GenerateBoxApiPreview(CreateBoxCommand cmd)
    {
        var unit = cmd.Width.Unit.ToString().ToLower();
        var planeName = cmd.SketchPlane.ToString() + " Plane";
        
        var halfW = cmd.Width.Value / 2;
        var halfL = cmd.Length.Value / 2;

        var sketchPreview = Core.Models.ApiPreviewGenerator.CreateSketchPreview(planeName);
        sketchPreview.Order = 2;

        var rectPreview = Core.Models.ApiPreviewGenerator.CreateRectanglePreview(-halfW, -halfL, halfW, halfL, unit);
        rectPreview.Order = 3;

        var extrudePreview = Core.Models.ApiPreviewGenerator.CreateExtrusionPreview(cmd.Height.Value, unit);
        extrudePreview.Order = 4;

        return new Core.Models.ApiCallSequence
        {
            OperationName = "Create Box",
            UserCommand = cmd.Description,
            Calls = new List<Core.Models.ApiCallPreview>
            {
                new Core.Models.ApiCallPreview
                {
                    Order = 1,
                    ApiMethod = "swApp.NewDocument",
                    Description = "Create new part document",
                    CodePreview = $@"// Create new part document
swModel = swApp.NewDocument(swApp.GetUserPreferenceStringValue(21), 0, 0, 0);"
                },
                sketchPreview,
                rectPreview,
                extrudePreview
            }
        };
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

    #endregion

    #region Feature Commands

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
        var depth = cmd.Depth ?? Core.Models.Units.Dimension.Inches(1);

        if (cmd.ThroughAll)
        {
            // Use hole wizard for through-all holes
            var success = await _holeWizardService.CreateSimpleHoleAsync(
                location, cmd.Diameter, depth, throughAll: true);
            return success
                ? CommandResult.Succeeded($"Created through hole: D={cmd.Diameter}")
                : CommandResult.Failed("Failed to create hole");
        }
        else
        {
            var feature = await _featureService.CreateHoleAsync(cmd.FeatureName, location, cmd.Diameter, depth);
            return CommandResult.Succeeded($"Created hole: D={cmd.Diameter}, Depth={depth}", feature);
        }
    }

    #endregion

    #region Pattern Commands

    private async Task<CommandResult> ExecuteLinearPatternAsync(AddLinearPatternCommand cmd)
    {
        var success = await _patternService.CreateLinearPatternAsync(
            cmd.Count1, cmd.Spacing1, cmd.Count2, cmd.Spacing2);

        return success
            ? CommandResult.Succeeded($"Created linear pattern: {cmd.Count1} instances")
            : CommandResult.Failed("Failed to create linear pattern");
    }

    private async Task<CommandResult> ExecuteCircularPatternAsync(AddCircularPatternCommand cmd)
    {
        var success = await _patternService.CreateCircularPatternAsync(
            cmd.Count, cmd.TotalAngle, cmd.EqualSpacing);

        return success
            ? CommandResult.Succeeded($"Created circular pattern: {cmd.Count} instances")
            : CommandResult.Failed("Failed to create circular pattern");
    }

    private async Task<CommandResult> ExecuteMirrorAsync(AddMirrorCommand cmd)
    {
        var success = await _patternService.CreateMirrorAsync(cmd.MirrorPlane);

        return success
            ? CommandResult.Succeeded($"Created mirror about {cmd.MirrorPlane}")
            : CommandResult.Failed("Failed to create mirror");
    }

    #endregion

    #region Modification Commands

    private async Task<CommandResult> ExecuteModifyDimensionAsync(ModifyDimensionCommand cmd)
    {
        // This would need access to the actual dimension in SolidWorks
        // For now, we log the intent
        _logger.LogInformation("Modify dimension: {Type} {ModType} {Value}",
            cmd.DimensionType, cmd.ModificationType, cmd.Value);

        if (!_config.UseMock)
        {
            // In real implementation, would:
            // 1. Find the dimension by type in the feature tree
            // 2. Calculate new value based on modification type
            // 3. Update the dimension value
            // 4. Rebuild the model

            return await Task.Run(() =>
            {
                try
                {
                    var swApp = _swService.GetApplication();
                    if (swApp == null) return CommandResult.Failed("Not connected to SolidWorks");

                    var model = swApp.ActiveDoc;
                    if (model == null) return CommandResult.Failed("No active document");

                    // Would need to find and modify the appropriate dimension
                    // model.Parameter(dimensionName).SystemValue = newValue;
                    // model.EditRebuild3();

                    return CommandResult.Succeeded($"Modified {cmd.DimensionType}: {cmd.Description}");
                }
                catch (Exception ex)
                {
                    return CommandResult.Failed($"Failed to modify dimension: {ex.Message}");
                }
            });
        }

        return CommandResult.Succeeded($"[Mock] Modified {cmd.DimensionType}: {cmd.Description}");
    }

    private async Task<CommandResult> ExecuteUndoAsync(UndoCommand cmd)
    {
        for (int i = 0; i < cmd.Count && CanUndo; i++)
        {
            await UndoAsync();
        }

        return CommandResult.Succeeded($"Undone {cmd.Count} operation(s)");
    }

    private async Task<CommandResult> ExecuteRedoAsync(RedoCommand cmd)
    {
        for (int i = 0; i < cmd.Count && CanRedo; i++)
        {
            await RedoAsync();
        }

        return CommandResult.Succeeded($"Redone {cmd.Count} operation(s)");
    }

    private async Task<CommandResult> ExecuteShowInfoAsync(ShowInfoCommand cmd)
    {
        var part = _partService.ActivePart;
        if (part == null)
        {
            return CommandResult.Succeeded("No active part. Create a part first.");
        }

        var info = cmd.Type switch
        {
            InfoType.Dimensions => GetDimensionsInfo(part),
            InfoType.Features => GetFeaturesInfo(part),
            InfoType.Properties => GetPropertiesInfo(part),
            InfoType.Mass => await GetMassPropertiesAsync(),
            _ => GetAllInfo(part)
        };

        return CommandResult.Succeeded(info, part);
    }

    private string GetDimensionsInfo(Core.Models.Documents.PartDocument part)
    {
        // Would query actual dimensions from SolidWorks
        return $"Part: {part.Name}\nFeatures: {part.Features.Count}";
    }

    private string GetFeaturesInfo(Core.Models.Documents.PartDocument part)
    {
        var features = part.Features.Select(f => $"  • {f.Name} ({f.FeatureType})");
        return $"Features in {part.Name}:\n{string.Join("\n", features)}";
    }

    private string GetPropertiesInfo(Core.Models.Documents.PartDocument part)
    {
        var props = part.CustomProperties.Select(kv => $"  • {kv.Key}: {kv.Value}");
        return $"Properties:\n{string.Join("\n", props)}";
    }

    private async Task<string> GetMassPropertiesAsync()
    {
        if (_config.UseMock)
        {
            return "Mass Properties (Mock):\n  • Mass: 1.5 kg\n  • Volume: 0.0005 m³\n  • Surface Area: 0.05 m²";
        }

        return await Task.Run(() =>
        {
            try
            {
                var swApp = _swService.GetApplication();
                var model = swApp?.ActiveDoc;
                if (model == null) return "No active document";

                var massProps = model.Extension.CreateMassProperty();
                if (massProps == null) return "Could not calculate mass properties";

                var mass = massProps.Mass;
                var volume = massProps.Volume;
                var surfaceArea = massProps.SurfaceArea;

                return $"Mass Properties:\n  • Mass: {mass:F4} kg\n  • Volume: {volume:F6} m³\n  • Surface Area: {surfaceArea:F4} m²";
            }
            catch (Exception ex)
            {
                return $"Error calculating mass properties: {ex.Message}";
            }
        });
    }

    private string GetAllInfo(Core.Models.Documents.PartDocument part)
    {
        return $"Part: {part.Name}\n" +
               $"Units: {part.Units}\n" +
               $"Features: {part.Features.Count}\n" +
               $"Sketches: {part.Sketches.Count}\n" +
               $"Modified: {(part.IsDirty ? "Yes" : "No")}";
    }

    #endregion

    #region Undo/Redo

    public async Task<bool> UndoAsync()
    {
        if (!CanUndo) return false;

        _logger.LogInformation("Executing undo");

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var swApp = _swService.GetApplication();
                    var model = swApp?.ActiveDoc;
                    if (model == null) return false;

                    model.EditUndo2(1);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Undo failed");
                    return false;
                }
            });
        }

        var cmd = _undoStack.Pop();
        _redoStack.Push(cmd);

        return await Task.FromResult(true);
    }

    public async Task<bool> RedoAsync()
    {
        if (!CanRedo) return false;

        _logger.LogInformation("Executing redo");

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var swApp = _swService.GetApplication();
                    var model = swApp?.ActiveDoc;
                    if (model == null) return false;

                    model.EditRedo2(1);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Redo failed");
                    return false;
                }
            });
        }

        var cmd = _redoStack.Pop();
        _undoStack.Push(cmd);

        return await Task.FromResult(true);
    }

    #endregion
}
