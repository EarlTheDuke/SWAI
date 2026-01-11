using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Service for mate operations in assemblies
/// </summary>
public class MateService : IMateService
{
    private readonly ILogger<MateService> _logger;
    private readonly SolidWorksService _swService;
    private readonly AssemblyService _assemblyService;
    private readonly SolidWorksConfiguration _config;

    private int _mateCounter = 0;

    public MateService(
        SolidWorksService swService,
        AssemblyService assemblyService,
        SolidWorksConfiguration config,
        ILogger<MateService> logger)
    {
        _swService = swService;
        _assemblyService = assemblyService;
        _config = config;
        _logger = logger;
    }

    public async Task<AssemblyMate?> AddMateAsync(
        MateType type,
        MateReference entity1,
        MateReference entity2,
        MateAlignment alignment = MateAlignment.Closest,
        Dimension? distance = null,
        double? angle = null)
    {
        var assembly = _assemblyService.ActiveAssembly;
        if (assembly == null)
        {
            _logger.LogWarning("No active assembly");
            return null;
        }

        var mateName = $"Mate{++_mateCounter}";
        _logger.LogInformation("Adding {Type} mate: {Name}", type, mateName);

        var mate = new AssemblyMate(mateName, type)
        {
            Entity1 = entity1,
            Entity2 = entity2,
            Alignment = alignment,
            Distance = distance,
            Angle = angle
        };

        if (!_config.UseMock)
        {
            var success = await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                // Select entities
                SelectEntity(model, entity1, 1);
                SelectEntity(model, entity2, 2);

                // Get the assembly doc
                var assemblyDoc = model; // In real code: (AssemblyDoc)model

                // Map mate type to SolidWorks constant
                var swMateType = MapMateType(type);
                var swAlignment = MapAlignment(alignment);

                // AddMate5 for standard mates
                var mateFeature = assemblyDoc.AddMate5(
                    swMateType,
                    swAlignment,
                    false,  // Flip
                    distance?.Meters ?? 0,
                    distance?.Meters ?? 0,  // Min (for limit mates)
                    distance?.Meters ?? 0,  // Max
                    angle ?? 0,
                    angle ?? 0,
                    angle ?? 0,
                    0,  // ForPositioningOnly
                    0,  // LockRotation
                    out int errorStatus
                );

                if (errorStatus != 0)
                {
                    _logger.LogWarning("Mate creation returned error: {Error}", errorStatus);
                    return false;
                }

                model.ClearSelection2(true);
                return mateFeature != null;
            });

            if (!success)
            {
                return null;
            }
        }

        assembly.AddMate(mate);
        return mate;
    }

    public async Task<AssemblyMate?> AddCoincidentMateAsync(
        string comp1, string face1,
        string comp2, string face2,
        MateAlignment alignment = MateAlignment.Closest)
    {
        return await AddMateAsync(
            MateType.Coincident,
            MateReference.Face(comp1, face1),
            MateReference.Face(comp2, face2),
            alignment
        );
    }

    public async Task<AssemblyMate?> AddConcentricMateAsync(
        string comp1, string cylindricalFace1,
        string comp2, string cylindricalFace2)
    {
        return await AddMateAsync(
            MateType.Concentric,
            MateReference.Face(comp1, cylindricalFace1),
            MateReference.Face(comp2, cylindricalFace2)
        );
    }

    public async Task<AssemblyMate?> AddDistanceMateAsync(
        string comp1, string face1,
        string comp2, string face2,
        Dimension distance)
    {
        return await AddMateAsync(
            MateType.Distance,
            MateReference.Face(comp1, face1),
            MateReference.Face(comp2, face2),
            MateAlignment.Closest,
            distance
        );
    }

    public async Task<AssemblyMate?> AddAngleMateAsync(
        string comp1, string face1,
        string comp2, string face2,
        double angleDegrees)
    {
        return await AddMateAsync(
            MateType.Angle,
            MateReference.Face(comp1, face1),
            MateReference.Face(comp2, face2),
            MateAlignment.Closest,
            null,
            angleDegrees
        );
    }

    public async Task<AssemblyMate?> AddParallelMateAsync(
        string comp1, string face1,
        string comp2, string face2)
    {
        return await AddMateAsync(
            MateType.Parallel,
            MateReference.Face(comp1, face1),
            MateReference.Face(comp2, face2)
        );
    }

    public async Task<AssemblyMate?> AddPerpendicularMateAsync(
        string comp1, string face1,
        string comp2, string face2)
    {
        return await AddMateAsync(
            MateType.Perpendicular,
            MateReference.Face(comp1, face1),
            MateReference.Face(comp2, face2)
        );
    }

    public async Task<bool> SuppressMateAsync(string mateName, bool suppress = true)
    {
        _logger.LogInformation("{Action} mate: {Name}", suppress ? "Suppressing" : "Unsuppressing", mateName);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                model.Extension.SelectByID2(mateName, "MATE", 0, 0, 0, false, 0, null, 0);

                if (suppress)
                    model.EditSuppress2();
                else
                    model.EditUnsuppress2();

                return true;
            });
        }

        var assembly = _assemblyService.ActiveAssembly;
        var mate = assembly?.Mates.FirstOrDefault(m => m.Name == mateName);
        if (mate != null)
        {
            mate.IsSuppressed = suppress;
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteMateAsync(string mateName)
    {
        _logger.LogInformation("Deleting mate: {Name}", mateName);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                model.Extension.SelectByID2(mateName, "MATE", 0, 0, 0, false, 0, null, 0);
                return model.EditDelete();
            });
        }

        var assembly = _assemblyService.ActiveAssembly;
        if (assembly != null)
        {
            var mate = assembly.Mates.FirstOrDefault(m => m.Name == mateName);
            if (mate != null)
            {
                assembly.Mates.Remove(mate);
                return true;
            }
        }

        return false;
    }

    public async Task<List<AssemblyMate>> GetMatesAsync()
    {
        var assembly = _assemblyService.ActiveAssembly;
        if (assembly == null)
            return new List<AssemblyMate>();

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var mates = new List<AssemblyMate>();
                // Would enumerate through assembly mates from SolidWorks
                return mates;
            });
        }

        return assembly.Mates.ToList();
    }

    public async Task<bool> EditMateValueAsync(string mateName, Dimension? distance = null, double? angle = null)
    {
        _logger.LogInformation("Editing mate {Name}: Distance={Distance}, Angle={Angle}",
            mateName, distance, angle);

        if (!_config.UseMock)
        {
            return await Task.Run(() =>
            {
                var swApp = _swService.GetApplication();
                if (swApp == null) return false;

                var model = swApp.ActiveDoc;
                if (model == null) return false;

                // Would select the mate and modify its dimension/angle
                // via IDisplayDimension or mate properties

                return true;
            });
        }

        var assembly = _assemblyService.ActiveAssembly;
        var mate = assembly?.Mates.FirstOrDefault(m => m.Name == mateName);
        if (mate != null)
        {
            if (distance != null) mate.Distance = distance;
            if (angle != null) mate.Angle = angle;
            return true;
        }

        return false;
    }

    #region Helper Methods

    private void SelectEntity(dynamic model, MateReference reference, int mark)
    {
        // Build selection string based on entity type
        var selectionType = reference.EntityType switch
        {
            "Face" => "FACE",
            "Edge" => "EDGE",
            "Plane" => "PLANE",
            "Axis" => "AXIS",
            "Point" or "Origin" => "POINT",
            _ => "FACE"
        };

        // In real implementation, would need to find the actual entity
        // by traversing the component's faces/edges
        var selectionPath = $"{reference.EntityName}@{reference.ComponentName}";

        model.Extension.SelectByID2(
            selectionPath,
            selectionType,
            0, 0, 0,
            true,  // Append to selection
            mark,  // Selection mark
            null,
            0
        );
    }

    private int MapMateType(MateType type)
    {
        // swMateType_e constants
        return type switch
        {
            MateType.Coincident => 0,    // swMateCOINCIDENT
            MateType.Concentric => 1,    // swMateCONCENTRIC
            MateType.Perpendicular => 2, // swMatePERPENDICULAR
            MateType.Parallel => 3,      // swMatePARALLEL
            MateType.Tangent => 4,       // swMateTANGENT
            MateType.Distance => 5,      // swMateDISTANCE
            MateType.Angle => 6,         // swMateANGLE
            MateType.Lock => 7,          // swMateLOCK
            MateType.Width => 11,        // swMateWIDTH
            MateType.Symmetric => 8,     // swMateSYMMETRIC
            MateType.Cam => 9,           // swMateCAMFOLLOWER
            MateType.Gear => 10,         // swMateGEAR
            MateType.RackPinion => 12,   // swMateRACKPINION
            MateType.Screw => 13,        // swMateSCREW
            MateType.Path => 14,         // swMatePATH
            _ => 0
        };
    }

    private int MapAlignment(MateAlignment alignment)
    {
        // swMateAlign_e constants
        return alignment switch
        {
            MateAlignment.Aligned => 0,     // swMateAlignALIGNED
            MateAlignment.AntiAligned => 1, // swMateAlignANTI_ALIGNED
            MateAlignment.Closest => 2,     // swMateAlignCLOSEST
            _ => 2
        };
    }

    #endregion
}
