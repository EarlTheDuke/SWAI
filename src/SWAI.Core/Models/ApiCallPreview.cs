namespace SWAI.Core.Models;

/// <summary>
/// Represents a preview of a SolidWorks API call
/// </summary>
public class ApiCallPreview
{
    /// <summary>
    /// The API method being called (e.g., "SketchManager.CreateCircleByRadius")
    /// </summary>
    public string ApiMethod { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this call does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The parameters being passed to the API
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>
    /// The raw API call as it would appear in code
    /// </summary>
    public string CodePreview { get; set; } = string.Empty;

    /// <summary>
    /// Link to SolidWorks API documentation for this method
    /// </summary>
    public string? DocumentationUrl { get; set; }

    /// <summary>
    /// Order of execution
    /// </summary>
    public int Order { get; set; }

    public override string ToString()
    {
        return $"[{Order}] {ApiMethod}: {Description}";
    }
}

/// <summary>
/// Collection of API calls for a complete operation
/// </summary>
public class ApiCallSequence
{
    public string OperationName { get; set; } = string.Empty;
    public string UserCommand { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<ApiCallPreview> Calls { get; set; } = new();
    
    /// <summary>
    /// Generate a complete code preview showing all API calls
    /// </summary>
    public string GenerateCodePreview()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"// SWAI Generated Code for: {UserCommand}");
        sb.AppendLine($"// Generated: {Timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        foreach (var call in Calls.OrderBy(c => c.Order))
        {
            sb.AppendLine($"// Step {call.Order}: {call.Description}");
            sb.AppendLine(call.CodePreview);
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}

/// <summary>
/// Static helper to generate API previews
/// </summary>
public static class ApiPreviewGenerator
{
    private const string API_BASE_URL = "https://help.solidworks.com/2025/english/api/sldworksapi/";

    public static ApiCallPreview CreateSketchPreview(string planeName)
    {
        return new ApiCallPreview
        {
            ApiMethod = "SketchManager.InsertSketch",
            Description = $"Create new sketch on {planeName}",
            Parameters = new Dictionary<string, string>
            {
                ["plane"] = planeName,
                ["autoCreate"] = "true"
            },
            CodePreview = $@"// Select the reference plane
swModel.Extension.SelectByID2(""{planeName}"", ""PLANE"", 0, 0, 0, false, 0, null, 0);
// Insert sketch on selected plane
swModel.SketchManager.InsertSketch(true);",
            DocumentationUrl = API_BASE_URL + "SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISketchManager~InsertSketch.html"
        };
    }

    public static ApiCallPreview CreateRectanglePreview(double x1, double y1, double x2, double y2, string unit)
    {
        // Convert to meters for API (SolidWorks uses meters internally)
        double scale = unit.ToLower() == "inches" ? 0.0254 : 1.0;
        double mx1 = x1 * scale, my1 = y1 * scale;
        double mx2 = x2 * scale, my2 = y2 * scale;

        return new ApiCallPreview
        {
            ApiMethod = "SketchManager.CreateCornerRectangle",
            Description = $"Draw rectangle from ({x1}, {y1}) to ({x2}, {y2}) {unit}",
            Parameters = new Dictionary<string, string>
            {
                ["x1"] = $"{mx1:F6} m ({x1} {unit})",
                ["y1"] = $"{my1:F6} m ({y1} {unit})",
                ["z1"] = "0 m",
                ["x2"] = $"{mx2:F6} m ({x2} {unit})",
                ["y2"] = $"{my2:F6} m ({y2} {unit})",
                ["z2"] = "0 m"
            },
            CodePreview = $@"// Create corner rectangle (coordinates in meters)
swModel.SketchManager.CreateCornerRectangle({mx1:F6}, {my1:F6}, 0, {mx2:F6}, {my2:F6}, 0);",
            DocumentationUrl = API_BASE_URL + "SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISketchManager~CreateCornerRectangle.html"
        };
    }

    public static ApiCallPreview CreateCirclePreview(double centerX, double centerY, double radius, string unit)
    {
        double scale = unit.ToLower() == "inches" ? 0.0254 : 1.0;
        double mcx = centerX * scale, mcy = centerY * scale, mr = radius * scale;

        return new ApiCallPreview
        {
            ApiMethod = "SketchManager.CreateCircleByRadius",
            Description = $"Draw circle at ({centerX}, {centerY}) with radius {radius} {unit}",
            Parameters = new Dictionary<string, string>
            {
                ["centerX"] = $"{mcx:F6} m ({centerX} {unit})",
                ["centerY"] = $"{mcy:F6} m ({centerY} {unit})",
                ["centerZ"] = "0 m",
                ["radius"] = $"{mr:F6} m ({radius} {unit})"
            },
            CodePreview = $@"// Create circle by center and radius (coordinates in meters)
swModel.SketchManager.CreateCircleByRadius({mcx:F6}, {mcy:F6}, 0, {mr:F6});",
            DocumentationUrl = API_BASE_URL + "SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.ISketchManager~CreateCircleByRadius.html"
        };
    }

    public static ApiCallPreview CreateExtrusionPreview(double depth, string unit, bool midPlane = false)
    {
        double scale = unit.ToLower() == "inches" ? 0.0254 : 1.0;
        double md = depth * scale;

        return new ApiCallPreview
        {
            ApiMethod = "FeatureManager.FeatureExtrusion3",
            Description = $"Extrude sketch {depth} {unit} {(midPlane ? "(mid-plane)" : "(blind)")}",
            Parameters = new Dictionary<string, string>
            {
                ["singleDirection"] = "true",
                ["depth"] = $"{md:F6} m ({depth} {unit})",
                ["endCondition"] = "swEndCondBlind (0)",
                ["flipDirection"] = "false"
            },
            CodePreview = $@"// Close the sketch first
swModel.SketchManager.InsertSketch(true);
// Create boss extrusion (depth in meters)
swModel.FeatureManager.FeatureExtrusion3(
    true,      // SingleDirection
    false,     // Flip
    false,     // Dir1Reverse  
    0,         // Dir1EndCondition (0 = Blind)
    0,         // Dir1EndCondition2
    {md:F6},   // Depth ({depth} {unit})
    0,         // Depth2
    false,     // DraftOutward
    false,     // DraftOutward2
    0,         // DraftAngle
    0,         // DraftAngle2
    false,     // OffsetReverse1
    false,     // OffsetReverse2
    0,         // TranslateSketch1
    0,         // TranslateSketch2
    false,     // MergeResult
    true,      // UseBothDirections
    0,         // Dir2EndCondition
    0,         // Dir2EndCondition2
    0,         // Dir2Depth
    0,         // Dir2Depth2
    false,     // Dir2Draft
    false,     // Dir2DraftOutward
    0,         // Dir2DraftAngle
    false,     // Dir2OffsetReverse
    0,         // Dir2TranslateSketch
    false,     // ThinFeature
    0, 0, 0,   // ThinWall parameters
    false,     // CapEnds
    false,     // CapThickness
    false,     // OptimizeGeometry
    0, 0       // Additional parameters
);",
            DocumentationUrl = API_BASE_URL + "SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFeatureManager~FeatureExtrusion3.html"
        };
    }

    public static ApiCallPreview CreateHolePreview(double diameter, string unit, bool throughAll = false)
    {
        double scale = unit.ToLower() == "inches" ? 0.0254 : 1.0;
        double md = diameter * scale;

        return new ApiCallPreview
        {
            ApiMethod = "FeatureManager.HoleWizard5",
            Description = $"Create {(throughAll ? "through" : "blind")} hole, diameter {diameter} {unit}",
            Parameters = new Dictionary<string, string>
            {
                ["holeType"] = "swWzdHole (0)",
                ["standardType"] = "swStandardAnsiMetric (0)",
                ["diameter"] = $"{md:F6} m ({diameter} {unit})",
                ["endCondition"] = throughAll ? "swEndCondThroughAll (1)" : "swEndCondBlind (0)"
            },
            CodePreview = $@"// Create hole using Hole Wizard
swModel.FeatureManager.HoleWizard5(
    0,         // HoleType (0 = Hole)
    0,         // StandardType (0 = ANSI Metric)
    0,         // FastenerType
    ""{diameter} {unit}"",  // Size string
    {(throughAll ? "1" : "0")},  // EndCondition ({(throughAll ? "Through All" : "Blind")})
    {md:F6},   // Diameter ({diameter} {unit})
    0,         // Depth (for blind holes)
    0, 0, 0,   // CounterBore/Sink parameters
    0, 0,      // Thread parameters
    0, 0, 0,   // Additional parameters
    0, 0, 0,
    0, 0, 0
);",
            DocumentationUrl = API_BASE_URL + "SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFeatureManager~HoleWizard5.html"
        };
    }

    public static ApiCallPreview CreateFilletPreview(double radius, string unit, bool allEdges = false)
    {
        double scale = unit.ToLower() == "inches" ? 0.0254 : 1.0;
        double mr = radius * scale;

        return new ApiCallPreview
        {
            ApiMethod = "FeatureManager.FeatureFillet3",
            Description = $"Add fillet with radius {radius} {unit} {(allEdges ? "to all edges" : "")}",
            Parameters = new Dictionary<string, string>
            {
                ["radius"] = $"{mr:F6} m ({radius} {unit})",
                ["featureType"] = "swFeatureFilletSimple (0)",
                ["propagateToTangent"] = allEdges.ToString()
            },
            CodePreview = $@"// Select edges first (if not selecting all)
// swModel.Extension.SelectByID2(""Edge<1>"", ""EDGE"", 0, 0, 0, true, 0, null, 0);
// Create constant radius fillet
swModel.FeatureManager.FeatureFillet3(
    195,       // Options bitmask
    {mr:F6},   // Radius ({radius} {unit})
    0,         // Fillet type (0 = Constant)
    0,         // Overflow type
    0, 0,      // Radius2, ConicRatio
    0,         // SetBack distance  
    0, 0, 0,   // Point coordinates
    0, 0, 0,   // Additional parameters
    0, 0, 0
);",
            DocumentationUrl = API_BASE_URL + "SolidWorks.Interop.sldworks~SolidWorks.Interop.sldworks.IFeatureManager~FeatureFillet3.html"
        };
    }
}
