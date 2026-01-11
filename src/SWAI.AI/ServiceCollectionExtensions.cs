using Microsoft.Extensions.DependencyInjection;
using SWAI.AI.Parsing;
using SWAI.AI.Services;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;

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
        services.AddSingleton<IAIService, AIService>();
        services.AddSingleton<CommandParser>();

        return services;
    }
}
