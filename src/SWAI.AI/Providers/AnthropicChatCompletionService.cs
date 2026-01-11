using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWAI.AI.Providers;

/// <summary>
/// Anthropic Claude chat completion service adapter for Semantic Kernel
/// Uses the Anthropic Messages API directly via HTTP
/// </summary>
public class AnthropicChatCompletionService : IChatCompletionService
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
    {
        ["ModelId"] = _model,
        ["Provider"] = "Anthropic"
    };

    public AnthropicChatCompletionService(
        string apiKey,
        string model,
        ILogger logger,
        HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _model = model;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        
        // Set default headers
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", ApiVersion);
    }

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(chatHistory, executionSettings);
        
        _logger.LogDebug("Sending request to Anthropic: {Model}", _model);

        var response = await SendRequestAsync(request, cancellationToken);
        
        var content = ExtractContent(response);
        
        return new List<ChatMessageContent>
        {
            new ChatMessageContent(AuthorRole.Assistant, content)
        };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(chatHistory, executionSettings);
        request.Stream = true;

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var httpContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = httpContent
        };

        var httpResponse = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        await using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line[6..]; // Remove "data: " prefix
            
            if (data == "[DONE]")
                break;

            var streamEvent = JsonSerializer.Deserialize<AnthropicStreamEvent>(data, JsonOptions);
            
            if (streamEvent?.Type == "content_block_delta" && streamEvent.Delta?.Text != null)
            {
                yield return new StreamingChatMessageContent(
                    AuthorRole.Assistant,
                    streamEvent.Delta.Text);
            }
        }
    }

    #region Private Methods

    private AnthropicRequest BuildRequest(ChatHistory chatHistory, PromptExecutionSettings? settings)
    {
        var messages = new List<AnthropicMessage>();
        string? systemPrompt = null;

        foreach (var message in chatHistory)
        {
            if (message.Role == AuthorRole.System)
            {
                systemPrompt = message.Content;
            }
            else
            {
                messages.Add(new AnthropicMessage
                {
                    Role = message.Role == AuthorRole.User ? "user" : "assistant",
                    Content = message.Content ?? string.Empty
                });
            }
        }

        // Ensure alternating user/assistant messages (Anthropic requirement)
        messages = EnsureAlternatingMessages(messages);

        return new AnthropicRequest
        {
            Model = _model,
            Messages = messages,
            System = systemPrompt,
            MaxTokens = 4096,
            Temperature = settings is OpenAIPromptExecutionSettings oaiSettings 
                ? oaiSettings.Temperature ?? 0.7 
                : 0.7
        };
    }

    private List<AnthropicMessage> EnsureAlternatingMessages(List<AnthropicMessage> messages)
    {
        var result = new List<AnthropicMessage>();
        string? lastRole = null;

        foreach (var msg in messages)
        {
            if (msg.Role == lastRole)
            {
                // Merge with previous message of same role
                if (result.Count > 0)
                {
                    result[^1].Content += "\n" + msg.Content;
                }
            }
            else
            {
                result.Add(msg);
                lastRole = msg.Role;
            }
        }

        // Anthropic requires first message to be from user
        if (result.Count > 0 && result[0].Role != "user")
        {
            result.Insert(0, new AnthropicMessage { Role = "user", Content = "Hello" });
        }

        return result;
    }

    private async Task<AnthropicResponse> SendRequestAsync(
        AnthropicRequest request,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(BaseUrl, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Anthropic API error: {StatusCode} - {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Anthropic API error: {response.StatusCode} - {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from Anthropic");
    }

    private string ExtractContent(AnthropicResponse response)
    {
        var textContent = response.Content?
            .Where(c => c.Type == "text")
            .Select(c => c.Text)
            .FirstOrDefault();

        return textContent ?? string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion

    #region Request/Response Models

    private class AnthropicRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<AnthropicMessage> Messages { get; set; } = new();
        public string? System { get; set; }
        public int MaxTokens { get; set; } = 4096;
        public double? Temperature { get; set; }
        public bool Stream { get; set; }
    }

    private class AnthropicMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class AnthropicResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<ContentBlock>? Content { get; set; }
        public string? StopReason { get; set; }
        public UsageInfo? Usage { get; set; }
    }

    private class ContentBlock
    {
        public string Type { get; set; } = string.Empty;
        public string? Text { get; set; }
    }

    private class UsageInfo
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }

    private class AnthropicStreamEvent
    {
        public string Type { get; set; } = string.Empty;
        public int? Index { get; set; }
        public DeltaContent? Delta { get; set; }
    }

    private class DeltaContent
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

    #endregion
}
