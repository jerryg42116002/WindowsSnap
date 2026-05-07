using WindowSnapper.Core.Results;

namespace WindowSnapper.Hotkeys.Tests;

public sealed class HotkeyParserTests
{
    [Fact]
    public void ParsesCtrlAltLeft()
    {
        var result = HotkeyParser.Parse("Ctrl+Alt+Left", HotkeyCommand.SnapLeftHalf);

        Assert.True(result.IsSuccess);
        Assert.Equal(HotkeyCommand.SnapLeftHalf, result.Value.Command);
        Assert.True(result.Value.Modifiers.HasFlag(HotkeyModifiers.Control));
        Assert.True(result.Value.Modifiers.HasFlag(HotkeyModifiers.Alt));
        Assert.Equal(HotkeyKey.Left, result.Value.Key);
    }

    [Fact]
    public void ParsesCtrlAltOne()
    {
        var result = HotkeyParser.Parse("Ctrl + Alt + 1", HotkeyCommand.SnapZone1);

        Assert.True(result.IsSuccess);
        Assert.Equal(HotkeyCommand.SnapZone1, result.Value.Command);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, result.Value.Modifiers);
        Assert.Equal(HotkeyKey.D1, result.Value.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+Alt+Mouse1")]
    [InlineData("Ctrl+Alt+Left+Right")]
    public void InvalidHotkeyStringReturnsFailure(string text)
    {
        var result = HotkeyParser.Parse(text, HotkeyCommand.SnapLeftHalf);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
    }
}
