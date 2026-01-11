using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SWAI.Core.Configuration;

namespace SWAI.AI.Providers;

/// <summary>
/// Supported AI providers
/// </summary>
public enum AiProvider
{
    OpenAI,
    AzureOpenAI,
    xAI,
    Anthropic
}

/// <summary>
/// Configuration for a specific AI provider
/// </summary>
public class ProviderConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? BaseUrl { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? DeploymentName { get; set; }
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}

/// <summary>
/// Extended AI configuration with multiple providers
/// </summary>
public class MultiProviderAIConfiguration
{
    /// <summary>
    /// Primary provider to use
    /// </summary>
    public AiProvider PrimaryProvider { get; set; } = AiProvider.OpenAI;

    /// <summary>
    /// Fallback provider if primary fails
    /// </summary>
    public AiProvider? FallbackProvider { get; set; }

    /// <summary>
    /// Provider-specific configurations
    /// </summary>
    public Dictionary<AiProvider, ProviderConfiguration> Providers { get; set; } = new();

    /// <summary>
    /// Get configuration for a specific provider
    /// </summary>
    public ProviderConfiguration? GetProviderConfig(AiProvider provider) =>
        Providers.GetValueOrDefault(provider);
}

/// <summary>
/// Factory for creating AI provider instances
/// </summary>
public interface IAiProviderFactory
{
    /// <summary>
    /// Available providers
    /// </summary>
    IReadOnlyList<AiProvider> AvailableProviders { get; }

    /// <summary>
    /// Current active provider
    /// </summary>
    AiProvider CurrentProvider { get; }

    /// <summary>
    /// Current model name
    /// </summary>
    string CurrentModel { get; }

    /// <summary>
    /// Create a chat completion service for the specified provider
    /// </summary>
    IChatCompletionService CreateChatService(AiProvider provider);

    /// <summary>
    /// Create a Semantic Kernel with the specified provider
    /// </summary>
    Kernel CreateKernel(AiProvider provider);

    /// <summary>
    /// Create a Semantic Kernel with the primary provider and fallback
    /// </summary>
    Kernel CreateKernelWithFallback();

    /// <summary>
    /// Switch to a different provider
    /// </summary>
    void SwitchProvider(AiProvider provider);

    /// <summary>
    /// Check if a provider is properly configured
    /// </summary>
    bool IsProviderConfigured(AiProvider provider);

    /// <summary>
    /// Get display name for a provider
    /// </summary>
    string GetProviderDisplayName(AiProvider provider);

    /// <summary>
    /// Get available models for a provider
    /// </summary>
    IReadOnlyList<string> GetAvailableModels(AiProvider provider);
}
