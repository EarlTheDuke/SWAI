using Microsoft.Extensions.Logging;
using SWAI.Core.Commands;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using SWAI.Core.Models.Documents;
using SWAI.Core.Models.Session;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWAI.Core.Services;

/// <summary>
/// Session manager with persistence support
/// </summary>
public class PersistentSessionManager : ISessionManager, IDisposable
{
    private readonly ILogger<PersistentSessionManager> _logger;
    private readonly SwaiConfiguration _config;
    private readonly string _sessionsDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    private SessionData _currentSession;
    private bool _autoSaveEnabled = true;
    private Timer? _autoSaveTimer;

    /// <summary>
    /// Current session ID (ISessionManager)
    /// </summary>
    public Guid SessionId => _currentSession.SessionId;

    /// <summary>
    /// Alias for SessionId
    /// </summary>
    public Guid CurrentSessionId => SessionId;

    /// <summary>
    /// Current session name
    /// </summary>
    public string CurrentSessionName => _currentSession.Name;

    /// <summary>
    /// Chat history for the current session (ISessionManager)
    /// </summary>
    public IReadOnlyList<ChatMessage> History => _currentSession.ChatHistory;

    /// <summary>
    /// Current design state
    /// </summary>
    public DesignState? CurrentState => _currentSession.GetLatestState();

    /// <summary>
    /// Command history
    /// </summary>
    public IReadOnlyList<CommandHistoryEntry> CommandHistory => _currentSession.CommandHistory;

    /// <summary>
    /// Event raised when session changes
    /// </summary>
    public event EventHandler<SessionData>? SessionChanged;

    /// <summary>
    /// Event raised when state snapshot is taken
    /// </summary>
    public event EventHandler<DesignState>? StateSnapshotTaken;

    public PersistentSessionManager(
        SwaiConfiguration config,
        ILogger<PersistentSessionManager> logger)
    {
        _config = config;
        _logger = logger;

        // Setup sessions directory
        _sessionsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SWAI", "sessions"
        );
        Directory.CreateDirectory(_sessionsDirectory);

        // JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        // Initialize with new session
        _currentSession = new SessionData
        {
            Name = $"Session_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        // Setup auto-save timer (every 30 seconds)
        _autoSaveTimer = new Timer(AutoSaveCallback, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        _logger.LogInformation("Session manager initialized: {SessionId}", CurrentSessionId);
    }

    #region ISessionManager Implementation

    public void AddMessage(ChatMessage message)
    {
        _currentSession.ChatHistory.Add(message);
        _currentSession.ModifiedAt = DateTime.UtcNow;
        
        _logger.LogDebug("Added message: {Role}", message.Role);
    }

    public void ClearHistory()
    {
        _currentSession.ChatHistory.Clear();
        _currentSession.ModifiedAt = DateTime.UtcNow;
        
        _logger.LogInformation("Chat history cleared");
    }

    public string GetFormattedHistory(int maxMessages = 20)
    {
        var messages = _currentSession.ChatHistory.TakeLast(maxMessages);
        return string.Join("\n", messages.Select(m => $"{m.Role}: {m.Content}"));
    }

    /// <summary>
    /// Start a new session (ISessionManager - synchronous wrapper)
    /// </summary>
    public void NewSession()
    {
        NewSessionAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Export session history (ISessionManager)
    /// </summary>
    public string ExportHistory()
    {
        return GetFormattedHistory(int.MaxValue);
    }

    #endregion

    #region Session Management

    /// <summary>
    /// Create a new session
    /// </summary>
    public async Task<SessionData> NewSessionAsync(string? name = null)
    {
        // Save current session first
        if (_currentSession.ChatHistory.Count > 0)
        {
            await SaveAsync();
        }

        _currentSession = new SessionData
        {
            Name = name ?? $"Session_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        _logger.LogInformation("Created new session: {Name}", _currentSession.Name);
        SessionChanged?.Invoke(this, _currentSession);

        return _currentSession;
    }

    /// <summary>
    /// Save current session
    /// </summary>
    public async Task SaveAsync(string? path = null)
    {
        var filePath = path ?? GetSessionFilePath(_currentSession.SessionId);
        
        try
        {
            var json = JsonSerializer.Serialize(_currentSession, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogInformation("Session saved: {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session");
            throw;
        }
    }

    /// <summary>
    /// Load a session
    /// </summary>
    public async Task<SessionData> LoadAsync(Guid sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        return await LoadFromFileAsync(filePath);
    }

    /// <summary>
    /// Load a session from file
    /// </summary>
    public async Task<SessionData> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Session file not found", filePath);
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var session = JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
            
            if (session == null)
            {
                throw new InvalidDataException("Failed to deserialize session");
            }

            _currentSession = session;
            _logger.LogInformation("Session loaded: {Name}", _currentSession.Name);
            SessionChanged?.Invoke(this, _currentSession);

            return _currentSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session from {Path}", filePath);
            throw;
        }
    }

    /// <summary>
    /// List available sessions
    /// </summary>
    public async Task<List<SessionInfo>> ListSessionsAsync()
    {
        var sessions = new List<SessionInfo>();
        var files = Directory.GetFiles(_sessionsDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<SessionData>(json, _jsonOptions);
                
                if (session != null)
                {
                    sessions.Add(new SessionInfo
                    {
                        SessionId = session.SessionId,
                        Name = session.Name,
                        CreatedAt = session.CreatedAt,
                        ModifiedAt = session.ModifiedAt,
                        MessageCount = session.ChatHistory.Count,
                        CommandCount = session.CommandHistory.Count,
                        FilePath = file
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read session file: {File}", file);
            }
        }

        return sessions.OrderByDescending(s => s.ModifiedAt).ToList();
    }

    /// <summary>
    /// Delete a session
    /// </summary>
    public Task DeleteSessionAsync(Guid sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Session deleted: {SessionId}", sessionId);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Design State Management

    /// <summary>
    /// Take a snapshot of the current design state
    /// </summary>
    public void TakeSnapshot(string trigger, PartDocument? activePart = null, AssemblyDocument? activeAssembly = null)
    {
        var state = new DesignState
        {
            Trigger = trigger,
            ActiveDocumentType = activePart != null ? DocumentType.Part : 
                                 activeAssembly != null ? DocumentType.Assembly : DocumentType.Part,
            ActiveDocumentName = activePart?.Name ?? activeAssembly?.Name
        };

        if (activePart != null)
        {
            state.OpenParts.Add(PartSummary.FromDocument(activePart));
            state.RecentFeatures.AddRange(
                activePart.Features.TakeLast(5).Select(FeatureSummary.FromFeature)
            );
        }

        if (activeAssembly != null)
        {
            state.OpenAssemblies.Add(AssemblySummary.FromDocument(activeAssembly));
        }

        _currentSession.AddSnapshot(state);
        StateSnapshotTaken?.Invoke(this, state);
        
        _logger.LogDebug("State snapshot taken: {Trigger}", trigger);
    }

    /// <summary>
    /// Add named reference
    /// </summary>
    public void AddNamedReference(string name, EntityReference reference)
    {
        var state = CurrentState;
        if (state != null)
        {
            state.NamedReferences[name.ToLowerInvariant()] = reference;
            _logger.LogDebug("Added named reference: {Name}", name);
        }
    }

    /// <summary>
    /// Get named reference
    /// </summary>
    public EntityReference? GetNamedReference(string name)
    {
        var state = CurrentState;
        return state?.NamedReferences.GetValueOrDefault(name.ToLowerInvariant());
    }

    /// <summary>
    /// Get context summary for prompt injection
    /// </summary>
    public string GetContextSummary()
    {
        var state = CurrentState;
        return state?.ToPromptSummary() ?? "No design context";
    }

    #endregion

    #region Command History

    /// <summary>
    /// Record a command execution
    /// </summary>
    public void RecordCommand(ISwaiCommand command, CommandResult result, string userInput)
    {
        var entry = new CommandHistoryEntry
        {
            UserInput = userInput,
            CommandType = command.CommandType,
            Description = command.Description,
            Success = result.Success,
            ResultMessage = result.Message,
            ExecutionTime = result.ExecutionTime,
            CanUndo = command.CanUndo
        };

        _currentSession.CommandHistory.Add(entry);
        _currentSession.ModifiedAt = DateTime.UtcNow;

        _logger.LogDebug("Recorded command: {Type} ({Success})", command.CommandType, result.Success);
    }

    /// <summary>
    /// Get the last successful command
    /// </summary>
    public CommandHistoryEntry? GetLastCommand() =>
        _currentSession.CommandHistory.LastOrDefault(c => c.Success && !c.IsUndone);

    /// <summary>
    /// Get undoable commands
    /// </summary>
    public IEnumerable<CommandHistoryEntry> GetUndoableCommands() =>
        _currentSession.CommandHistory.Where(c => c.Success && c.CanUndo && !c.IsUndone).Reverse();

    /// <summary>
    /// Mark a command as undone
    /// </summary>
    public void MarkCommandUndone(Guid commandId)
    {
        var entry = _currentSession.CommandHistory.FirstOrDefault(c => c.Id == commandId);
        if (entry != null)
        {
            entry.IsUndone = true;
            _currentSession.ModifiedAt = DateTime.UtcNow;
        }
    }

    #endregion

    #region Special Commands

    /// <summary>
    /// Get session summary
    /// </summary>
    public string GetSessionSummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📋 Session: {_currentSession.Name}");
        sb.AppendLine($"   Created: {_currentSession.CreatedAt:g}");
        sb.AppendLine($"   Messages: {_currentSession.ChatHistory.Count}");
        sb.AppendLine($"   Commands: {_currentSession.CommandHistory.Count}");
        sb.AppendLine($"   Snapshots: {_currentSession.StateSnapshots.Count}");

        var state = CurrentState;
        if (state != null)
        {
            sb.AppendLine();
            sb.AppendLine($"📐 Current State:");
            sb.AppendLine($"   {state.ToPromptSummary()}");
        }

        var recentCmds = _currentSession.CommandHistory.TakeLast(5);
        if (recentCmds.Any())
        {
            sb.AppendLine();
            sb.AppendLine("🔧 Recent Commands:");
            foreach (var cmd in recentCmds)
            {
                var status = cmd.Success ? "✓" : "✗";
                sb.AppendLine($"   {status} {cmd.Description}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// List components in current context
    /// </summary>
    public string ListComponents()
    {
        var state = CurrentState;
        if (state == null)
            return "No design context available.";

        var sb = new System.Text.StringBuilder();

        if (state.OpenParts.Count > 0)
        {
            sb.AppendLine("Parts:");
            foreach (var part in state.OpenParts)
            {
                sb.AppendLine($"  📄 {part.Name}");
                foreach (var feature in part.FeatureNames.Take(10))
                {
                    sb.AppendLine($"      • {feature}");
                }
            }
        }

        if (state.OpenAssemblies.Count > 0)
        {
            sb.AppendLine("Assemblies:");
            foreach (var asm in state.OpenAssemblies)
            {
                sb.AppendLine($"  📦 {asm.Name}");
                foreach (var comp in asm.ComponentNames.Take(10))
                {
                    sb.AppendLine($"      • {comp}");
                }
            }
        }

        return sb.Length > 0 ? sb.ToString() : "No components found.";
    }

    #endregion

    #region Private Methods

    private string GetSessionFilePath(Guid sessionId) =>
        Path.Combine(_sessionsDirectory, $"{sessionId}.json");

    private void AutoSaveCallback(object? state)
    {
        if (_autoSaveEnabled && _currentSession.ChatHistory.Count > 0)
        {
            try
            {
                // Fire and forget - we're in a timer callback
                _ = SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto-save failed");
            }
        }
    }

    #endregion

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
        
        // Final save
        if (_currentSession.ChatHistory.Count > 0)
        {
            SaveAsync().GetAwaiter().GetResult();
        }
    }
}

/// <summary>
/// Summary info for session listing
/// </summary>
public class SessionInfo
{
    public Guid SessionId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public int MessageCount { get; init; }
    public int CommandCount { get; init; }
    public string? FilePath { get; init; }
}
