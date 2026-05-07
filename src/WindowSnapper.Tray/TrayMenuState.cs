namespace WindowSnapper.Tray;

/// <summary>
/// Describes the tray menu state.
/// </summary>
/// <param name="HotkeysPaused">Whether global hotkeys are paused.</param>
public sealed record TrayMenuState(bool HotkeysPaused);
