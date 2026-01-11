using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Models.Sketch;

/// <summary>
/// Base class for all sketch entities
/// </summary>
public abstract class SketchEntity
{
    /// <summary>
    /// Unique identifier for this entity
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Optional name for this entity
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether this entity is construction geometry
    /// </summary>
    public bool IsConstruction { get; set; }
}

/// <summary>
/// A line segment in a sketch
/// </summary>
public class SketchLine : SketchEntity
{
    public Point3D StartPoint { get; }
    public Point3D EndPoint { get; }

    public SketchLine(Point3D start, Point3D end)
    {
        StartPoint = start;
        EndPoint = end;
    }

    /// <summary>
    /// Length of the line
    /// </summary>
    public Dimension Length => StartPoint.DistanceTo(EndPoint);

    public override string ToString() => $"Line: {StartPoint} to {EndPoint}";
}

/// <summary>
/// A rectangle in a sketch (defined by two corner points)
/// </summary>
public class SketchRectangle : SketchEntity
{
    public Point3D Corner1 { get; }
    public Point3D Corner2 { get; }

    public SketchRectangle(Point3D corner1, Point3D corner2)
    {
        Corner1 = corner1;
        Corner2 = corner2;
    }

    /// <summary>
    /// Create a rectangle centered at origin with given width and height
    /// </summary>
    public static SketchRectangle Centered(Dimension width, Dimension height, UnitSystem unit = UnitSystem.Inches)
    {
        var halfWidth = width / 2;
        var halfHeight = height / 2;

        return new SketchRectangle(
            new Point3D(-halfWidth.Value, -halfHeight.Value, 0, unit),
            new Point3D(halfWidth.Value, halfHeight.Value, 0, unit)
        );
    }

    /// <summary>
    /// Create a rectangle from origin corner with given width and height
    /// </summary>
    public static SketchRectangle FromOrigin(Dimension width, Dimension height, UnitSystem unit = UnitSystem.Inches)
    {
        return new SketchRectangle(
            Point3D.Origin,
            new Point3D(width.Value, height.Value, 0, unit)
        );
    }

    public Dimension Width => Dimension.MetersValue(Math.Abs(Corner2.X.Meters - Corner1.X.Meters));
    public Dimension Height => Dimension.MetersValue(Math.Abs(Corner2.Y.Meters - Corner1.Y.Meters));

    public override string ToString() => $"Rectangle: {Width} x {Height}";
}

/// <summary>
/// A circle in a sketch
/// </summary>
public class SketchCircle : SketchEntity
{
    public Point3D Center { get; }
    public Dimension Radius { get; }

    public SketchCircle(Point3D center, Dimension radius)
    {
        Center = center;
        Radius = radius;
    }

    /// <summary>
    /// Create a circle at origin
    /// </summary>
    public static SketchCircle AtOrigin(Dimension radius)
    {
        return new SketchCircle(Point3D.Origin, radius);
    }

    public Dimension Diameter => Radius * 2;

    public override string ToString() => $"Circle: R={Radius} at {Center}";
}

/// <summary>
/// An arc in a sketch
/// </summary>
public class SketchArc : SketchEntity
{
    public Point3D Center { get; }
    public Dimension Radius { get; }
    public double StartAngle { get; }  // In radians
    public double EndAngle { get; }    // In radians

    public SketchArc(Point3D center, Dimension radius, double startAngle, double endAngle)
    {
        Center = center;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    public override string ToString() => $"Arc: R={Radius} from {StartAngle:F2} to {EndAngle:F2} rad";
}
