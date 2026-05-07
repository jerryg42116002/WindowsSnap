namespace WindowSnapper.Tray;

/// <summary>
/// Provides a system tray icon without exposing UI framework details to callers.
/// </summary>
public interface ITrayIcon : IDisposable
{
    /// <summary>
    /// Raised when the user selects a tray menu command.
    /// </summary>
    event EventHandler<TrayMenuCommandEventArgs>? CommandRequested;

    /// <summary>
    /// Shows the tray icon.
    /// </summary>
    void Show(TrayMenuState state);

    /// <summary>
    /// Updates tray menu labels and state.
    /// </summary>
    void UpdateState(TrayMenuState state);
}
