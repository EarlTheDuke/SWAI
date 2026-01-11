using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Features;
using SWAI.Core.Models.Units;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Enhanced mock service with realistic responses
/// </summary>
public class EnhancedMockService
{
    private readonly ILogger _logger;
    private readonly MockConfiguration _config;
    private readonly MockRecorder? _recorder;
    private readonly Random _random;

    private int _featureCounter = 1;
    private int _sketchCounter = 1;

    private static readonly string[] FeatureNamePrefixes = 
    {
        "Boss-Extrude", "Cut-Extrude", "Fillet", "Chamfer", "Hole", 
        "LinearPattern", "CircularPattern", "Mirror", "Shell"
    };

    public EnhancedMockService(
        MockConfiguration config,
        ILogger logger,
        MockRecorder? recorder = null)
    {
        _config = config;
        _logger = logger;
        _recorder = recorder;
        _random = _config.RandomSeed.HasValue 
            ? new Random(_config.RandomSeed.Value) 
            : new Random();
    }

    /// <summary>
    /// Simulate an API call with configurable delay and failure rate
    /// </summary>
    public async Task<MockResult<T>> SimulateCallAsync<T>(
        string methodName,
        Func<T> generator,
        object? request = null) where T : class
    {
        var startTime = DateTime.UtcNow;
        
        // Simulate network/processing delay
        var delay = _random.Next(_config.MinDelayMs, _config.MaxDelayMs + 1);
        await Task.Delay(delay);

        // Check for random failure
        if (_random.NextDouble() < _config.FailureRate)
        {
            _logger.LogWarning("Mock failure triggered for {Method}", methodName);
            
            var failResult = new MockResult<T>
            {
                Success = false,
                Error = $"Simulated failure for {methodName}",
                Duration = DateTime.UtcNow - startTime
            };

            _recorder?.RecordCall(methodName, request, null, false, failResult.Duration);
            return failResult;
        }

        // Generate successful response
        var response = generator();
        var duration = DateTime.UtcNow - startTime;

        _logger.LogDebug("Mock {Method} completed in {Duration}ms", methodName, duration.TotalMilliseconds);
        
        _recorder?.RecordCall(methodName, request, response, true, duration);

        return new MockResult<T>
        {
            Success = true,
            Data = response,
            Duration = duration
        };
    }

    /// <summary>
    /// Generate a mock part document
    /// </summary>
    public PartDocument GeneratePart(string name)
    {
        var part = new PartDocument(name);
        
        if (_config.RealisticResponses)
        {
            // Add standard reference planes as features would be seen in SW
            _logger.LogDebug("Generated mock part: {Name}", name);
        }

        return part;
    }

    /// <summary>
    /// Generate a mock extrusion feature info
    /// </summary>
    public MockFeatureInfo GenerateExtrusion(
        string? name = null,
        Dimension? depth = null,
        ExtrusionDirection direction = ExtrusionDirection.MidPlane)
    {
        return new MockFeatureInfo
        {
            Name = name ?? $"Boss-Extrude{_featureCounter++}",
            Type = "Boss-Extrude",
            Parameters = new Dictionary<string, object>
            {
                ["Depth"] = depth ?? Dimension.Inches(1),
                ["Direction"] = direction
            }
        };
    }

    /// <summary>
    /// Generate a mock fillet feature info
    /// </summary>
    public MockFeatureInfo GenerateFillet(Dimension? radius = null)
    {
        return new MockFeatureInfo
        {
            Name = $"Fillet{_featureCounter++}",
            Type = "Fillet",
            Parameters = new Dictionary<string, object>
            {
                ["Radius"] = radius ?? Dimension.Inches(0.25)
            }
        };
    }

    /// <summary>
    /// Generate a mock chamfer feature info
    /// </summary>
    public MockFeatureInfo GenerateChamfer(Dimension? distance = null)
    {
        return new MockFeatureInfo
        {
            Name = $"Chamfer{_featureCounter++}",
            Type = "Chamfer",
            Parameters = new Dictionary<string, object>
            {
                ["Distance"] = distance ?? Dimension.Inches(0.125)
            }
        };
    }

    /// <summary>
    /// Generate a mock hole feature info
    /// </summary>
    public MockFeatureInfo GenerateHole(
        Dimension? diameter = null,
        Dimension? depth = null,
        bool throughAll = false)
    {
        return new MockFeatureInfo
        {
            Name = $"Hole{_featureCounter++}",
            Type = "Hole",
            Parameters = new Dictionary<string, object>
            {
                ["Diameter"] = diameter ?? Dimension.Inches(0.5),
                ["Depth"] = depth ?? Dimension.Inches(1),
                ["ThroughAll"] = throughAll
            }
        };
    }

    /// <summary>
    /// Generate a realistic feature name
    /// </summary>
    public string GenerateFeatureName(string featureType)
    {
        return $"{featureType}{_featureCounter++}";
    }

    /// <summary>
    /// Generate a sketch name
    /// </summary>
    public string GenerateSketchName()
    {
        return $"Sketch{_sketchCounter++}";
    }

    /// <summary>
    /// Generate a mock file path
    /// </summary>
    public string GenerateFilePath(string name, string extension = "sldprt")
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documentsPath, $"{name}.{extension}");
    }

    /// <summary>
    /// Reset counters (for new session)
    /// </summary>
    public void Reset()
    {
        _featureCounter = 1;
        _sketchCounter = 1;
    }
}

/// <summary>
/// Result of a mock operation
/// </summary>
public class MockResult<T> where T : class
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Mock feature information
/// </summary>
public class MockFeatureInfo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
}
