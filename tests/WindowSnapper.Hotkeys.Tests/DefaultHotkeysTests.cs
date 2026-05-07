namespace WindowSnapper.Hotkeys.Tests;

public sealed class DefaultHotkeysTests
{
    [Fact]
    public void DefaultHotkeysContainRequiredCommands()
    {
        var commands = DefaultHotkeys.All.Select(hotkey => hotkey.Command).ToArray();

        Assert.Contains(HotkeyCommand.SnapLeftHalf, commands);
        Assert.Contains(HotkeyCommand.SnapRightHalf, commands);
        Assert.Contains(HotkeyCommand.SnapTopHalf, commands);
        Assert.Contains(HotkeyCommand.SnapBottomHalf, commands);
        Assert.Contains(HotkeyCommand.SnapZone1, commands);
        Assert.Contains(HotkeyCommand.SnapZone2, commands);
        Assert.Contains(HotkeyCommand.SnapZone3, commands);
        Assert.Contains(HotkeyCommand.SnapZone4, commands);
        Assert.Contains(HotkeyCommand.OpenLayoutSelector, commands);
    }

    [Fact]
    public void DefaultHotkeysHaveUniqueChords()
    {
        var chords = DefaultHotkeys.All.Select(hotkey => hotkey.ChordText).ToArray();

        Assert.Equal(chords.Length, chords.Distinct(StringComparer.Ordinal).Count());
    }
}
