using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Service for part document operations
/// </summary>
public class PartService : IPartService
{
    private readonly ILogger<PartService> _logger;
    private readonly SolidWorksService _swService;
    private readonly SolidWorksConfiguration _config;
    private PartDocument? _activePart;

    public PartDocument? ActivePart => _activePart;

    public event EventHandler<PartDocument?>? ActivePartChanged;

    public PartService(
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<PartService> logger)
    {
        _swService = swService;
        _config = config;
        _logger = logger;
    }

    public async Task<PartDocument> CreatePartAsync(string name, UnitSystem units = UnitSystem.Inches)
    {
        _logger.LogInformation("Creating new part: {Name}", name);

        var part = new PartDocument(name) { Units = units };

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null)
                    throw new InvalidOperationException("Not connected to SolidWorks");

                // Create new part document
                // swDocPART = 1
                var model = swApp.NewDocument(
                    swApp.GetUserPreferenceStringValue(21), // Default part template
                    0, // swDwgPaperAsize
                    0, // Width (not used for parts)
                    0  // Height (not used for parts)
                );

                if (model == null)
                {
                    // Try with explicit template path
                    var templatePath = GetDefaultPartTemplate();
                    model = swApp.NewDocument(templatePath, 0, 0, 0);
                }

                if (model == null)
                    throw new InvalidOperationException("Failed to create new part document");

                _logger.LogInformation("Part document created in SolidWorks");
            });
        }

        SetActivePart(part);
        return part;
    }

    public async Task<PartDocument?> OpenPartAsync(string filePath)
    {
        _logger.LogInformation("Opening part: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return null;
        }

        var part = new PartDocument(Path.GetFileNameWithoutExtension(filePath))
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
                    1, // swDocPART
                    0, // swOpenDocOptions_Silent
                    "",
                    ref errors,
                    ref warnings
                );

                if (model == null)
                    throw new InvalidOperationException($"Failed to open part. Errors: {errors}");

                _logger.LogInformation("Part opened in SolidWorks");
            });
        }

        SetActivePart(part);
        return part;
    }

    public async Task<bool> SavePartAsync(string? filePath = null)
    {
        if (_activePart == null)
        {
            _logger.LogWarning("No active part to save");
            return false;
        }

        var savePath = filePath ?? _activePart.FilePath;
        if (string.IsNullOrEmpty(savePath))
        {
            savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"{_activePart.Name}.sldprt"
            );
        }

        _logger.LogInformation("Saving part to: {Path}", savePath);

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

                var success = model.Extension.SaveAs(
                    savePath,
                    0, // swSaveAsCurrentVersion
                    1, // swSaveAsOptions_Silent
                    null,
                    ref errors,
                    ref warnings
                );

                if (success)
                {
                    _activePart!.MarkSaved(savePath);
                    _logger.LogInformation("Part saved successfully");
                }
                else
                {
                    _logger.LogError("Failed to save part. Errors: {Errors}", errors);
                }

                return success;
            });
        }

        _activePart.MarkSaved(savePath);
        return true;
    }

    public async Task<bool> ExportPartAsync(string filePath, ExportFormat format)
    {
        if (_activePart == null)
        {
            _logger.LogWarning("No active part to export");
            return false;
        }

        // Ensure correct extension
        var extension = PartDocument.GetExtension(format);
        if (!filePath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            filePath = Path.ChangeExtension(filePath, extension);
        }

        _logger.LogInformation("Exporting part to: {Path} as {Format}", filePath, format);

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

                var success = model.Extension.SaveAs(
                    filePath,
                    0, // swSaveAsCurrentVersion
                    1, // swSaveAsOptions_Silent
                    null,
                    ref errors,
                    ref warnings
                );

                if (success)
                    _logger.LogInformation("Part exported successfully");
                else
                    _logger.LogError("Failed to export part. Errors: {Errors}", errors);

                return success;
            });
        }

        return true;
    }

    public async Task<bool> ClosePartAsync(bool saveFirst = false)
    {
        if (_activePart == null)
        {
            _logger.LogWarning("No active part to close");
            return false;
        }

        if (saveFirst && _activePart.IsDirty)
        {
            await SavePartAsync();
        }

        _logger.LogInformation("Closing part: {Name}", _activePart.Name);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                swApp.CloseDoc(_activePart!.Name);
            });
        }

        SetActivePart(null);
        return true;
    }

    public async Task<bool> RebuildAsync()
    {
        if (_activePart == null)
        {
            return false;
        }

        _logger.LogInformation("Rebuilding part");

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

    private void SetActivePart(PartDocument? part)
    {
        _activePart = part;
        ActivePartChanged?.Invoke(this, part);
    }

    private string GetDefaultPartTemplate()
    {
        // Common template locations
        var possiblePaths = new[]
        {
            @"C:\ProgramData\SolidWorks\SOLIDWORKS 2025\templates\Part.prtdot",
            @"C:\ProgramData\SolidWorks\SOLIDWORKS 2024\templates\Part.prtdot",
            @"C:\ProgramData\SolidWorks\SOLIDWORKS 2023\templates\Part.prtdot",
            Path.Combine(_config.InstallPath ?? "", @"lang\english\Tutorial\Part.prtdot")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        return "Part.prtdot"; // Let SolidWorks try to find it
    }
}
