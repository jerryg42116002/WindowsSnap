namespace WindowSnapper.Tray;

/// <summary>
/// Describes the tray menu state.
/// </summary>
/// <param name="HotkeysPaused">Whether global hotkeys are paused.</param>
/// <param name="Layouts">The layouts available to snap from the tray.</param>
public sealed record TrayMenuState(
    bool HotkeysPaused,
    IReadOnlyList<TrayLayoutMenuItem>? Layouts = null);
