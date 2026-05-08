using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Core.Windows;

/// <summary>
/// Provides window operations without exposing platform-specific APIs.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Gets the window currently receiving foreground input.
    /// </summary>
    Result<WindowHandle> GetActiveWindow();

    /// <summary>
    /// Gets current metadata for the specified window.
    /// </summary>
    Result<WindowInfo> GetWindowInfo(WindowHandle handle);

    /// <summary>
    /// Determines whether the specified window should be managed.
    /// </summary>
    Result<bool> IsWindowManageable(WindowInfo windowInfo);

    /// <summary>
    /// Restores the specified window when it is maximized or minimized.
    /// </summary>
    Result RestoreWindow(WindowHandle handle);

    /// <summary>
    /// Minimizes the specified window.
    /// </summary>
    Result MinimizeWindow(WindowHandle handle);

    /// <summary>
    /// Hides the specified window.
    /// </summary>
    Result HideWindow(WindowHandle handle);

    /// <summary>
    /// Moves and resizes the specified window to the target bounds.
    /// </summary>
    Result MoveWindow(WindowHandle handle, RectInt targetBounds);
}
