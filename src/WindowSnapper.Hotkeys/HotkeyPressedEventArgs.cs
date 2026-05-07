namespace WindowSnapper.Hotkeys;

/// <summary>
/// Provides data for a pressed hotkey event.
/// </summary>
/// <param name="Definition">The pressed hotkey definition.</param>
public sealed class HotkeyPressedEventArgs(HotkeyDefinition definition) : EventArgs
{
    /// <summary>
    /// Gets the pressed hotkey definition.
    /// </summary>
    public HotkeyDefinition Definition { get; } = definition;

    /// <summary>
    /// Gets the command triggered by the hotkey.
    /// </summary>
    public HotkeyCommand Command => Definition.Command;
}
