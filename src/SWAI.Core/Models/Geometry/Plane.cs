namespace SWAI.Core.Models.Geometry;

/// <summary>
/// Standard reference planes in SolidWorks
/// </summary>
public enum ReferencePlane
{
    /// <summary>
    /// Front plane (XY plane at Z=0)
    /// </summary>
    Front,

    /// <summary>
    /// Top plane (XZ plane at Y=0)
    /// </summary>
    Top,

    /// <summary>
    /// Right plane (YZ plane at X=0)
    /// </summary>
    Right
}

/// <summary>
/// Represents a plane in 3D space
/// </summary>
public class Plane
{
    /// <summary>
    /// A point on the plane (origin)
    /// </summary>
    public Point3D Origin { get; }

    /// <summary>
    /// Normal vector of the plane
    /// </summary>
    public Vector3D Normal { get; }

    /// <summary>
    /// Optional name for the plane
    /// </summary>
    public string? Name { get; }

    public Plane(Point3D origin, Vector3D normal, string? name = null)
    {
        Origin = origin;
        Normal = normal;
        Name = name;
    }

    /// <summary>
    /// Create a plane from a reference plane type
    /// </summary>
    public static Plane FromReference(ReferencePlane reference) => reference switch
    {
        ReferencePlane.Front => new Plane(Point3D.Origin, Vector3D.UnitZ, "Front Plane"),
        ReferencePlane.Top => new Plane(Point3D.Origin, Vector3D.UnitY, "Top Plane"),
        ReferencePlane.Right => new Plane(Point3D.Origin, Vector3D.UnitX, "Right Plane"),
        _ => throw new ArgumentOutOfRangeException(nameof(reference))
    };

    /// <summary>
    /// Get the SolidWorks plane name
    /// </summary>
    public static string GetSolidWorksName(ReferencePlane reference) => reference switch
    {
        ReferencePlane.Front => "Front Plane",
        ReferencePlane.Top => "Top Plane",
        ReferencePlane.Right => "Right Plane",
        _ => throw new ArgumentOutOfRangeException(nameof(reference))
    };

    public override string ToString() => Name ?? $"Plane at {Origin} with normal {Normal}";
}
