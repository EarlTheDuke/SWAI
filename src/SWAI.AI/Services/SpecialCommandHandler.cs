using Microsoft.Extensions.Logging;
using SWAI.Core.Interfaces;
using SWAI.Core.Services;

namespace SWAI.AI.Services;

/// <summary>
/// Handles special slash commands before they go to the AI
/// </summary>
public class SpecialCommandHandler
{
    private readonly ILogger<SpecialCommandHandler> _logger;
    private readonly PersistentSessionManager? _sessionManager;
    private readonly ICommandExecutor _commandExecutor;

    private readonly Dictionary<string, Func<string[], Task<SpecialCommandResult>>> _handlers;

    public SpecialCommandHandler(
        ICommandExecutor commandExecutor,
        ILogger<SpecialCommandHandler> logger,
        PersistentSessionManager? sessionManager = null)
    {
        _commandExecutor = commandExecutor;
        _sessionManager = sessionManager;
        _logger = logger;

        // Register handlers
        _handlers = new Dictionary<string, Func<string[], Task<SpecialCommandResult>>>(StringComparer.OrdinalIgnoreCase)
        {
            { "/help", HandleHelp },
            { "/summarize", HandleSummarize },
            { "/summary", HandleSummarize },
            { "/list", HandleList },
            { "/list-components", HandleList },
            { "/components", HandleList },
            { "/undo", HandleUndo },
            { "/undo-last", HandleUndo },
            { "/history", HandleHistory },
            { "/clear", HandleClear },
            { "/save-session", HandleSaveSession },
            { "/load-session", HandleLoadSession },
            { "/sessions", HandleListSessions },
            { "/new-session", HandleNewSession },
            { "/status", HandleStatus },
            { "/context", HandleContext },
        };
    }

    /// <summary>
    /// Check if input is a special command
    /// </summary>
    public bool IsSpecialCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith('/');
    }

    /// <summary>
    /// Try to handle a special command
    /// </summary>
    public async Task<SpecialCommandResult?> TryHandleAsync(string input)
    {
        if (!IsSpecialCommand(input))
            return null;

        var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        var command = parts[0].ToLowerInvariant();
        var args = parts.Skip(1).ToArray();

        if (_handlers.TryGetValue(command, out var handler))
        {
            _logger.LogInformation("Handling special command: {Command}", command);
            return await handler(args);
        }

        // Unknown command
        return new SpecialCommandResult
        {
            Handled = true,
            Message = $"Unknown command: {command}. Type /help for available commands."
        };
    }

    #region Command Handlers

    private Task<SpecialCommandResult> HandleHelp(string[] args)
    {
        var helpText = """
            📚 **SWAI Special Commands**

            **Session Commands:**
            • `/summarize` - Show session summary
            • `/history` - Show recent command history
            • `/clear` - Clear chat history
            • `/save-session` - Save current session
            • `/load-session <name>` - Load a saved session
            • `/sessions` - List saved sessions
            • `/new-session [name]` - Start a new session

            **Design Commands:**
            • `/list` or `/components` - List current parts and features
            • `/context` - Show current design context
            • `/status` - Show connection and system status
            • `/undo` - Undo the last command

            **CAD Commands:**
            Just type naturally! Examples:
            • "Create a box 10 x 20 x 5 inches"
            • "Add a 1 inch fillet"
            • "Save the part as Cabinet.sldprt"
            """;

        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = helpText
        });
    }

    private Task<SpecialCommandResult> HandleSummarize(string[] args)
    {
        var summary = _sessionManager?.GetSessionSummary() ?? "Session manager not available.";
        
        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = summary
        });
    }

    private Task<SpecialCommandResult> HandleList(string[] args)
    {
        var list = _sessionManager?.ListComponents() ?? "Session manager not available.";
        
        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = list
        });
    }

    private async Task<SpecialCommandResult> HandleUndo(string[] args)
    {
        if (_sessionManager == null)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Session manager not available."
            };
        }

        var lastCommand = _sessionManager.GetLastCommand();
        if (lastCommand == null || !lastCommand.CanUndo)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Nothing to undo."
            };
        }

        // Execute undo
        var undoCommand = new Core.Commands.UndoCommand();
        var result = await _commandExecutor.ExecuteAsync(undoCommand);

        if (result.Success)
        {
            _sessionManager.MarkCommandUndone(lastCommand.Id);
        }

        return new SpecialCommandResult
        {
            Handled = true,
            Message = result.Success 
                ? $"✓ Undone: {lastCommand.Description}" 
                : $"✗ Failed to undo: {result.Message}"
        };
    }

    private Task<SpecialCommandResult> HandleHistory(string[] args)
    {
        if (_sessionManager == null)
        {
            return Task.FromResult(new SpecialCommandResult
            {
                Handled = true,
                Message = "Session manager not available."
            });
        }

        var count = args.Length > 0 && int.TryParse(args[0], out var n) ? n : 10;
        var history = _sessionManager.CommandHistory.TakeLast(count);

        if (!history.Any())
        {
            return Task.FromResult(new SpecialCommandResult
            {
                Handled = true,
                Message = "No command history."
            });
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📜 Last {history.Count()} commands:");
        
        var index = 1;
        foreach (var cmd in history)
        {
            var status = cmd.IsUndone ? "↩️" : cmd.Success ? "✓" : "✗";
            sb.AppendLine($"  {index++}. {status} {cmd.Description} ({cmd.ExecutedAt:HH:mm})");
        }

        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = sb.ToString()
        });
    }

    private Task<SpecialCommandResult> HandleClear(string[] args)
    {
        _sessionManager?.ClearHistory();
        
        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = "Chat history cleared.",
            ClearChat = true
        });
    }

    private async Task<SpecialCommandResult> HandleSaveSession(string[] args)
    {
        if (_sessionManager == null)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Session manager not available."
            };
        }

        await _sessionManager.SaveAsync();
        
        return new SpecialCommandResult
        {
            Handled = true,
            Message = $"Session saved: {_sessionManager.CurrentSessionName}"
        };
    }

    private async Task<SpecialCommandResult> HandleLoadSession(string[] args)
    {
        if (_sessionManager == null)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Session manager not available."
            };
        }

        if (args.Length == 0)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Usage: /load-session <name or id>"
            };
        }

        var sessions = await _sessionManager.ListSessionsAsync();
        var target = args[0];

        var session = sessions.FirstOrDefault(s => 
            s.Name.Contains(target, StringComparison.OrdinalIgnoreCase) ||
            s.SessionId.ToString().StartsWith(target, StringComparison.OrdinalIgnoreCase));

        if (session == null)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = $"Session not found: {target}"
            };
        }

        await _sessionManager.LoadAsync(session.SessionId);
        
        return new SpecialCommandResult
        {
            Handled = true,
            Message = $"Loaded session: {session.Name}",
            RefreshChat = true
        };
    }

    private async Task<SpecialCommandResult> HandleListSessions(string[] args)
    {
        if (_sessionManager == null)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Session manager not available."
            };
        }

        var sessions = await _sessionManager.ListSessionsAsync();

        if (sessions.Count == 0)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "No saved sessions found."
            };
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📁 Saved Sessions ({sessions.Count}):");
        
        foreach (var session in sessions.Take(10))
        {
            var current = session.SessionId == _sessionManager.CurrentSessionId ? " ← current" : "";
            sb.AppendLine($"  • {session.Name} ({session.MessageCount} msgs, {session.ModifiedAt:g}){current}");
        }

        return new SpecialCommandResult
        {
            Handled = true,
            Message = sb.ToString()
        };
    }

    private async Task<SpecialCommandResult> HandleNewSession(string[] args)
    {
        if (_sessionManager == null)
        {
            return new SpecialCommandResult
            {
                Handled = true,
                Message = "Session manager not available."
            };
        }

        var name = args.Length > 0 ? string.Join(" ", args) : null;
        await _sessionManager.NewSessionAsync(name);
        
        return new SpecialCommandResult
        {
            Handled = true,
            Message = $"Started new session: {_sessionManager.CurrentSessionName}",
            ClearChat = true
        };
    }

    private Task<SpecialCommandResult> HandleStatus(string[] args)
    {
        var status = new System.Text.StringBuilder();
        status.AppendLine("📊 **System Status**");
        status.AppendLine($"  Session: {_sessionManager?.CurrentSessionName ?? "N/A"}");
        status.AppendLine($"  Messages: {_sessionManager?.History.Count ?? 0}");
        // Add more status info as needed
        
        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = status.ToString()
        });
    }

    private Task<SpecialCommandResult> HandleContext(string[] args)
    {
        var context = _sessionManager?.GetContextSummary() ?? "No context available.";
        
        return Task.FromResult(new SpecialCommandResult
        {
            Handled = true,
            Message = $"📐 Design Context:\n{context}"
        });
    }

    #endregion
}

/// <summary>
/// Result of handling a special command
/// </summary>
public class SpecialCommandResult
{
    /// <summary>
    /// Whether the command was handled
    /// </summary>
    public bool Handled { get; init; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Whether to clear the chat UI
    /// </summary>
    public bool ClearChat { get; init; }

    /// <summary>
    /// Whether to refresh the chat from session history
    /// </summary>
    public bool RefreshChat { get; init; }
}
