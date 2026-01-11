using SWAI.Core.Interfaces;
using System.Text;
using System.Text.Json;

namespace SWAI.Core.Services;

/// <summary>
/// Manages conversation sessions and chat history
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly List<ChatMessage> _history = new();
    private readonly int _maxHistoryItems;

    public Guid SessionId { get; private set; } = Guid.NewGuid();

    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    public SessionManager(int maxHistoryItems = 100)
    {
        _maxHistoryItems = maxHistoryItems;
    }

    public void AddMessage(ChatMessage message)
    {
        _history.Add(message);

        // Trim history if it exceeds max
        while (_history.Count > _maxHistoryItems)
        {
            _history.RemoveAt(0);
        }
    }

    public void ClearHistory()
    {
        _history.Clear();
    }

    public void NewSession()
    {
        SessionId = Guid.NewGuid();
        _history.Clear();
    }

    public string ExportHistory()
    {
        var export = new
        {
            SessionId,
            ExportedAt = DateTime.UtcNow,
            MessageCount = _history.Count,
            Messages = _history.Select(m => new
            {
                m.Role,
                m.Content,
                m.Timestamp,
                m.IsError,
                CommandType = m.AssociatedCommand?.CommandType
            })
        };

        return JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Get a summary of the conversation for context
    /// </summary>
    public string GetContextSummary(int maxMessages = 10)
    {
        var recentMessages = _history.TakeLast(maxMessages);
        var sb = new StringBuilder();

        foreach (var msg in recentMessages)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get messages formatted for LLM context
    /// </summary>
    public IEnumerable<(string Role, string Content)> GetMessagesForContext(int maxMessages = 10)
    {
        return _history
            .TakeLast(maxMessages)
            .Select(m => (m.Role, m.Content));
    }
}
