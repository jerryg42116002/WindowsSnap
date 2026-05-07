using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Core.Monitors;

/// <summary>
/// Provides monitor discovery without exposing platform-specific structures.
/// </summary>
public interface IMonitorManager
{
    /// <summary>
    /// Gets the monitors currently available to the desktop session.
    /// </summary>
    Result<IReadOnlyList<MonitorInfo>> GetMonitors();

    /// <summary>
    /// Gets the monitor containing the specified window.
    /// </summary>
    Result<MonitorInfo> GetMonitorForWindow(WindowHandle handle);

    /// <summary>
    /// Gets the monitor containing the specified point.
    /// </summary>
    Result<MonitorInfo> GetMonitorForPoint(PointInt point);
}
