using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SWAI.AI;
using SWAI.App.ViewModels;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Services;
using SWAI.SolidWorks;
using System.IO;
using System.Windows;

namespace SWAI.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
            })
            .UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.Debug()
                    .WriteTo.Console()
                    .WriteTo.File(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SWAI", "logs", "swai-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7
                    );
            })
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(context.Configuration, services);
            })
            .Build();
    }

    private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        // Load configuration
        var swaiConfig = new SwaiConfiguration();
        configuration.Bind(swaiConfig);

        // If no config file exists, use defaults with mock mode
        if (string.IsNullOrEmpty(swaiConfig.AI.ApiKey) || swaiConfig.AI.ApiKey == "your-openai-api-key-here")
        {
            swaiConfig.SolidWorks.UseMock = true;
        }

        // Register configuration
        services.AddSingleton(swaiConfig);
        services.AddSingleton(swaiConfig.AI);
        services.AddSingleton(swaiConfig.SolidWorks);
        services.AddSingleton(swaiConfig.Export);
        services.AddSingleton(swaiConfig.Application);

        // Register AI services
        services.AddSwaiAIServices(swaiConfig.AI);

        // Register SolidWorks services
        services.AddSwaiSolidWorksServices(swaiConfig.SolidWorks);

        // Register session manager
        services.AddSingleton<ISessionManager>(sp => 
            new SessionManager(swaiConfig.Application.MaxHistoryItems));

        // Register ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();

        // Register main window
        services.AddSingleton<Views.MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<Views.MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Get service from the DI container
    /// </summary>
    public static T GetService<T>() where T : class
    {
        var app = (App)Current;
        return app._host.Services.GetRequiredService<T>();
    }
}
