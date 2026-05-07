using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Core.Monitors;

/// <summary>
/// Describes a physical or virtual monitor.
/// </summary>
/// <param name="Id">A stable identifier when the platform can provide one.</param>
/// <param name="DeviceName">The platform display device name.</param>
/// <param name="Bounds">The full monitor bounds.</param>
/// <param name="WorkArea">The usable work area, excluding taskbars and reserved areas.</param>
/// <param name="IsPrimary">Whether this is the primary monitor.</param>
/// <param name="DpiScale">The effective DPI scale for this monitor.</param>
public sealed record MonitorInfo(
    string Id,
    string DeviceName,
    RectInt Bounds,
    RectInt WorkArea,
    bool IsPrimary,
    double DpiScale);
