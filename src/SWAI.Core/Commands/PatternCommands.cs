using SWAI.Core.Models.Geometry;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Commands;

/// <summary>
/// Pattern type
/// </summary>
public enum PatternType
{
    Linear,
    Circular
}

/// <summary>
/// Direction for linear pattern
/// </summary>
public enum PatternDirection
{
    X,
    Y,
    Z,
    Custom
}

/// <summary>
/// Command to create a linear pattern
/// </summary>
public class AddLinearPatternCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    
    /// <summary>
    /// Number of instances in direction 1
    /// </summary>
    public int Count1 { get; init; }
    
    /// <summary>
    /// Spacing between instances in direction 1
    /// </summary>
    public Dimension Spacing1 { get; init; }
    
    /// <summary>
    /// Direction for pattern (X, Y, or custom vector)
    /// </summary>
    public PatternDirection Direction1 { get; init; } = PatternDirection.X;
    
    /// <summary>
    /// Number of instances in direction 2 (0 = single direction)
    /// </summary>
    public int Count2 { get; init; } = 0;
    
    /// <summary>
    /// Spacing in direction 2
    /// </summary>
    public Dimension? Spacing2 { get; init; }
    
    /// <summary>
    /// Direction for second pattern direction
    /// </summary>
    public PatternDirection Direction2 { get; init; } = PatternDirection.Y;

    public AddLinearPatternCommand(string featureName, int count, Dimension spacing)
    {
        FeatureName = featureName;
        Count1 = count;
        Spacing1 = spacing;
    }

    public override string CommandType => "LinearPattern";
    public override string Description => Count2 > 0
        ? $"Linear pattern: {Count1}x{Count2} instances, {Spacing1} spacing"
        : $"Linear pattern: {Count1} instances, {Spacing1} spacing";
}

/// <summary>
/// Command to create a circular pattern
/// </summary>
public class AddCircularPatternCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    
    /// <summary>
    /// Number of instances around the circle
    /// </summary>
    public int Count { get; init; }
    
    /// <summary>
    /// Total angle to span (360 for full circle)
    /// </summary>
    public double TotalAngle { get; init; } = 360.0;
    
    /// <summary>
    /// Whether to space evenly (ignore angle between)
    /// </summary>
    public bool EqualSpacing { get; init; } = true;
    
    /// <summary>
    /// Axis of rotation (reference)
    /// </summary>
    public string? AxisReference { get; init; }
    
    /// <summary>
    /// Center point for pattern
    /// </summary>
    public Point3D? Center { get; init; }

    public AddCircularPatternCommand(string featureName, int count)
    {
        FeatureName = featureName;
        Count = count;
    }

    public override string CommandType => "CircularPattern";
    public override string Description => TotalAngle < 360
        ? $"Circular pattern: {Count} instances over {TotalAngle}°"
        : $"Circular pattern: {Count} instances around full circle";
}

/// <summary>
/// Command to mirror features
/// </summary>
public class AddMirrorCommand : SwaiCommandBase
{
    public string FeatureName { get; init; }
    
    /// <summary>
    /// Reference plane or face for mirroring
    /// </summary>
    public ReferencePlane MirrorPlane { get; init; } = ReferencePlane.Right;
    
    /// <summary>
    /// List of feature names to mirror
    /// </summary>
    public List<string> FeaturesToMirror { get; init; } = new();

    public AddMirrorCommand(string featureName, ReferencePlane plane)
    {
        FeatureName = featureName;
        MirrorPlane = plane;
    }

    public override string CommandType => "Mirror";
    public override string Description => $"Mirror about {MirrorPlane}";
}
