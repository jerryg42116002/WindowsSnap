namespace WindowSnapper.Tray;

/// <summary>
/// Provides data for tray menu command events.
/// </summary>
public sealed class TrayMenuCommandEventArgs(TrayMenuCommand command) : EventArgs
{
    /// <summary>
    /// Gets the requested command.
    /// </summary>
    public TrayMenuCommand Command { get; } = command;
}
