namespace WindowSnapper.Hotkeys;

/// <summary>
/// Provides the default hotkey set for the MVP.
/// </summary>
public static class DefaultHotkeys
{
    /// <summary>
    /// Gets the default hotkey definitions.
    /// </summary>
    public static IReadOnlyList<HotkeyDefinition> All { get; } =
    [
        new(HotkeyCommand.SnapLeftHalf, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Left),
        new(HotkeyCommand.SnapRightHalf, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Right),
        new(HotkeyCommand.SnapTopHalf, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Up),
        new(HotkeyCommand.SnapBottomHalf, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Down),
        new(HotkeyCommand.SnapZone1, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.D1),
        new(HotkeyCommand.SnapZone2, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.D2),
        new(HotkeyCommand.SnapZone3, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.D3),
        new(HotkeyCommand.SnapZone4, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.D4),
        new(HotkeyCommand.OpenLayoutSelector, HotkeyModifiers.Control | HotkeyModifiers.Alt, HotkeyKey.Space)
    ];
}
