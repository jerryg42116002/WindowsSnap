namespace WindowSnapper.Hotkeys;

/// <summary>
/// Identifies the action triggered by a hotkey.
/// </summary>
public enum HotkeyCommand
{
    None = 0,
    SnapLeftHalf = 1,
    SnapRightHalf = 2,
    SnapTopHalf = 3,
    SnapBottomHalf = 4,
    SnapZone1 = 5,
    SnapZone2 = 6,
    SnapZone3 = 7,
    SnapZone4 = 8,
    OpenLayoutSelector = 9
}
