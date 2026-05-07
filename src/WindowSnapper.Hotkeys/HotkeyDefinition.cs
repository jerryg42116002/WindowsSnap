namespace WindowSnapper.Hotkeys;

/// <summary>
/// Defines a command and the key chord that triggers it.
/// </summary>
/// <param name="Command">The command triggered by the hotkey.</param>
/// <param name="Modifiers">The modifier keys.</param>
/// <param name="Key">The non-modifier key.</param>
public sealed record HotkeyDefinition(
    HotkeyCommand Command,
    HotkeyModifiers Modifiers,
    HotkeyKey Key)
{
    /// <summary>
    /// Gets a stable text representation of the key chord.
    /// </summary>
    public string ChordText => HotkeyParser.FormatChord(Modifiers, Key);
}
