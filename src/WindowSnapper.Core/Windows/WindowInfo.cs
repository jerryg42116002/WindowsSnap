using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Core.Windows;

/// <summary>
/// Describes a desktop window at a point in time.
/// </summary>
/// <param name="Handle">The window handle.</param>
/// <param name="Title">The window title, when available.</param>
/// <param name="ProcessName">The owning process name, when available.</param>
/// <param name="ClassName">The platform window class name, when available.</param>
/// <param name="Bounds">The current window bounds.</param>
/// <param name="IsVisible">Whether the window is visible.</param>
/// <param name="IsMinimized">Whether the window is minimized.</param>
/// <param name="IsMaximized">Whether the window is maximized.</param>
public sealed record WindowInfo(
    WindowHandle Handle,
    string Title,
    string ProcessName,
    string ClassName,
    RectInt Bounds,
    bool IsVisible,
    bool IsMinimized,
    bool IsMaximized);
