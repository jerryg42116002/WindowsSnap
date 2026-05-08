using WindowSnapper.Core.Time;

namespace WindowSnapper.Snap.Tests;

public sealed class RepeatHotkeyCycleServiceTests
{
    private static readonly WindowHandle FirstWindow = WindowHandle.FromIntPtr(new IntPtr(100));
    private static readonly WindowHandle SecondWindow = WindowHandle.FromIntPtr(new IntPtr(200));

    [Fact]
    public void SameWindowAndCommandIncrementCycleIndexWithinResetWindow()
    {
        var clock = new FakeClock();
        var service = new RepeatHotkeyCycleService(clock);

        var first = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);
        clock.Advance(TimeSpan.FromSeconds(1));
        var third = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);

        AssertSelection(first, 0, BuiltinLayouts.RightHalfId);
        AssertSelection(second, 1, BuiltinLayouts.RightOneThirdId);
        AssertSelection(third, 2, BuiltinLayouts.RightTwoThirdsId);
    }

    [Fact]
    public void CycleIndexResetsAfterResetWindowExpires()
    {
        var clock = new FakeClock();
        var service = new RepeatHotkeyCycleService(clock);

        var first = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);
        clock.Advance(TimeSpan.FromMilliseconds(1501));
        var second = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);

        AssertSelection(first, 0, BuiltinLayouts.RightHalfId);
        AssertSelection(second, 0, BuiltinLayouts.RightHalfId);
    }

    [Fact]
    public void CycleIndexResetsWhenWindowChanges()
    {
        var clock = new FakeClock();
        var service = new RepeatHotkeyCycleService(clock);

        var first = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = service.Select(HotkeyCommand.SnapRightHalf, SecondWindow);

        AssertSelection(first, 0, BuiltinLayouts.RightHalfId);
        AssertSelection(second, 0, BuiltinLayouts.RightHalfId);
    }

    [Fact]
    public void CycleIndexResetsWhenCommandChanges()
    {
        var clock = new FakeClock();
        var service = new RepeatHotkeyCycleService(clock);

        var first = service.Select(HotkeyCommand.SnapRightHalf, FirstWindow);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = service.Select(HotkeyCommand.SnapLeftHalf, FirstWindow);

        AssertSelection(first, 0, BuiltinLayouts.RightHalfId);
        AssertSelection(second, 0, BuiltinLayouts.LeftHalfId);
    }

    [Fact]
    public void CycleIndexWrapsToBeginningAfterLastTarget()
    {
        var clock = new FakeClock();
        var service = new RepeatHotkeyCycleService(clock);

        var selections = new List<Result<RepeatHotkeyCycleSelection>>();
        for (var index = 0; index < 6; index++)
        {
            selections.Add(service.Select(HotkeyCommand.SnapRightHalf, FirstWindow));
            clock.Advance(TimeSpan.FromMilliseconds(100));
        }

        AssertSelection(selections[0], 0, BuiltinLayouts.RightHalfId);
        AssertSelection(selections[1], 1, BuiltinLayouts.RightOneThirdId);
        AssertSelection(selections[2], 2, BuiltinLayouts.RightTwoThirdsId);
        AssertSelection(selections[3], 3, BuiltinLayouts.QuadTopRightId);
        AssertSelection(selections[4], 4, BuiltinLayouts.QuadBottomRightId);
        AssertSelection(selections[5], 0, BuiltinLayouts.RightHalfId);
    }

    private static void AssertSelection(
        Result<RepeatHotkeyCycleSelection> result,
        int expectedCycleIndex,
        string expectedLayoutId)
    {
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCycleIndex, result.Value.CycleIndex);
        Assert.Equal(expectedLayoutId, result.Value.Command.LayoutId);
        Assert.Equal(BuiltinLayouts.MainZoneId, result.Value.Command.ZoneId);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);

        public DateTimeOffset LocalNow => UtcNow.ToLocalTime();

        public void Advance(TimeSpan elapsed)
        {
            UtcNow += elapsed;
        }
    }
}
