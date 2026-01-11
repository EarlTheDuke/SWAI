using SWAI.Core.Models.Geometry;

namespace SWAI.Core.Models.Sketch;

/// <summary>
/// Represents a complete sketch profile (closed contour) that can be used for features
/// </summary>
public class SketchProfile
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Name of this sketch
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The plane this sketch is on
    /// </summary>
    public ReferencePlane Plane { get; }

    /// <summary>
    /// Entities in this sketch
    /// </summary>
    public List<SketchEntity> Entities { get; } = new();

    public SketchProfile(string name, ReferencePlane plane = ReferencePlane.Front)
    {
        Name = name;
        Plane = plane;
    }

    /// <summary>
    /// Add an entity to the sketch
    /// </summary>
    public SketchProfile AddEntity(SketchEntity entity)
    {
        Entities.Add(entity);
        return this;
    }

    /// <summary>
    /// Add a rectangle to the sketch
    /// </summary>
    public SketchProfile AddRectangle(SketchRectangle rect)
    {
        Entities.Add(rect);
        return this;
    }

    /// <summary>
    /// Add a circle to the sketch
    /// </summary>
    public SketchProfile AddCircle(SketchCircle circle)
    {
        Entities.Add(circle);
        return this;
    }

    /// <summary>
    /// Add a line to the sketch
    /// </summary>
    public SketchProfile AddLine(SketchLine line)
    {
        Entities.Add(line);
        return this;
    }

    public override string ToString() => $"Sketch '{Name}' on {Plane} with {Entities.Count} entities";
}
