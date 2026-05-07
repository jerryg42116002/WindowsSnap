using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;

namespace WindowSnapper.Layouts.Tests;

public sealed class LayoutEngineTests
{
    private readonly LayoutEngine engine = new();

    [Fact]
    public void CalculatesLeftHalfFor1920x1080WorkArea()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var layout = CreateLayout(new ZoneDefinition("left", "Left", 0, 0, 0.5, 1));

        var result = engine.CalculateTargetRect(monitor, layout, "left");

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(0, 0, 960, 1080), result.Value);
    }

    [Fact]
    public void CalculatesRightHalfFor1920x1080WorkArea()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var layout = CreateLayout(new ZoneDefinition("right", "Right", 0.5, 0, 0.5, 1));

        var result = engine.CalculateTargetRect(monitor, layout, "right");

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(960, 0, 960, 1080), result.Value);
    }

    [Fact]
    public void CalculatesZoneWithMargin()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var layout = CreateLayout(
            new ZoneDefinition("left", "Left", 0, 0, 0.5, 1),
            margin: 8);

        var result = engine.CalculateTargetRect(monitor, layout, "left");

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(8, 8, 952, 1064), result.Value);
    }

    [Fact]
    public void CalculatesAdjacentZonesWithGap()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var layout = CreateLayout(
            [
                new ZoneDefinition("left", "Left", 0, 0, 0.5, 1),
                new ZoneDefinition("right", "Right", 0.5, 0, 0.5, 1)
            ],
            gap: 8);

        var left = engine.CalculateTargetRect(monitor, layout, "left");
        var right = engine.CalculateTargetRect(monitor, layout, "right");

        Assert.True(left.IsSuccess);
        Assert.True(right.IsSuccess);
        Assert.Equal(new RectInt(0, 0, 956, 1080), left.Value);
        Assert.Equal(new RectInt(964, 0, 956, 1080), right.Value);
        Assert.Equal(8, right.Value.X - left.Value.Right);
    }

    [Fact]
    public void CalculatesZoneOnNegativeCoordinateMonitor()
    {
        var monitor = CreateMonitor(new RectInt(-1920, 0, 1920, 1080));
        var layout = CreateLayout(new ZoneDefinition("left", "Left", 0, 0, 0.5, 1));

        var result = engine.CalculateTargetRect(monitor, layout, "left");

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(-1920, 0, 960, 1080), result.Value);
    }

    [Fact]
    public void CalculatesZoneOnPortraitMonitor()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1080, 1920));
        var layout = CreateLayout(new ZoneDefinition("top", "Top", 0, 0, 1, 0.5));

        var result = engine.CalculateTargetRect(monitor, layout, "top");

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(0, 0, 1080, 960), result.Value);
    }

    [Fact]
    public void UsesWorkAreaInsteadOfBounds()
    {
        var monitor = new MonitorInfo(
            "primary",
            "DISPLAY1",
            new RectInt(0, 0, 1920, 1080),
            new RectInt(0, 0, 1920, 1040),
            IsPrimary: true,
            DpiScale: 1);
        var layout = CreateLayout(new ZoneDefinition("bottom", "Bottom", 0, 0.5, 1, 0.5));

        var result = engine.CalculateTargetRect(monitor, layout, "bottom");

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(0, 520, 1920, 520), result.Value);
    }

    private static MonitorInfo CreateMonitor(RectInt workArea)
    {
        return new MonitorInfo("monitor", "DISPLAY", workArea, workArea, IsPrimary: true, DpiScale: 1);
    }

    private static LayoutDefinition CreateLayout(ZoneDefinition zone, int margin = 0, int gap = 0)
    {
        return CreateLayout([zone], margin, gap);
    }

    private static LayoutDefinition CreateLayout(IReadOnlyList<ZoneDefinition> zones, int margin = 0, int gap = 0)
    {
        return new LayoutDefinition("test-layout", "Test Layout", Version: 1, gap, margin, zones);
    }
}
