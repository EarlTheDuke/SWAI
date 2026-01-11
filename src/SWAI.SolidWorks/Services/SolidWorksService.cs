using Microsoft.Extensions.Logging;
using SWAI.Core.Configuration;
using SWAI.Core.Interfaces;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SWAI.SolidWorks.Services;

/// <summary>
/// Main SolidWorks connection service
/// </summary>
public class SolidWorksService : ISolidWorksService, IDisposable
{
    private readonly ILogger<SolidWorksService> _logger;
    private readonly SolidWorksConfiguration _config;
    private dynamic? _swApp;
    private bool _disposed;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public event EventHandler<ConnectionStatus>? StatusChanged;

    public SolidWorksService(SolidWorksConfiguration config, ILogger<SolidWorksService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(bool startIfNotRunning = true)
    {
        if (_config.UseMock)
        {
            _logger.LogInformation("Using mock SolidWorks service");
            SetStatus(ConnectionStatus.Connected);
            return true;
        }

        return await Task.Run(() =>
        {
            try
            {
                SetStatus(ConnectionStatus.Connecting);

                // Try to get running instance first
                try
                {
                    _swApp = GetActiveComObject("SldWorks.Application");
                    if (_swApp != null)
                    {
                        _logger.LogInformation("Connected to existing SolidWorks instance");
                        SetStatus(ConnectionStatus.Connected);
                        return true;
                    }
                }
                catch (COMException)
                {
                    _logger.LogInformation("No running SolidWorks instance found");
                }

                // Start new instance if requested
                if (startIfNotRunning)
                {
                    _logger.LogInformation("Starting new SolidWorks instance...");
                    var swType = Type.GetTypeFromProgID("SldWorks.Application");
                    if (swType == null)
                    {
                        _logger.LogError("SolidWorks is not installed or registered");
                        SetStatus(ConnectionStatus.Error);
                        return false;
                    }

                    _swApp = Activator.CreateInstance(swType);
                    if (_swApp != null)
                    {
                        if (_config.StartVisible)
                        {
                            _swApp.Visible = true;
                        }
                        _logger.LogInformation("SolidWorks started successfully");
                        SetStatus(ConnectionStatus.Connected);
                        return true;
                    }
                }

                SetStatus(ConnectionStatus.Disconnected);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SolidWorks");
                SetStatus(ConnectionStatus.Error);
                return false;
            }
        });
    }

    public async Task DisconnectAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (_swApp != null)
                {
                    // Don't close SW, just release our reference
                    Marshal.ReleaseComObject(_swApp);
                    _swApp = null;
                }
                SetStatus(ConnectionStatus.Disconnected);
                _logger.LogInformation("Disconnected from SolidWorks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from SolidWorks");
            }
        });
    }

    public async Task<SolidWorksInfo?> GetInfoAsync()
    {
        if (_config.UseMock)
        {
            return new SolidWorksInfo(
                Version: "2025 (Mock)",
                RevisionNumber: 33,
                IsRunning: true,
                InstallPath: @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS"
            );
        }

        if (_swApp == null)
            return null;

        return await Task.Run(() =>
        {
            try
            {
                var revision = (int)_swApp.RevisionNumber();
                var version = GetVersionFromRevision(revision);

                return new SolidWorksInfo(
                    Version: version,
                    RevisionNumber: revision,
                    IsRunning: true,
                    InstallPath: _config.InstallPath ?? @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get SolidWorks info");
                return null;
            }
        });
    }

    public async Task<bool> IsRunningAsync()
    {
        if (_config.UseMock)
            return true;

        return await Task.Run(() =>
        {
            try
            {
                var obj = GetActiveComObject("SldWorks.Application");
                if (obj != null)
                {
                    Marshal.ReleaseComObject(obj);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        });
    }

    /// <summary>
    /// Gets an active COM object by ProgID (replacement for Marshal.GetActiveObject in .NET Core)
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static object? GetActiveComObject(string progId)
    {
        try
        {
            var clsid = CLSIDFromProgID(progId);
            if (clsid == Guid.Empty) return null;
            
            var hr = Ole32GetActiveObject(ref clsid, IntPtr.Zero, out var obj);
            if (hr == 0)
                return obj;
            return null;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("ole32.dll", EntryPoint = "GetActiveObject")]
    private static extern int Ole32GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    private static Guid CLSIDFromProgID(string progId)
    {
        var type = Type.GetTypeFromProgID(progId);
        return type?.GUID ?? Guid.Empty;
    }

    /// <summary>
    /// Get the underlying SolidWorks application object
    /// </summary>
    internal dynamic? GetApplication() => _swApp;

    private void SetStatus(ConnectionStatus status)
    {
        if (Status != status)
        {
            Status = status;
            StatusChanged?.Invoke(this, status);
        }
    }

    private string GetVersionFromRevision(int revision)
    {
        // SolidWorks revision numbers
        return revision switch
        {
            >= 33 => "2025",
            32 => "2024",
            31 => "2023",
            30 => "2022",
            29 => "2021",
            28 => "2020",
            27 => "2019",
            26 => "2018",
            _ => $"Unknown ({revision})"
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Managed resources
            }

            // Release COM object
            if (_swApp != null)
            {
                try
                {
                    Marshal.ReleaseComObject(_swApp);
                }
                catch { }
                _swApp = null;
            }

            _disposed = true;
        }
    }

    ~SolidWorksService()
    {
        Dispose(false);
    }
}
