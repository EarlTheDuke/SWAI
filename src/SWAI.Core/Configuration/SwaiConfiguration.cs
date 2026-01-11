using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Units;

namespace SWAI.Core.Configuration;

/// <summary>
/// AI provider configuration (legacy - single provider)
/// </summary>
public class AIConfiguration
{
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string Model { get; set; } = "gpt-4o";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// Fallback provider name
    /// </summary>
    public string? FallbackProvider { get; set; }
    
    /// <summary>
    /// Multi-provider configurations
    /// </summary>
    public ProvidersConfiguration? Providers { get; set; }
}

/// <summary>
/// Configuration for all supported AI providers
/// </summary>
public class ProvidersConfiguration
{
    public ProviderConfig? OpenAI { get; set; }
    public AzureProviderConfig? AzureOpenAI { get; set; }
    public XAIProviderConfig? xAI { get; set; }
    public ProviderConfig? Anthropic { get; set; }
}

/// <summary>
/// Base provider configuration
/// </summary>
public class ProviderConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Azure OpenAI specific configuration
/// </summary>
public class AzureProviderConfig : ProviderConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
}

/// <summary>
/// xAI (Grok) specific configuration
/// </summary>
public class XAIProviderConfig : ProviderConfig
{
    public string BaseUrl { get; set; } = "https://api.x.ai/v1";
}

/// <summary>
/// SolidWorks configuration
/// </summary>
public class SolidWorksConfiguration
{
    public bool AutoConnect { get; set; } = true;
    public bool UseMock { get; set; } = false;
    public string DefaultUnits { get; set; } = "Inches";
    public bool StartVisible { get; set; } = true;
    public string? InstallPath { get; set; }

    public UnitSystem GetDefaultUnitSystem() => DefaultUnits.ToLowerInvariant() switch
    {
        "inches" or "in" => UnitSystem.Inches,
        "millimeters" or "mm" => UnitSystem.Millimeters,
        "centimeters" or "cm" => UnitSystem.Centimeters,
        "meters" or "m" => UnitSystem.Meters,
        _ => UnitSystem.Inches
    };
}

/// <summary>
/// Export configuration
/// </summary>
public class ExportConfiguration
{
    public string DefaultFormat { get; set; } = "STEP";
    public string DefaultDirectory { get; set; } = string.Empty;
    public bool IncludeTimestamp { get; set; } = true;

    public ExportFormat GetDefaultFormat() => DefaultFormat.ToUpperInvariant() switch
    {
        "STEP" or "STP" => ExportFormat.STEP,
        "IGES" or "IGS" => ExportFormat.IGES,
        "STL" => ExportFormat.STL,
        "DXF" => ExportFormat.DXF,
        "DWG" => ExportFormat.DWG,
        "PARASOLID" or "X_T" => ExportFormat.Parasolid,
        _ => ExportFormat.STEP
    };
}

/// <summary>
/// Logging configuration
/// </summary>
public class LoggingConfiguration
{
    public Dictionary<string, string> LogLevel { get; set; } = new()
    {
        { "Default", "Information" },
        { "Microsoft", "Warning" },
        { "SWAI", "Debug" }
    };
}

/// <summary>
/// Application configuration
/// </summary>
public class ApplicationConfiguration
{
    public string Theme { get; set; } = "Dark";
    public bool SaveHistoryOnExit { get; set; } = true;
    public int MaxHistoryItems { get; set; } = 100;
}

/// <summary>
/// Root configuration class
/// </summary>
public class SwaiConfiguration
{
    public AIConfiguration AI { get; set; } = new();
    public SolidWorksConfiguration SolidWorks { get; set; } = new();
    public ExportConfiguration Export { get; set; } = new();
    public LoggingConfiguration Logging { get; set; } = new();
    public ApplicationConfiguration Application { get; set; } = new();
}
