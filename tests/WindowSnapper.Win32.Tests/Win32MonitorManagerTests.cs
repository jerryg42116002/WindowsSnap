using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Win32.Tests;

public sealed class Win32MonitorManagerTests
{
    [Fact]
    public void SelectMonitorContainingPointSupportsNegativeCoordinates()
    {
        var monitors = new[]
        {
            new MonitorInfo("primary", "DISPLAY1", new RectInt(0, 0, 1920, 1080), new RectInt(0, 0, 1920, 1040), true, 1),
            new MonitorInfo("left", "DISPLAY2", new RectInt(-1280, 0, 1280, 1024), new RectInt(-1280, 0, 1280, 984), false, 1)
        };

        var result = Win32MonitorManager.SelectMonitorContainingPoint(monitors, new PointInt(-100, 100));

        Assert.True(result.IsSuccess);
        Assert.Equal("left", result.Value.Id);
    }

    [Fact]
    public void SelectMonitorContainingPointReturnsNotFoundWhenNoMonitorContainsPoint()
    {
        var monitors = new[]
        {
            new MonitorInfo("primary", "DISPLAY1", new RectInt(0, 0, 1920, 1080), new RectInt(0, 0, 1920, 1040), true, 1)
        };

        var result = Win32MonitorManager.SelectMonitorContainingPoint(monitors, new PointInt(-100, 100));

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.NotFound, result.ErrorCode);
    }
}
