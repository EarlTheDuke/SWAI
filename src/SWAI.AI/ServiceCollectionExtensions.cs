using Microsoft.Extensions.DependencyInjection;
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
}
