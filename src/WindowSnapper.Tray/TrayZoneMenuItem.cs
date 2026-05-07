namespace WindowSnapper.Tray;

/// <summary>
/// Describes a tray menu item for a layout zone.
/// </summary>
/// <param name="Id">The zone id.</param>
/// <param name="Name">The display name.</param>
public sealed record TrayZoneMenuItem(string Id, string Name);
