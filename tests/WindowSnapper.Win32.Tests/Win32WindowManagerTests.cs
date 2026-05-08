using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Win32.Tests;

public sealed class Win32WindowManagerTests
{
    [Fact]
    public void CalculateOuterBoundsForVisibleTargetCompensatesInvisibleResizeBorder()
    {
        var outerBounds = new RectInt(-8, -8, 1936, 1096);
        var visibleBounds = new RectInt(0, 0, 1920, 1080);
        var targetVisibleBounds = new RectInt(960, 0, 960, 1080);

        var result = Win32WindowManager.CalculateOuterBoundsForVisibleTarget(
            outerBounds,
            visibleBounds,
            targetVisibleBounds);

        Assert.Equal(new RectInt(952, -8, 976, 1096), result);
    }

    [Fact]
    public void CalculateOuterBoundsForVisibleTargetKeepsBoundsWhenNoInvisibleBorderExists()
    {
        var bounds = new RectInt(0, 0, 960, 1080);

        var result = Win32WindowManager.CalculateOuterBoundsForVisibleTarget(bounds, bounds, bounds);

        Assert.Equal(bounds, result);
    }
}
