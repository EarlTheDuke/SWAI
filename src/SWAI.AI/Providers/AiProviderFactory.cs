#pragma warning disable SKEXP0010 // Experimental API

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SWAI.AI.Providers;

/// <summary>
/// Factory implementation for creating AI provider instances
/// </summary>
public class AiProviderFactory : IAiProviderFactory
{
    private readonly ILogger<AiProviderFactory> _logger;
    private readonly MultiProviderAIConfiguration _config;
    private AiProvider _currentProvider;

    private static readonly Dictionary<AiProvider, string[]> ProviderModels = new()
    {
        [AiProvider.OpenAI] = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo" },
        [AiProvider.AzureOpenAI] = new[] { "gpt-4o", "gpt-4", "gpt-35-turbo" },
        [AiProvider.xAI] = new[] { "grok-4", "grok-4-mini", "grok-beta", "grok-2" },
        [AiProvider.Anthropic] = new[] { "claude-opus-4-5-20250514", "claude-sonnet-4-20250514", "claude-3-5-sonnet-20241022", "claude-3-opus-20240229" }
    };

    private static readonly Dictionary<AiProvider, string> ProviderDisplayNames = new()
    {
        [AiProvider.OpenAI] = "OpenAI",
        [AiProvider.AzureOpenAI] = "Azure OpenAI",
        [AiProvider.xAI] = "xAI (Grok)",
        [AiProvider.Anthropic] = "Anthropic (Claude)"
    };

    public IReadOnlyList<AiProvider> AvailableProviders => 
        _config.Providers.Keys.Where(IsProviderConfigured).ToList();

    public AiProvider CurrentProvider => _currentProvider;

    public string CurrentModel => 
        _config.GetProviderConfig(_currentProvider)?.Model ?? "Unknown";

    public AiProviderFactory(
        MultiProviderAIConfiguration config,
        ILogger<AiProviderFactory> logger)
    {
        _config = config;
        _logger = logger;
        _currentProvider = config.PrimaryProvider;

        _logger.LogInformation("AI Provider Factory initialized. Primary: {Provider}", _currentProvider);
    }

    public IChatCompletionService CreateChatService(AiProvider provider)
    {
        var config = _config.GetProviderConfig(provider);
        if (config == null || string.IsNullOrEmpty(config.ApiKey))
        {
            throw new InvalidOperationException($"Provider {provider} is not configured");
        }

        _logger.LogInformation("Creating chat service for {Provider} with model {Model}", provider, config.Model);

        return provider switch
        {
            AiProvider.OpenAI => CreateOpenAIService(config),
            AiProvider.AzureOpenAI => CreateAzureOpenAIService(config),
            AiProvider.xAI => CreateXAIService(config),
            AiProvider.Anthropic => CreateAnthropicService(config),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };
    }

    public Kernel CreateKernel(AiProvider provider)
    {
        var config = _config.GetProviderConfig(provider);
        if (config == null || string.IsNullOrEmpty(config.ApiKey))
        {
            throw new InvalidOperationException($"Provider {provider} is not configured");
        }

        var builder = Kernel.CreateBuilder();

        switch (provider)
        {
            case AiProvider.OpenAI:
                builder.AddOpenAIChatCompletion(
                    modelId: config.Model,
                    apiKey: config.ApiKey);
                break;

            case AiProvider.AzureOpenAI:
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: config.DeploymentName ?? config.Model,
                    endpoint: config.Endpoint!,
                    apiKey: config.ApiKey);
                break;

            case AiProvider.xAI:
                // xAI uses OpenAI-compatible API
                builder.AddOpenAIChatCompletion(
                    modelId: config.Model,
                    apiKey: config.ApiKey,
                    endpoint: new Uri(config.BaseUrl ?? "https://api.x.ai/v1"));
                break;

            case AiProvider.Anthropic:
                // For Anthropic, we add a custom service
                var anthropicService = CreateAnthropicService(config);
                builder.Services.AddSingleton<IChatCompletionService>(anthropicService);
                break;

            default:
                throw new ArgumentException($"Unknown provider: {provider}");
        }

        _logger.LogInformation("Created Kernel for {Provider}", provider);
        return builder.Build();
    }

    public Kernel CreateKernelWithFallback()
    {
        try
        {
            return CreateKernel(_currentProvider);
        }
        catch (Exception ex) when (_config.FallbackProvider.HasValue)
        {
            _logger.LogWarning(ex, "Primary provider {Primary} failed, falling back to {Fallback}",
                _currentProvider, _config.FallbackProvider.Value);
            
            return CreateKernel(_config.FallbackProvider.Value);
        }
    }

    public void SwitchProvider(AiProvider provider)
    {
        if (!IsProviderConfigured(provider))
        {
            throw new InvalidOperationException($"Provider {provider} is not configured");
        }

        _currentProvider = provider;
        _logger.LogInformation("Switched to provider: {Provider}", provider);
    }

    public bool IsProviderConfigured(AiProvider provider)
    {
        var config = _config.GetProviderConfig(provider);
        if (config == null || string.IsNullOrEmpty(config.ApiKey))
            return false;

        // Provider-specific validation
        return provider switch
        {
            AiProvider.AzureOpenAI => !string.IsNullOrEmpty(config.Endpoint),
            _ => true
        };
    }

    public string GetProviderDisplayName(AiProvider provider) =>
        ProviderDisplayNames.GetValueOrDefault(provider, provider.ToString());

    public IReadOnlyList<string> GetAvailableModels(AiProvider provider) =>
        ProviderModels.GetValueOrDefault(provider, Array.Empty<string>());

    #region Provider-Specific Service Creation

    private IChatCompletionService CreateOpenAIService(ProviderConfiguration config)
    {
        return new OpenAIChatCompletionService(
            modelId: config.Model,
            apiKey: config.ApiKey);
    }

    private IChatCompletionService CreateAzureOpenAIService(ProviderConfiguration config)
    {
        return new AzureOpenAIChatCompletionService(
            deploymentName: config.DeploymentName ?? config.Model,
            endpoint: config.Endpoint!,
            apiKey: config.ApiKey);
    }

    private IChatCompletionService CreateXAIService(ProviderConfiguration config)
    {
        // xAI uses OpenAI-compatible API with custom endpoint
        return new OpenAIChatCompletionService(
            modelId: config.Model,
            apiKey: config.ApiKey,
            endpoint: new Uri(config.BaseUrl ?? "https://api.x.ai/v1"));
    }

    private IChatCompletionService CreateAnthropicService(ProviderConfiguration config)
    {
        // Return Anthropic adapter service
        return new AnthropicChatCompletionService(
            apiKey: config.ApiKey,
            model: config.Model,
            logger: _logger);
    }

    #endregion
}
