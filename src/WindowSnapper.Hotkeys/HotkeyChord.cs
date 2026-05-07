namespace WindowSnapper.Hotkeys;

internal readonly record struct HotkeyChord(HotkeyModifiers Modifiers, HotkeyKey Key)
{
    public string ChordText => HotkeyParser.FormatChord(Modifiers, Key);

    public static HotkeyChord FromDefinition(HotkeyDefinition definition)
    {
        return new HotkeyChord(definition.Modifiers, definition.Key);
    }
}
