using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Standard hole types
/// </summary>
public enum HoleType
{
    Simple,
    Counterbore,
    Countersink,
    Tapped,
    PipeTapped,
    Legacy
}

/// <summary>
/// Hole standard (ANSI, ISO, etc.)
/// </summary>
public enum HoleStandard
{
    ANSI_Inch,
    ANSI_Metric,
    ISO,
    DIN,
    JIS,
    Custom
}

/// <summary>
/// Service for creating holes using Hole Wizard
/// </summary>
public class HoleWizardService
{
    private readonly ILogger<HoleWizardService> _logger;
    private readonly SolidWorksService _swService;
    private readonly SolidWorksConfiguration _config;

    public HoleWizardService(
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<HoleWizardService> logger)
    {
        _swService = swService;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Create a simple hole at the specified location
    /// </summary>
    public async Task<bool> CreateSimpleHoleAsync(
        Point3D location,
        Dimension diameter,
        Dimension depth,
        bool throughAll = false)
    {
        _logger.LogInformation("Creating simple hole: D={Diameter}, Depth={Depth}, Through={Through}",
            diameter, depth, throughAll);

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

                // Create a point at the location for the hole
                // First, need to be in sketch mode on a face or create a 3D sketch point

                var (x, y, z) = location.ToMeters();
                var diam = diameter.Meters;
                var dep = depth.Meters;

                // End type: 0 = Blind, 1 = Through All
                var endType = throughAll ? 1 : 0;

                // HoleWizard5
                var feature = featMgr.HoleWizard5(
                    0,      // HoleType: 0 = Simple
                    0,      // Standard: 0 = ANSI Inch
                    0,      // FastenerType
                    "",     // Size string (empty for custom)
                    endType,// End condition
                    diam,   // Diameter
                    dep,    // Depth
                    0,      // HeadDiameter (not used for simple)
                    0,      // HeadDepth (not used for simple)
                    0,      // TipAngle
                    false,  // NearSideCB
                    false,  // FarSideCB
                    0,      // NearCBDia
                    0,      // NearCBDepth
                    0,      // FarCBDia  
                    0,      // FarCBDepth
                    0,      // ThreadClass
                    false,  // TapDrillDia
                    "",     // TapDrillDiaStr
                    false,  // ThreadDisplay
                    false   // Cosmetic
                );

                if (feature != null)
                {
                    _logger.LogInformation("Hole created successfully");
                    return true;
                }

                _logger.LogWarning("Hole creation returned null");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create hole");
                return false;
            }
        });
    }

    /// <summary>
    /// Create a counterbore hole
    /// </summary>
    public async Task<bool> CreateCounterboreHoleAsync(
        Point3D location,
        Dimension holeDiameter,
        Dimension holeDepth,
        Dimension cbDiameter,
        Dimension cbDepth,
        bool throughAll = false)
    {
        _logger.LogInformation("Creating counterbore hole: Hole D={HoleDia}, CB D={CBDia}",
            holeDiameter, cbDiameter);

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

                var endType = throughAll ? 1 : 0;

                var feature = featMgr.HoleWizard5(
                    1,                      // HoleType: 1 = Counterbore
                    0,                      // Standard
                    0,                      // FastenerType
                    "",                     // Size
                    endType,                // End condition
                    holeDiameter.Meters,    // Hole diameter
                    holeDepth.Meters,       // Hole depth
                    cbDiameter.Meters,      // CB diameter
                    cbDepth.Meters,         // CB depth
                    0, false, false,
                    0, 0, 0, 0,
                    0, false, "", false, false
                );

                return feature != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create counterbore hole");
                return false;
            }
        });
    }

    /// <summary>
    /// Create a countersink hole
    /// </summary>
    public async Task<bool> CreateCountersinkHoleAsync(
        Point3D location,
        Dimension holeDiameter,
        Dimension holeDepth,
        Dimension csDiameter,
        double csAngle = 82.0,
        bool throughAll = false)
    {
        _logger.LogInformation("Creating countersink hole: Hole D={HoleDia}, CS D={CSDia}, Angle={Angle}",
            holeDiameter, csDiameter, csAngle);

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

                var endType = throughAll ? 1 : 0;
                var angleRad = csAngle * Math.PI / 180.0;

                var feature = featMgr.HoleWizard5(
                    2,                      // HoleType: 2 = Countersink
                    0,                      // Standard
                    0,                      // FastenerType
                    "",                     // Size
                    endType,                // End condition
                    holeDiameter.Meters,    // Hole diameter
                    holeDepth.Meters,       // Hole depth
                    csDiameter.Meters,      // CS diameter
                    0,                      // CS depth (calculated from angle)
                    angleRad,               // CS angle
                    false, false,
                    0, 0, 0, 0,
                    0, false, "", false, false
                );

                return feature != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create countersink hole");
                return false;
            }
        });
    }

    /// <summary>
    /// Create a tapped hole
    /// </summary>
    public async Task<bool> CreateTappedHoleAsync(
        Point3D location,
        string threadSize,  // e.g., "1/4-20", "M6x1.0"
        Dimension depth,
        bool throughAll = false)
    {
        _logger.LogInformation("Creating tapped hole: Thread={Thread}, Depth={Depth}",
            threadSize, depth);

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

                var endType = throughAll ? 1 : 0;

                var feature = featMgr.HoleWizard5(
                    3,              // HoleType: 3 = Tapped
                    0,              // Standard
                    0,              // FastenerType
                    threadSize,     // Thread size
                    endType,
                    0,              // Diameter (from thread)
                    depth.Meters,
                    0, 0, 0,
                    false, false,
                    0, 0, 0, 0,
                    0,              // ThreadClass
                    true,           // Use tap drill
                    "",
                    true,           // Show threads
                    false
                );

                return feature != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tapped hole");
                return false;
            }
        });
    }
}
