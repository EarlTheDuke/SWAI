namespace SWAI.Core.Configuration;

/// <summary>
/// Configuration for mock mode operation
/// </summary>
public class MockConfiguration
{
    /// <summary>
    /// Whether mock mode is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Random failure rate (0.0 to 1.0)
    /// </summary>
    public double FailureRate { get; set; } = 0.0;

    /// <summary>
    /// Whether to record API calls for later playback
    /// </summary>
    public bool RecordMode { get; set; } = false;

    /// <summary>
    /// Path to recorded sessions directory
    /// </summary>
    public string RecordingsPath { get; set; } = string.Empty;

    /// <summary>
    /// Simulated response delay range (min ms)
    /// </summary>
    public int MinDelayMs { get; set; } = 100;

    /// <summary>
    /// Simulated response delay range (max ms)
    /// </summary>
    public int MaxDelayMs { get; set; } = 500;

    /// <summary>
    /// Whether to generate realistic feature IDs and names
    /// </summary>
    public bool RealisticResponses { get; set; } = true;

    /// <summary>
    /// Seed for random number generation (for reproducibility)
    /// </summary>
    public int? RandomSeed { get; set; }
}
