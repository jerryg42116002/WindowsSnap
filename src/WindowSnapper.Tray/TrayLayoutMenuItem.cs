namespace WindowSnapper.Tray;

/// <summary>
/// Describes a tray menu item for a layout.
/// </summary>
/// <param name="Id">The layout id.</param>
/// <param name="Name">The display name.</param>
/// <param name="Zones">The zones that can be selected.</param>
public sealed record TrayLayoutMenuItem(
    string Id,
    string Name,
    IReadOnlyList<TrayZoneMenuItem> Zones);
