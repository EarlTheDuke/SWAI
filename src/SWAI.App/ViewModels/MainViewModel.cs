using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using System.Collections.ObjectModel;

namespace SWAI.App.ViewModels;

/// <summary>
/// Main view model for the application
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly ISolidWorksService _solidWorksService;
    private readonly IPartService _partService;
    private readonly IAIService _aiService;
    private readonly ICommandExecutor _commandExecutor;
    private readonly ISessionManager _sessionManager;
    private readonly SwaiConfiguration _config;

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private string _connectionStatus = "Disconnected";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _showConnectButton = true;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _operationMode = "Mock Mode";

    [ObservableProperty]
    private string _activePartName = string.Empty;

    [ObservableProperty]
    private bool _hasActivePart;

    [ObservableProperty]
    private bool _isProcessing;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();
    public ObservableCollection<string> PartFeatures { get; } = new();

    public MainViewModel(
        ISolidWorksService solidWorksService,
        IPartService partService,
        IAIService aiService,
        ICommandExecutor commandExecutor,
        ISessionManager sessionManager,
        SwaiConfiguration config,
        ILogger<MainViewModel> logger)
    {
        _solidWorksService = solidWorksService;
        _partService = partService;
        _aiService = aiService;
        _commandExecutor = commandExecutor;
        _sessionManager = sessionManager;
        _config = config;
        _logger = logger;

        // Subscribe to events
        _solidWorksService.StatusChanged += OnConnectionStatusChanged;
        _partService.ActivePartChanged += OnActivePartChanged;

        // Set initial mode
        OperationMode = config.SolidWorks.UseMock ? "Mock Mode" : "Live Mode";

        // Add welcome message
        AddAssistantMessage(GetWelcomeMessage());

        // Auto-connect if configured
        if (config.SolidWorks.AutoConnect)
        {
            _ = ConnectAsync();
        }
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        StatusMessage = "Connecting to SolidWorks...";
        var success = await _solidWorksService.ConnectAsync();
        
        if (success)
        {
            var info = await _solidWorksService.GetInfoAsync();
            if (info != null)
            {
                AddAssistantMessage($"Connected to SolidWorks {info.Version}. Ready to create!");
            }
        }
        else if (!_config.SolidWorks.UseMock)
        {
            AddAssistantMessage("Could not connect to SolidWorks. Running in mock mode for testing.");
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userInput = InputText.Trim();
        InputText = string.Empty;

        // Add user message
        AddUserMessage(userInput);

        IsProcessing = true;
        StatusMessage = "Processing...";

        try
        {
            // Process through AI
            var response = await _aiService.ProcessInputAsync(userInput, _sessionManager.History);

            // Add AI response
            if (!string.IsNullOrEmpty(response.Message))
            {
                AddAssistantMessage(response.Message);
            }

            // Execute commands if any
            if (response.Commands.Count > 0)
            {
                foreach (var command in response.Commands)
                {
                    StatusMessage = $"Executing: {command.Description}";
                    var result = await _commandExecutor.ExecuteAsync(command);
                    
                    if (result.Success)
                    {
                        AddAssistantMessage($"✓ {result.Message}");
                    }
                    else
                    {
                        AddErrorMessage($"✗ {result.Message}");
                    }
                }
            }
            else if (response.NeedsClarification)
            {
                // AI needs more information - message already added
            }

            StatusMessage = "Ready";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input");
            AddErrorMessage($"Error: {ex.Message}");
            StatusMessage = "Error occurred";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool CanSend() => !IsProcessing && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_partService.ActivePart == null) return;

        StatusMessage = "Saving...";
        var result = await _commandExecutor.ExecuteAsync(new SavePartCommand());
        
        if (result.Success)
            AddAssistantMessage("Part saved successfully.");
        else
            AddErrorMessage($"Failed to save: {result.Message}");

        StatusMessage = "Ready";
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_partService.ActivePart == null) return;

        var filename = $"{_partService.ActivePart.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.step";
        var command = new ExportPartCommand(filename, Core.Models.Documents.ExportFormat.STEP);

        StatusMessage = "Exporting...";
        var result = await _commandExecutor.ExecuteAsync(command);

        if (result.Success)
            AddAssistantMessage($"Exported to: {filename}");
        else
            AddErrorMessage($"Failed to export: {result.Message}");

        StatusMessage = "Ready";
    }

    private void OnConnectionStatusChanged(object? sender, Core.Interfaces.ConnectionStatus status)
    {
        IsConnected = status == Core.Interfaces.ConnectionStatus.Connected;
        ConnectionStatus = status switch
        {
            Core.Interfaces.ConnectionStatus.Connected => "Connected",
            Core.Interfaces.ConnectionStatus.Connecting => "Connecting...",
            Core.Interfaces.ConnectionStatus.Disconnected => "Disconnected",
            Core.Interfaces.ConnectionStatus.Error => "Error",
            _ => "Unknown"
        };
        ShowConnectButton = status == Core.Interfaces.ConnectionStatus.Disconnected || status == Core.Interfaces.ConnectionStatus.Error;
    }

    private void OnActivePartChanged(object? sender, Core.Models.Documents.PartDocument? part)
    {
        HasActivePart = part != null;
        ActivePartName = part?.Name ?? string.Empty;

        PartFeatures.Clear();
        if (part != null)
        {
            foreach (var feature in part.Features)
            {
                PartFeatures.Add($"• {feature.Name} ({feature.FeatureType})");
            }
        }
    }

    private void AddUserMessage(string content)
    {
        var msg = new ChatMessageViewModel
        {
            Content = content,
            IsUser = true,
            Timestamp = DateTime.Now
        };
        Messages.Add(msg);
        _sessionManager.AddMessage(new Core.Interfaces.ChatMessage
        {
            Role = "user",
            Content = content
        });
    }

    private void AddAssistantMessage(string content)
    {
        var msg = new ChatMessageViewModel
        {
            Content = content,
            IsUser = false,
            Timestamp = DateTime.Now
        };
        Messages.Add(msg);
        _sessionManager.AddMessage(new Core.Interfaces.ChatMessage
        {
            Role = "assistant",
            Content = content
        });
    }

    private void AddErrorMessage(string content)
    {
        var msg = new ChatMessageViewModel
        {
            Content = content,
            IsUser = false,
            IsError = true,
            Timestamp = DateTime.Now
        };
        Messages.Add(msg);
    }

    private string GetWelcomeMessage()
    {
        return @"Welcome to SWAI - SolidWorks AI Assistant! 🔧

I can help you create 3D parts using natural language. Here are some examples:

• ""Create a box 10 x 20 x 5 inches""
• ""Make a plate 36 inches wide, 96 inches long, 0.75 inches thick""
• ""Create a cylinder 2 inch diameter, 6 inches tall""

Type ""help"" for more commands. What would you like to create?";
    }
}

/// <summary>
/// View model for chat messages
/// </summary>
public class ChatMessageViewModel
{
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public bool IsError { get; set; }
    public DateTime Timestamp { get; set; }
}
