using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Service for pattern operations (linear, circular, mirror)
/// </summary>
public class PatternService
{
    private readonly ILogger<PatternService> _logger;
    private readonly SolidWorksService _swService;
    private readonly SolidWorksConfiguration _config;

    public PatternService(
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<PatternService> logger)
    {
        _swService = swService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Create a linear pattern of the selected feature
    /// </summary>
    public async Task<bool> CreateLinearPatternAsync(
        int count1, Dimension spacing1,
        int count2 = 0, Dimension? spacing2 = null)
    {
        _logger.LogInformation("Creating linear pattern: {Count1} x {Count2}, spacing {Spacing1}",
            count1, count2, spacing1);

        if (_config.UseMock)
        {
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                var featMgr = model.FeatureManager;

                // Get direction vectors (X and Y by default)
                double d1X = 1, d1Y = 0, d1Z = 0;  // X direction
                double d2X = 0, d2Y = 1, d2Z = 0;  // Y direction

                var spacing1Meters = spacing1.Meters;
                var spacing2Meters = spacing2?.Meters ?? 0;

                // FeatureLinearPattern4
                var feature = featMgr.FeatureLinearPattern4(
                    count1,             // D1TotalInstances
                    spacing1Meters,     // D1Spacing
                    count2 > 0 ? count2 : 1,  // D2TotalInstances
                    spacing2Meters,     // D2Spacing
                    true,               // D1ReverseDirection
                    false,              // D2ReverseDirection
                    d1X, d1Y, d1Z,      // D1 direction
                    d2X, d2Y, d2Z,      // D2 direction
                    false,              // GeometryPattern
                    true,               // PatternSeedOnly
                    false,              // UseTransform
                    0,                  // NumSkipInstances
                    null,               // SkipInstances
                    false               // Vary
                );

                if (feature != null)
                {
                    _logger.LogInformation("Linear pattern created successfully");
                    return true;
                }

                _logger.LogWarning("Linear pattern creation returned null");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create linear pattern");
                return false;
            }
        });
    }

    /// <summary>
    /// Create a circular pattern of the selected feature
    /// </summary>
    public async Task<bool> CreateCircularPatternAsync(
        int count, double totalAngle = 360.0, bool equalSpacing = true)
    {
        _logger.LogInformation("Creating circular pattern: {Count} instances over {Angle}°",
            count, totalAngle);

        if (_config.UseMock)
        {
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                var featMgr = model.FeatureManager;

                // Convert angle to radians
                var angleRad = totalAngle * Math.PI / 180.0;
                var spacing = equalSpacing ? angleRad / count : angleRad;

                // FeatureCircularPattern4
                var feature = featMgr.FeatureCircularPattern4(
                    count,              // Number of instances
                    spacing,            // Spacing (radians)
                    true,               // ReverseDirection
                    null,               // Axis (null = auto select)
                    false,              // GeometryPattern
                    true,               // EqualSpacing
                    false               // Vary
                );

                if (feature != null)
                {
                    _logger.LogInformation("Circular pattern created successfully");
                    return true;
                }

                _logger.LogWarning("Circular pattern creation returned null");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create circular pattern");
                return false;
            }
        });
    }

    /// <summary>
    /// Mirror selected features about a plane
    /// </summary>
    public async Task<bool> CreateMirrorAsync(Core.Models.Geometry.ReferencePlane plane)
    {
        _logger.LogInformation("Creating mirror about {Plane}", plane);

        if (_config.UseMock)
        {
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                // Select the mirror plane
                var planeName = Core.Models.Geometry.Plane.GetSolidWorksName(plane);
                model.Extension.SelectByID2(planeName, "PLANE", 0, 0, 0, true, 0, null, 0);

                var featMgr = model.FeatureManager;

                // InsertMirrorFeature2
                var feature = featMgr.InsertMirrorFeature2(
                    true,   // MirrorBody
                    false,  // GeometryPattern
                    true,   // Merge
                    false,  // KnittedSurface
                    0       // FeatureScope
                );

                if (feature != null)
                {
                    _logger.LogInformation("Mirror created successfully");
                    return true;
                }

                _logger.LogWarning("Mirror creation returned null");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create mirror");
                return false;
            }
        });
    }
}
