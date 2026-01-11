using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SWAI.AI.Parsing;
using SWAI.AI.Services;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Services;

namespace SWAI.AI;

/// <summary>
/// Extension methods for registering AI services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SWAI AI services to the service collection
    /// </summary>
    public static IServiceCollection AddSwaiAIServices(
        this IServiceCollection services,
        AIConfiguration configuration)
    {
        services.AddSingleton(configuration);
        
        // Register Semantic Kernel based on provider
        services.AddSingleton<Kernel>(sp =>
        {
            var logger = sp.GetService<ILogger<Kernel>>();
            return CreateKernel(configuration, logger);
        });
        
        // Use StructuredAIService for enhanced parsing
        services.AddSingleton<IAIService, StructuredAIService>();
        
        // Command parser for offline fallback
        services.AddSingleton<CommandParser>();
        
        // Conversation context for stateful interactions
        services.AddSingleton<ConversationContext>();
        
        // Command preview service
        services.AddSingleton<ICommandPreviewService, CommandPreviewService>();

        return services;
    }

    private static Kernel CreateKernel(AIConfiguration config, ILogger? logger)
    {
        var builder = Kernel.CreateBuilder();

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            logger?.LogWarning("No API key configured, AI features will be limited");
            return builder.Build();
        }

        var provider = config.Provider?.ToLowerInvariant() ?? "openai";

        try
        {
            switch (provider)
            {
                case "xai":
                case "grok":
                    // xAI uses OpenAI-compatible API
                    var xaiBaseUrl = config.Providers?.xAI?.BaseUrl ?? "https://api.x.ai/v1";
                    var xaiKey = config.Providers?.xAI?.ApiKey ?? config.ApiKey;
                    var xaiModel = config.Providers?.xAI?.Model ?? config.Model ?? "grok-beta";
                    
                    #pragma warning disable SKEXP0010
                    builder.AddOpenAIChatCompletion(
                        modelId: xaiModel,
                        apiKey: xaiKey,
                        endpoint: new Uri(xaiBaseUrl));
                    #pragma warning restore SKEXP0010
                    
                    logger?.LogInformation("Configured xAI provider with model {Model}", xaiModel);
                    break;

                case "azure":
                case "azureopenai":
                    var azureConfig = config.Providers?.AzureOpenAI;
                    if (azureConfig != null && !string.IsNullOrEmpty(azureConfig.Endpoint))
                    {
                        builder.AddAzureOpenAIChatCompletion(
                            deploymentName: azureConfig.DeploymentName ?? config.Model,
                            endpoint: azureConfig.Endpoint,
                            apiKey: azureConfig.ApiKey ?? config.ApiKey);
                        logger?.LogInformation("Configured Azure OpenAI provider");
                    }
                    break;

                case "anthropic":
                case "claude":
                    // Anthropic would need custom implementation
                    logger?.LogWarning("Anthropic provider requires custom setup");
                    break;

                case "openai":
                default:
                    builder.AddOpenAIChatCompletion(
                        modelId: config.Model ?? "gpt-4o",
                        apiKey: config.ApiKey);
                    logger?.LogInformation("Configured OpenAI provider with model {Model}", config.Model);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to configure AI provider {Provider}", provider);
        }

        return builder.Build();
    }
}
