using SWAI.Core.Commands;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Features;
using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Sketch;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Interfaces;

/// <summary>
/// Connection status to SolidWorks
/// </summary>
public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error
}

/// <summary>
/// SolidWorks application information
/// </summary>
public record SolidWorksInfo(
    string Version,
    int RevisionNumber,
    bool IsRunning,
    string InstallPath
);

/// <summary>
/// Main interface for SolidWorks operations
/// </summary>
public interface ISolidWorksService
{
    /// <summary>
    /// Current connection status
    /// </summary>
    ConnectionStatus Status { get; }

    /// <summary>
    /// Event raised when connection status changes
    /// </summary>
    event EventHandler<ConnectionStatus>? StatusChanged;

    /// <summary>
    /// Connect to SolidWorks (starts it if not running)
    /// </summary>
    Task<bool> ConnectAsync(bool startIfNotRunning = true);

    /// <summary>
    /// Disconnect from SolidWorks
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Get information about the connected SolidWorks instance
    /// </summary>
    Task<SolidWorksInfo?> GetInfoAsync();

    /// <summary>
    /// Check if SolidWorks is currently running
    /// </summary>
    Task<bool> IsRunningAsync();
}

/// <summary>
/// Interface for part document operations
/// </summary>
public interface IPartService
{
    /// <summary>
    /// The currently active part document
    /// </summary>
    PartDocument? ActivePart { get; }

    /// <summary>
    /// Event raised when active part changes
    /// </summary>
    event EventHandler<PartDocument?>? ActivePartChanged;

    /// <summary>
    /// Create a new part document
    /// </summary>
    Task<PartDocument> CreatePartAsync(string name, UnitSystem units = UnitSystem.Inches);

    /// <summary>
    /// Open an existing part
    /// </summary>
    Task<PartDocument?> OpenPartAsync(string filePath);

    /// <summary>
    /// Save the current part
    /// </summary>
    Task<bool> SavePartAsync(string? filePath = null);

    /// <summary>
    /// Export the current part to a different format
    /// </summary>
    Task<bool> ExportPartAsync(string filePath, ExportFormat format);

    /// <summary>
    /// Close the current part
    /// </summary>
    Task<bool> ClosePartAsync(bool saveFirst = false);

    /// <summary>
    /// Rebuild the part (regenerate all features)
    /// </summary>
    Task<bool> RebuildAsync();
}

/// <summary>
/// Interface for sketch operations
/// </summary>
public interface ISketchService
{
    /// <summary>
    /// Create a new sketch on a plane
    /// </summary>
    Task<SketchProfile> CreateSketchAsync(string name, ReferencePlane plane);

    /// <summary>
    /// Enter sketch editing mode
    /// </summary>
    Task<bool> EditSketchAsync(SketchProfile sketch);

    /// <summary>
    /// Exit sketch editing mode
    /// </summary>
    Task<bool> CloseSketchAsync();

    /// <summary>
    /// Add a rectangle to the current sketch
    /// </summary>
    Task<SketchRectangle> AddRectangleAsync(Point3D corner1, Point3D corner2);

    /// <summary>
    /// Add a centered rectangle
    /// </summary>
    Task<SketchRectangle> AddCenteredRectangleAsync(Point3D center, Dimension width, Dimension height);

    /// <summary>
    /// Add a circle to the current sketch
    /// </summary>
    Task<SketchCircle> AddCircleAsync(Point3D center, Dimension radius);

    /// <summary>
    /// Add a line to the current sketch
    /// </summary>
    Task<SketchLine> AddLineAsync(Point3D start, Point3D end);
}

/// <summary>
/// Interface for feature operations
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// Create an extrusion from the last sketch
    /// </summary>
    Task<ExtrusionFeature> CreateExtrusionAsync(
        string name,
        Dimension depth,
        ExtrusionDirection direction = ExtrusionDirection.SingleDirection
    );

    /// <summary>
    /// Create a cut extrusion from the last sketch
    /// </summary>
    Task<CutExtrusionFeature> CreateCutExtrusionAsync(
        string name,
        Dimension depth,
        ExtrusionDirection direction = ExtrusionDirection.SingleDirection
    );

    /// <summary>
    /// Add a fillet to edges
    /// </summary>
    Task<FilletFeature> CreateFilletAsync(string name, Dimension radius, bool allEdges = false);

    /// <summary>
    /// Add a chamfer to edges
    /// </summary>
    Task<ChamferFeature> CreateChamferAsync(string name, Dimension distance, bool allEdges = false);

    /// <summary>
    /// Create a simple hole
    /// </summary>
    Task<HoleFeature> CreateHoleAsync(string name, Point3D location, Dimension diameter, Dimension depth);
}

/// <summary>
/// Interface for command execution
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Execute a command
    /// </summary>
    Task<CommandResult> ExecuteAsync(ISwaiCommand command);

    /// <summary>
    /// Undo the last command
    /// </summary>
    Task<bool> UndoAsync();

    /// <summary>
    /// Redo the last undone command
    /// </summary>
    Task<bool> RedoAsync();

    /// <summary>
    /// Get command history
    /// </summary>
    IReadOnlyList<ISwaiCommand> History { get; }

    /// <summary>
    /// Whether undo is available
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Whether redo is available
    /// </summary>
    bool CanRedo { get; }
}
