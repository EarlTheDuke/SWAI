using Microsoft.Extensions.Logging;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Service for assembly operations
/// </summary>
public class AssemblyService : IAssemblyService
{
    private readonly ILogger<AssemblyService> _logger;
    private readonly SolidWorksService _swService;
    private readonly SolidWorksConfiguration _config;
    private AssemblyDocument? _activeAssembly;

    public AssemblyDocument? ActiveAssembly => _activeAssembly;

    public event EventHandler<AssemblyDocument?>? ActiveAssemblyChanged;

    public AssemblyService(
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<AssemblyService> logger)
    {
        _swService = swService;
        _config = config;
        _logger = logger;
    }

    public async Task<AssemblyDocument> CreateAssemblyAsync(string name, UnitSystem units = UnitSystem.Inches)
    {
        _logger.LogInformation("Creating new assembly: {Name}", name);

        var assembly = new AssemblyDocument(name) { Units = units };

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null)
                    throw new InvalidOperationException("Not connected to SolidWorks");

                // Create new assembly document (swDocASSEMBLY = 2)
                var templatePath = GetDefaultAssemblyTemplate();
                var model = swApp.NewDocument(templatePath, 0, 0, 0);

                if (model == null)
                    throw new InvalidOperationException("Failed to create new assembly document");

                _logger.LogInformation("Assembly document created in SolidWorks");
            });
        }

        SetActiveAssembly(assembly);
        return assembly;
    }

    public async Task<AssemblyDocument?> OpenAssemblyAsync(string filePath)
    {
        _logger.LogInformation("Opening assembly: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return null;
        }

        var assembly = new AssemblyDocument(Path.GetFileNameWithoutExtension(filePath))
        {
            FilePath = filePath
        };

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null)
                    throw new InvalidOperationException("Not connected to SolidWorks");

                int errors = 0;
                int warnings = 0;

                var model = swApp.OpenDoc6(
                    filePath,
                    2, // swDocASSEMBLY
                    0, // swOpenDocOptions_Silent
                    "",
                    ref errors,
                    ref warnings
                );

                if (model == null)
                    throw new InvalidOperationException($"Failed to open assembly. Errors: {errors}");

                _logger.LogInformation("Assembly opened in SolidWorks");
            });
        }

        SetActiveAssembly(assembly);
        return assembly;
    }

    public async Task<bool> SaveAssemblyAsync(string? filePath = null, bool saveComponents = false)
    {
        if (_activeAssembly == null)
        {
            _logger.LogWarning("No active assembly to save");
            return false;
        }

        var savePath = filePath ?? _activeAssembly.FilePath;
        if (string.IsNullOrEmpty(savePath))
        {
            savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"{_activeAssembly.Name}.sldasm"
            );
        }

        _logger.LogInformation("Saving assembly to: {Path}", savePath);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                int errors = 0;
                int warnings = 0;

                // Save options
                var options = saveComponents ? 2 : 1; // swSaveAsOptions_SaveReferenced

                var success = model.Extension.SaveAs(
                    savePath,
                    0,
                    options,
                    null,
                    ref errors,
                    ref warnings
                );

                if (success)
                {
                    _activeAssembly!.MarkSaved(savePath);
                    _logger.LogInformation("Assembly saved successfully");
                }
                else
                {
                    _logger.LogError("Failed to save assembly. Errors: {Errors}", errors);
                }

                return success;
            });
        }

        _activeAssembly.MarkSaved(savePath);
        return true;
    }

    public async Task<bool> CloseAssemblyAsync(bool saveFirst = false)
    {
        if (_activeAssembly == null)
        {
            _logger.LogWarning("No active assembly to close");
            return false;
        }

        if (saveFirst && _activeAssembly.IsDirty)
        {
            await SaveAssemblyAsync();
        }

        _logger.LogInformation("Closing assembly: {Name}", _activeAssembly.Name);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                swApp.CloseDoc(_activeAssembly!.Name);
            });
        }

        SetActiveAssembly(null);
        return true;
    }

    public async Task<AssemblyComponent?> InsertComponentAsync(
        string partPath,
        Point3D? position = null,
        bool isFixed = false,
        string? configuration = null)
    {
        if (_activeAssembly == null)
        {
            _logger.LogWarning("No active assembly");
            return null;
        }

        _logger.LogInformation("Inserting component: {Path}", partPath);

        var componentName = Path.GetFileNameWithoutExtension(partPath);
        var instanceNumber = _activeAssembly.Components.Count(c => c.Name == componentName) + 1;

        var component = new AssemblyComponent(componentName, partPath)
        {
            InstanceNumber = instanceNumber,
            InstanceName = $"{componentName}-{instanceNumber}",
            IsFixed = isFixed,
            ConfigurationName = configuration
        };

        if (position.HasValue)
        {
            component.Transform = ComponentTransform.AtPosition(
                position.Value.X.Meters,
                position.Value.Y.Meters,
                position.Value.Z.Meters
            );
        }

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                var model = swApp.ActiveDoc;
                if (model == null) return;

                var assemblyDoc = model; // Cast to AssemblyDoc in real implementation

                // AddComponent5 parameters
                var transform = component.Transform.ToMatrix();
                var pos = position.HasValue
                    ? new double[] { position.Value.X.Meters, position.Value.Y.Meters, position.Value.Z.Meters }
                    : new double[] { 0, 0, 0 };

                var comp = assemblyDoc.AddComponent5(
                    partPath,
                    0,  // swAddComponentConfigOptions_CurrentSelectedConfig
                    configuration ?? "",
                    false, // UseConfigForPartReferences
                    configuration ?? "",
                    pos[0], pos[1], pos[2]
                );

                if (comp != null && isFixed)
                {
                    comp.SetFixedState2(true);
                }

                _logger.LogInformation("Component inserted: {Name}", component.InstanceName);
            });
        }

        _activeAssembly.AddComponent(component);
        return component;
    }

    public async Task<List<AssemblyComponent>> InsertComponentsAsync(
        string partPath,
        int count,
        Dimension spacing,
        PatternDirection direction = PatternDirection.X)
    {
        var components = new List<AssemblyComponent>();

        for (int i = 0; i < count; i++)
        {
            var offset = direction switch
            {
                PatternDirection.X => new Point3D(spacing.Value * i, 0, 0, spacing.Unit),
                PatternDirection.Y => new Point3D(0, spacing.Value * i, 0, spacing.Unit),
                PatternDirection.Z => new Point3D(0, 0, spacing.Value * i, spacing.Unit),
                _ => Point3D.Origin
            };

            var comp = await InsertComponentAsync(partPath, offset, isFixed: i == 0);
            if (comp != null)
            {
                components.Add(comp);
            }
        }

        return components;
    }

    public async Task<bool> MoveComponentAsync(string componentName, Point3D newPosition)
    {
        _logger.LogInformation("Moving component {Name} to {Position}", componentName, newPosition);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                // Select the component
                model.Extension.SelectByID2(componentName, "COMPONENT", 0, 0, 0, false, 0, null, 0);

                // In real implementation, would use IComponent2.Transform2
                return true;
            });
        }

        // Update mock
        var component = _activeAssembly?.FindComponent(componentName);
        if (component != null)
        {
            component.Transform = ComponentTransform.AtPosition(
                newPosition.X.Meters,
                newPosition.Y.Meters,
                newPosition.Z.Meters
            );
            return true;
        }

        return false;
    }

    public async Task<bool> MoveComponentByAsync(string componentName, Vector3D offset)
    {
        var component = _activeAssembly?.FindComponent(componentName);
        if (component == null) return false;

        var newPosition = new Point3D(
            component.Transform.X + offset.X.Meters,
            component.Transform.Y + offset.Y.Meters,
            component.Transform.Z + offset.Z.Meters,
            UnitSystem.Meters
        );

        return await MoveComponentAsync(componentName, newPosition);
    }

    public async Task<bool> RotateComponentAsync(string componentName, double angleX, double angleY, double angleZ)
    {
        _logger.LogInformation("Rotating component {Name}", componentName);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                // Would implement rotation via transform matrix
                return true;
            });
        }

        var component = _activeAssembly?.FindComponent(componentName);
        if (component != null)
        {
            component.Transform.RotationX = angleX * Math.PI / 180;
            component.Transform.RotationY = angleY * Math.PI / 180;
            component.Transform.RotationZ = angleZ * Math.PI / 180;
            return true;
        }

        return false;
    }

    public async Task<bool> FixComponentAsync(string componentName, bool fix = true)
    {
        _logger.LogInformation("{Action} component {Name}", fix ? "Fixing" : "Floating", componentName);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                model.Extension.SelectByID2(componentName, "COMPONENT", 0, 0, 0, false, 0, null, 0);

                var selMgr = model.SelectionManager;
                var comp = selMgr.GetSelectedObject6(1, -1);
                if (comp != null)
                {
                    comp.SetFixedState2(fix);
                    return true;
                }

                return false;
            });
        }

        var component = _activeAssembly?.FindComponent(componentName);
        if (component != null)
        {
            component.IsFixed = fix;
            return true;
        }

        return false;
    }

    public async Task<bool> SuppressComponentAsync(string componentName, bool suppress = true)
    {
        _logger.LogInformation("{Action} component {Name}", suppress ? "Suppressing" : "Unsuppressing", componentName);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                model.Extension.SelectByID2(componentName, "COMPONENT", 0, 0, 0, false, 0, null, 0);

                if (suppress)
                    model.EditSuppress2();
                else
                    model.EditUnsuppress2();

                return true;
            });
        }

        var component = _activeAssembly?.FindComponent(componentName);
        if (component != null)
        {
            component.IsSuppressed = suppress;
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteComponentAsync(string componentName)
    {
        _logger.LogInformation("Deleting component {Name}", componentName);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                model.Extension.SelectByID2(componentName, "COMPONENT", 0, 0, 0, false, 0, null, 0);
                return model.EditDelete();
            });
        }

        if (_activeAssembly != null)
        {
            var component = _activeAssembly.FindComponent(componentName);
            if (component != null)
            {
                _activeAssembly.Components.Remove(component);
                return true;
            }
        }

        return false;
    }

    public async Task<List<AssemblyComponent>> GetComponentsAsync()
    {
        if (_activeAssembly == null)
            return new List<AssemblyComponent>();

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var components = new List<AssemblyComponent>();
                var swApp = _swService.GetApplication();
                if (swApp == null) return components;

                var model = swApp.ActiveDoc;
                if (model == null) return components;

                // Would enumerate through assembly components
                // var assemblyDoc = (AssemblyDoc)model;
                // foreach component in assemblyDoc.GetComponents(false)

                return components;
            });
        }

        return _activeAssembly.Components.ToList();
    }

    public async Task<bool> RebuildAsync()
    {
        if (_activeAssembly == null) return false;

        _logger.LogInformation("Rebuilding assembly");

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                return model.EditRebuild3();
            });
        }

        return true;
    }

    private void SetActiveAssembly(AssemblyDocument? assembly)
    {
        _activeAssembly = assembly;
        ActiveAssemblyChanged?.Invoke(this, assembly);
    }

    private string GetDefaultAssemblyTemplate()
    {
        var possiblePaths = new[]
        {
            @"C:\ProgramData\SolidWorks\SOLIDWORKS 2025\templates\Assembly.asmdot",
            @"C:\ProgramData\SolidWorks\SOLIDWORKS 2024\templates\Assembly.asmdot",
            @"C:\ProgramData\SolidWorks\SOLIDWORKS 2023\templates\Assembly.asmdot",
            Path.Combine(_config.InstallPath ?? "", @"lang\english\Tutorial\Assembly.asmdot")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return "Assembly.asmdot";
    }
}
