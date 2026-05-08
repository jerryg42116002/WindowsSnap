namespace WindowSnapper.Tray;

/// <summary>
/// Identifies a command requested from the tray menu.
/// </summary>
public enum TrayMenuCommand
{
    OpenMainWindow = 1,
    OpenSettings = 2,
    ToggleHotkeysPaused = 3,
    SnapLayoutZone = 4,
    SaveWorkspaceSnapshot = 5,
    RestoreLatestWorkspaceSnapshot = 6,
    Exit = 7
}
