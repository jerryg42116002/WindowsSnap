namespace WindowSnapper.Tray;

/// <summary>
/// Provides data for tray menu command events.
/// </summary>
public sealed class TrayMenuCommandEventArgs(
    TrayMenuCommand command,
    string? layoutId = null,
    string? zoneId = null) : EventArgs
{
    /// <summary>
    /// Gets the requested command.
    /// </summary>
    public TrayMenuCommand Command { get; } = command;

    /// <summary>
    /// Gets the layout id for layout snap commands.
    /// </summary>
    public string? LayoutId { get; } = layoutId;

    /// <summary>
    /// Gets the zone id for layout snap commands.
    /// </summary>
    public string? ZoneId { get; } = zoneId;
}
