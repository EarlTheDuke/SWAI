using Microsoft.Extensions.DependencyInjection;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.SolidWorks.Services;

namespace SWAI.SolidWorks;

/// <summary>
/// Extension methods for registering SolidWorks services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SWAI SolidWorks services to the service collection
    /// </summary>
    public static IServiceCollection AddSwaiSolidWorksServices(
        this IServiceCollection services,
        SolidWorksConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddSingleton<SolidWorksService>();
        services.AddSingleton<ISolidWorksService>(sp => sp.GetRequiredService<SolidWorksService>());
        services.AddSingleton<IPartService, PartService>();
        services.AddSingleton<ISketchService, SketchService>();
        services.AddSingleton<IFeatureService, FeatureService>();
        
        // Pattern and hole wizard services
        services.AddSingleton<PatternService>();
        services.AddSingleton<HoleWizardService>();
        
        // Command executor
        services.AddSingleton<Core.Interfaces.ICommandExecutor, CommandExecutor>();

        return services;
    }
}
