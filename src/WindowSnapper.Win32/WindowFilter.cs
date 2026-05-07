using System.Diagnostics;
using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Win32;

/// <summary>
/// Applies WindowSnapper's default manageable-window rules.
/// </summary>
public sealed class WindowFilter
{
    private static readonly string[] DefaultIgnoredClassNames =
    [
        "Shell_TrayWnd",
        "Progman",
        "WorkerW",
        "DV2ControlHost",
        "MsgrIMEWindowClass",
        "WindowSnapperOverlayWindow"
    ];

    private readonly HashSet<string> ignoredClassNames;
    private readonly HashSet<string> ignoredProcessNames;
    private readonly int currentProcessId;
    private readonly string currentProcessName;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowFilter"/> class.
    /// </summary>
    public WindowFilter()
        : this(Process.GetCurrentProcess().ProcessName, Environment.ProcessId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowFilter"/> class.
    /// </summary>
    public WindowFilter(
        string currentProcessName,
        int currentProcessId,
        IEnumerable<string>? ignoredClassNames = null,
        IEnumerable<string>? ignoredProcessNames = null)
    {
        this.currentProcessName = currentProcessName;
        this.currentProcessId = currentProcessId;
        this.ignoredClassNames = new HashSet<string>(
            ignoredClassNames ?? DefaultIgnoredClassNames,
            StringComparer.Ordinal);
        this.ignoredProcessNames = new HashSet<string>(
            ignoredProcessNames ?? [],
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Evaluates whether a Core window model is manageable.
    /// </summary>
    public Result<bool> IsWindowManageable(WindowInfo windowInfo)
    {
        ArgumentNullException.ThrowIfNull(windowInfo);

        var input = new WindowFilterInfo(
            windowInfo.Handle,
            windowInfo.ProcessName,
            ProcessId: null,
            windowInfo.ClassName,
            windowInfo.Bounds,
            windowInfo.IsVisible,
            windowInfo.IsMinimized);

        return Result<bool>.Success(IsWindowManageable(input));
    }

    /// <summary>
    /// Evaluates whether a window is manageable using Win32-specific metadata.
    /// </summary>
    public bool IsWindowManageable(WindowFilterInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (window.Handle.IsNone)
        {
            return false;
        }

        if (!window.IsVisible || window.IsMinimized)
        {
            return false;
        }

        if (window.Bounds.Width <= 0 || window.Bounds.Height <= 0)
        {
            return false;
        }

        if (ignoredClassNames.Contains(window.ClassName))
        {
            return false;
        }

        if (window.ProcessId == currentProcessId)
        {
            return false;
        }

        if (string.Equals(window.ProcessName, currentProcessName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (ignoredProcessNames.Contains(window.ProcessName))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether a window appears to cover the full monitor bounds.
    /// </summary>
    public static bool CoversMonitorBounds(WindowInfo windowInfo, MonitorInfo monitorInfo)
    {
        ArgumentNullException.ThrowIfNull(windowInfo);
        ArgumentNullException.ThrowIfNull(monitorInfo);

        return windowInfo.Bounds.X <= monitorInfo.Bounds.X &&
            windowInfo.Bounds.Y <= monitorInfo.Bounds.Y &&
            windowInfo.Bounds.Right >= monitorInfo.Bounds.Right &&
            windowInfo.Bounds.Bottom >= monitorInfo.Bounds.Bottom;
    }
}
