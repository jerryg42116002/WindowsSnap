namespace WindowSnapper.Snap.Tests;

public sealed class SnapCommandTests
{
    [Theory]
    [InlineData(HotkeyCommand.SnapLeftHalf, BuiltinLayouts.LeftHalfId)]
    [InlineData(HotkeyCommand.SnapRightHalf, BuiltinLayouts.RightHalfId)]
    [InlineData(HotkeyCommand.SnapTopHalf, BuiltinLayouts.TopHalfId)]
    [InlineData(HotkeyCommand.SnapBottomHalf, BuiltinLayouts.BottomHalfId)]
    [InlineData(HotkeyCommand.SnapZone1, BuiltinLayouts.QuadTopLeftId)]
    [InlineData(HotkeyCommand.SnapZone2, BuiltinLayouts.QuadTopRightId)]
    [InlineData(HotkeyCommand.SnapZone3, BuiltinLayouts.QuadBottomLeftId)]
    [InlineData(HotkeyCommand.SnapZone4, BuiltinLayouts.QuadBottomRightId)]
    public void HotkeyCommandsMapToBuiltinLayoutZones(HotkeyCommand hotkeyCommand, string expectedLayoutId)
    {
        var result = SnapCommand.FromHotkeyCommand(hotkeyCommand);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedLayoutId, result.Value.LayoutId);
        Assert.Equal(BuiltinLayouts.MainZoneId, result.Value.ZoneId);
    }

    [Fact]
    public void OpenLayoutSelectorDoesNotMapToSnapTarget()
    {
        var result = SnapCommand.FromHotkeyCommand(HotkeyCommand.OpenLayoutSelector);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.NotSupported, result.ErrorCode);
    }
}
