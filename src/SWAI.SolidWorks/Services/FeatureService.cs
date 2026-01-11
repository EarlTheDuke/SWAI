using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Features;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Service for feature operations
/// </summary>
public class FeatureService : IFeatureService
{
    private readonly ILogger<FeatureService> _logger;
    private readonly SolidWorksService _swService;
    private readonly SolidWorksConfiguration _config;

    public FeatureService(
        SolidWorksService swService,
        SolidWorksConfiguration config,
        ILogger<FeatureService> logger)
    {
        _swService = swService;
        _config = config;
        _logger = logger;
    }

    public async Task<ExtrusionFeature> CreateExtrusionAsync(
        string name,
        Dimension depth,
        ExtrusionDirection direction = ExtrusionDirection.SingleDirection)
    {
        _logger.LogInformation("Creating extrusion '{Name}': {Depth}", name, depth);

        var feature = new ExtrusionFeature(name, null!, depth)
        {
            Direction = direction
        };

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                var model = swApp.ActiveDoc;
                if (model == null) return;

                var featMgr = model.FeatureManager;

                // FeatureExtrusion3 parameters:
                // Sd (single direction), Flip, Dir2, T1 (end condition), T2, D1 (depth), D2
                // Draft1, DraftAngle1, Draft2, DraftAngle2, DirDraft1, DirDraft2
                // OffsetReverse1, OffsetReverse2, TranslateDir1, TranslateDir2
                // Merge, UseFeatScope, UseAutoSelect, T0, T2EndCondition
                // AssemblyContext (for assembly features)

                var isMidPlane = direction == ExtrusionDirection.MidPlane;
                var depth1 = isMidPlane ? depth.Meters / 2 : depth.Meters;

                featMgr.FeatureExtrusion3(
                    !isMidPlane,    // Single direction (false for mid-plane)
                    false,          // Flip direction
                    isMidPlane,     // Dir2 (enable for mid-plane)
                    0,              // T1 = swEndCondBlind
                    0,              // T2 = swEndCondBlind
                    depth1,         // D1 = depth in meters
                    depth1,         // D2 = depth for other direction
                    false,          // Draft1
                    false,          // Draft2
                    false,          // DraftDir1
                    false,          // DraftDir2
                    0,              // DraftAngle1
                    0,              // DraftAngle2
                    false,          // OffsetReverse1
                    false,          // OffsetReverse2
                    false,          // TranslateSurface1
                    false,          // TranslateSurface2
                    true,           // Merge
                    true,           // UseFeatScope
                    true,           // UseAutoSelect
                    0,              // T0
                    0,              // T2 end condition
                    false           // NormalCut
                );

                _logger.LogInformation("Extrusion created in SolidWorks");
            });
        }

        return feature;
    }

    public async Task<CutExtrusionFeature> CreateCutExtrusionAsync(
        string name,
        Dimension depth,
        ExtrusionDirection direction = ExtrusionDirection.SingleDirection)
    {
        _logger.LogInformation("Creating cut extrusion '{Name}': {Depth}", name, depth);

        var feature = new CutExtrusionFeature(name, null!, depth)
        {
            Direction = direction
        };

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                var model = swApp.ActiveDoc;
                if (model == null) return;

                var featMgr = model.FeatureManager;

                var isMidPlane = direction == ExtrusionDirection.MidPlane;
                var depthMeters = isMidPlane ? depth.Meters / 2 : depth.Meters;

                // FeatureCut4 for cut extrusion
                featMgr.FeatureCut4(
                    !isMidPlane,    // Single direction
                    false,          // Flip
                    isMidPlane,     // Dir2
                    0,              // T1 = swEndCondBlind
                    0,              // T2
                    depthMeters,    // D1
                    depthMeters,    // D2
                    false, false,   // Draft1, Draft2
                    false, false,   // DraftDir1, DraftDir2
                    0, 0,           // DraftAngle1, DraftAngle2
                    false, false,   // OffsetReverse1, OffsetReverse2
                    false, false,   // TranslateSurface1, TranslateSurface2
                    false,          // NormalCut
                    false,          // UseFeatScope
                    false,          // UseAutoSelect
                    false,          // AssemblyFeatureScope
                    false,          // AutoSelectComponents
                    false,          // PropagateFeatureToParts
                    0,              // T0
                    0,              // StartFromType
                    false,          // FlipStartFromDir
                    0               // StartFromOffset
                );

                _logger.LogInformation("Cut extrusion created in SolidWorks");
            });
        }

        return feature;
    }

    public async Task<FilletFeature> CreateFilletAsync(string name, Dimension radius, bool allEdges = false)
    {
        _logger.LogInformation("Creating fillet '{Name}': R={Radius}, AllEdges={All}", name, radius, allEdges);

        var feature = new FilletFeature(name, radius)
        {
            ApplyToAllEdges = allEdges
        };

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                var model = swApp.ActiveDoc;
                if (model == null) return;

                var featMgr = model.FeatureManager;

                if (allEdges)
                {
                    // Select all edges - this is complex in real SW
                    // For now, create simple fillet on selected edges
                }

                // SimpleFilet2 for basic constant radius fillet
                featMgr.FeatureFillet3(
                    195,                // Options: swFeatureFilletOptions_e (constant radius)
                    radius.Meters,      // R1 radius
                    0,                  // Fillet type
                    0,                  // Overflow type
                    0, 0, 0,           // Radii arrays (not used for simple)
                    0, 0, 0,           // Set back arrays
                    0, 0, 0            // Point arrays
                );

                _logger.LogInformation("Fillet created in SolidWorks");
            });
        }

        return feature;
    }

    public async Task<ChamferFeature> CreateChamferAsync(string name, Dimension distance, bool allEdges = false)
    {
        _logger.LogInformation("Creating chamfer '{Name}': D={Distance}", name, distance);

        var feature = new ChamferFeature(name, distance);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                var model = swApp.ActiveDoc;
                if (model == null) return;

                var featMgr = model.FeatureManager;

                // InsertFeatureChamfer
                featMgr.InsertFeatureChamfer(
                    4,                  // Options
                    0,                  // ChamferType: Distance-Distance
                    distance.Meters,    // Width
                    0,                  // Angle (not used)
                    distance.Meters,    // OtherDist
                    0,                  // VertexChamDist
                    0,                  // VertexChamDist
                    0                   // VertexChamDist
                );

                _logger.LogInformation("Chamfer created in SolidWorks");
            });
        }

        return feature;
    }

    public async Task<HoleFeature> CreateHoleAsync(
        string name,
        Point3D location,
        Dimension diameter,
        Dimension depth)
    {
        _logger.LogInformation("Creating hole '{Name}': D={Diameter} at {Location}", name, diameter, location);

        var feature = new HoleFeature(name, diameter, depth);

        if (!_config.UseMock)
        {
            await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return;

                var model = swApp.ActiveDoc;
                if (model == null) return;

                var featMgr = model.FeatureManager;

                // For simple hole, we'd typically:
                // 1. Create a sketch with circle at location
                // 2. Cut-extrude through
                // 
                // Alternatively use HoleWizard for more complex holes

                // Simple approach: assume sketch is already active with circle
                featMgr.FeatureCut4(
                    true,           // Single direction
                    false,          // Flip
                    false,          // Dir2
                    0,              // T1 = Blind
                    0,              // T2
                    depth.Meters,   // D1
                    0,              // D2
                    false, false,
                    false, false,
                    0, 0,
                    false, false,
                    false, false,
                    false, false, false, false, false, false,
                    0, 0, false, 0
                );

                _logger.LogInformation("Hole created in SolidWorks");
            });
        }

        return feature;
    }
}
