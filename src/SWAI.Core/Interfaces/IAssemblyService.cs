using SWAI.Core.Commands;
using SWAI.Core.Models.Assembly;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Interfaces;

/// <summary>
/// Interface for assembly operations
/// </summary>
public interface IAssemblyService
{
    /// <summary>
    /// The currently active assembly
    /// </summary>
    AssemblyDocument? ActiveAssembly { get; }

    /// <summary>
    /// Event raised when active assembly changes
    /// </summary>
    event EventHandler<AssemblyDocument?>? ActiveAssemblyChanged;

    /// <summary>
    /// Create a new assembly
    /// </summary>
    Task<AssemblyDocument> CreateAssemblyAsync(string name, UnitSystem units = UnitSystem.Inches);

    /// <summary>
    /// Open an existing assembly
    /// </summary>
    Task<AssemblyDocument?> OpenAssemblyAsync(string filePath);

    /// <summary>
    /// Save the current assembly
    /// </summary>
    Task<bool> SaveAssemblyAsync(string? filePath = null, bool saveComponents = false);

    /// <summary>
    /// Close the current assembly
    /// </summary>
    Task<bool> CloseAssemblyAsync(bool saveFirst = false);

    /// <summary>
    /// Insert a component into the assembly
    /// </summary>
    Task<AssemblyComponent?> InsertComponentAsync(
        string partPath,
        Point3D? position = null,
        bool isFixed = false,
        string? configuration = null);

    /// <summary>
    /// Insert multiple instances of a component
    /// </summary>
    Task<List<AssemblyComponent>> InsertComponentsAsync(
        string partPath,
        int count,
        Dimension spacing,
        PatternDirection direction = PatternDirection.X);

    /// <summary>
    /// Move a component to a new position
    /// </summary>
    Task<bool> MoveComponentAsync(string componentName, Point3D newPosition);

    /// <summary>
    /// Move a component by an offset
    /// </summary>
    Task<bool> MoveComponentByAsync(string componentName, Vector3D offset);

    /// <summary>
    /// Rotate a component
    /// </summary>
    Task<bool> RotateComponentAsync(string componentName, double angleX, double angleY, double angleZ);

    /// <summary>
    /// Fix a component in place
    /// </summary>
    Task<bool> FixComponentAsync(string componentName, bool fix = true);

    /// <summary>
    /// Suppress a component
    /// </summary>
    Task<bool> SuppressComponentAsync(string componentName, bool suppress = true);

    /// <summary>
    /// Delete a component
    /// </summary>
    Task<bool> DeleteComponentAsync(string componentName);

    /// <summary>
    /// Get list of all components
    /// </summary>
    Task<List<AssemblyComponent>> GetComponentsAsync();

    /// <summary>
    /// Rebuild the assembly
    /// </summary>
    Task<bool> RebuildAsync();
}

/// <summary>
/// Interface for mate operations
/// </summary>
public interface IMateService
{
    /// <summary>
    /// Add a mate between two entities
    /// </summary>
    Task<AssemblyMate?> AddMateAsync(
        MateType type,
        MateReference entity1,
        MateReference entity2,
        MateAlignment alignment = MateAlignment.Closest,
        Dimension? distance = null,
        double? angle = null);

    /// <summary>
    /// Add a coincident mate
    /// </summary>
    Task<AssemblyMate?> AddCoincidentMateAsync(
        string comp1, string face1,
        string comp2, string face2,
        MateAlignment alignment = MateAlignment.Closest);

    /// <summary>
    /// Add a concentric mate
    /// </summary>
    Task<AssemblyMate?> AddConcentricMateAsync(
        string comp1, string cylindricalFace1,
        string comp2, string cylindricalFace2);

    /// <summary>
    /// Add a distance mate
    /// </summary>
    Task<AssemblyMate?> AddDistanceMateAsync(
        string comp1, string face1,
        string comp2, string face2,
        Dimension distance);

    /// <summary>
    /// Add an angle mate
    /// </summary>
    Task<AssemblyMate?> AddAngleMateAsync(
        string comp1, string face1,
        string comp2, string face2,
        double angleDegrees);

    /// <summary>
    /// Add a parallel mate
    /// </summary>
    Task<AssemblyMate?> AddParallelMateAsync(
        string comp1, string face1,
        string comp2, string face2);

    /// <summary>
    /// Add a perpendicular mate
    /// </summary>
    Task<AssemblyMate?> AddPerpendicularMateAsync(
        string comp1, string face1,
        string comp2, string face2);

    /// <summary>
    /// Suppress a mate
    /// </summary>
    Task<bool> SuppressMateAsync(string mateName, bool suppress = true);

    /// <summary>
    /// Delete a mate
    /// </summary>
    Task<bool> DeleteMateAsync(string mateName);

    /// <summary>
    /// Get list of all mates
    /// </summary>
    Task<List<AssemblyMate>> GetMatesAsync();

    /// <summary>
    /// Edit a mate's value (distance, angle)
    /// </summary>
    Task<bool> EditMateValueAsync(string mateName, Dimension? distance = null, double? angle = null);
}
