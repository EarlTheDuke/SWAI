using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Records and plays back SolidWorks API calls for testing
/// </summary>
public class MockRecorder
{
    private readonly ILogger<MockRecorder> _logger;
    private readonly MockConfiguration _config;
    private readonly string _recordingsPath;
    private readonly List<RecordedCall> _currentRecording = new();
    private Dictionary<string, List<RecordedCall>>? _playbackData;
    private bool _isRecording;
    private bool _isPlayingBack;
    private int _playbackIndex;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public bool IsRecording => _isRecording;
    public bool IsPlayingBack => _isPlayingBack;

    public MockRecorder(MockConfiguration config, ILogger<MockRecorder> logger)
    {
        _config = config;
        _logger = logger;
        
        _recordingsPath = string.IsNullOrEmpty(config.RecordingsPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SWAI", "recordings")
            : config.RecordingsPath;
        
        Directory.CreateDirectory(_recordingsPath);
    }

    /// <summary>
    /// Start recording API calls
    /// </summary>
    public void StartRecording()
    {
        _currentRecording.Clear();
        _isRecording = true;
        _logger.LogInformation("Started recording API calls");
    }

    /// <summary>
    /// Stop recording and save to file
    /// </summary>
    public async Task<string> StopRecordingAsync(string? sessionName = null)
    {
        _isRecording = false;
        
        var filename = $"{sessionName ?? $"recording_{DateTime.Now:yyyyMMdd_HHmmss}"}.json";
        var filepath = Path.Combine(_recordingsPath, filename);
        
        var json = JsonSerializer.Serialize(_currentRecording, JsonOptions);
        await File.WriteAllTextAsync(filepath, json);
        
        _logger.LogInformation("Saved recording to {Path} ({Count} calls)", filepath, _currentRecording.Count);
        _currentRecording.Clear();
        
        return filepath;
    }

    /// <summary>
    /// Load a recording for playback
    /// </summary>
    public async Task LoadRecordingAsync(string filepath)
    {
        if (!File.Exists(filepath))
        {
            throw new FileNotFoundException("Recording file not found", filepath);
        }

        var json = await File.ReadAllTextAsync(filepath);
        var calls = JsonSerializer.Deserialize<List<RecordedCall>>(json, JsonOptions);
        
        if (calls == null || calls.Count == 0)
        {
            throw new InvalidDataException("Empty or invalid recording file");
        }

        // Group by method for quick lookup
        _playbackData = calls.GroupBy(c => c.Method)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        _isPlayingBack = true;
        _playbackIndex = 0;
        
        _logger.LogInformation("Loaded recording from {Path} ({Count} calls)", filepath, calls.Count);
    }

    /// <summary>
    /// Stop playback mode
    /// </summary>
    public void StopPlayback()
    {
        _isPlayingBack = false;
        _playbackData = null;
        _playbackIndex = 0;
    }

    /// <summary>
    /// Record an API call
    /// </summary>
    public void RecordCall(string method, object? request, object? response, bool success, TimeSpan duration)
    {
        if (!_isRecording) return;

        _currentRecording.Add(new RecordedCall
        {
            Method = method,
            Request = request,
            Response = response,
            Success = success,
            DurationMs = (int)duration.TotalMilliseconds,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get a recorded response for playback
    /// </summary>
    public RecordedCall? GetPlaybackResponse(string method)
    {
        if (!_isPlayingBack || _playbackData == null)
            return null;

        if (_playbackData.TryGetValue(method, out var calls) && calls.Count > 0)
        {
            // Return calls in order, cycling if needed
            var index = _playbackIndex % calls.Count;
            _playbackIndex++;
            return calls[index];
        }

        return null;
    }

    /// <summary>
    /// List available recordings
    /// </summary>
    public IEnumerable<RecordingInfo> ListRecordings()
    {
        var files = Directory.GetFiles(_recordingsPath, "*.json");
        
        foreach (var file in files)
        {
            var info = new FileInfo(file);
            yield return new RecordingInfo
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Path = file,
                Size = info.Length,
                CreatedAt = info.CreationTime,
                ModifiedAt = info.LastWriteTime
            };
        }
    }
}

/// <summary>
/// A recorded API call
/// </summary>
public class RecordedCall
{
    public string Method { get; set; } = string.Empty;
    public object? Request { get; set; }
    public object? Response { get; set; }
    public bool Success { get; set; }
    public int DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Information about a recording file
/// </summary>
public class RecordingInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}
