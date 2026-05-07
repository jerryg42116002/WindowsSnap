using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Win32.Tests;

public sealed class WindowFilterTests
{
    private readonly WindowFilter filter = new("WindowSnapper.App", currentProcessId: 100);

    [Theory]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("DV2ControlHost")]
    [InlineData("MsgrIMEWindowClass")]
    [InlineData("WindowSnapperOverlayWindow")]
    public void IgnoresRequiredSystemWindowClasses(string className)
    {
        var window = CreateWindow(className: className);

        Assert.False(filter.IsWindowManageable(window));
    }

    [Fact]
    public void IgnoresInvisibleWindow()
    {
        var window = CreateWindow(isVisible: false);

        Assert.False(filter.IsWindowManageable(window));
    }

    [Fact]
    public void IgnoresMinimizedWindow()
    {
        var window = CreateWindow(isMinimized: true);

        Assert.False(filter.IsWindowManageable(window));
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    public void IgnoresWindowWithoutSize(int width, int height)
    {
        var window = CreateWindow(bounds: new RectInt(10, 10, width, height));

        Assert.False(filter.IsWindowManageable(window));
    }

    [Fact]
    public void IgnoresCurrentProcessWindow()
    {
        var window = CreateWindow(processName: "WindowSnapper.App", processId: 100);

        Assert.False(filter.IsWindowManageable(window));
    }

    [Fact]
    public void AllowsNormalDesktopWindow()
    {
        var window = CreateWindow();

        Assert.True(filter.IsWindowManageable(window));
    }

    [Fact]
    public void DetectsWindowCoveringFullMonitorBounds()
    {
        var window = new WindowInfo(
            new WindowHandle(1234),
            string.Empty,
            "game",
            "GameWindow",
            new RectInt(0, 0, 1920, 1080),
            IsVisible: true,
            IsMinimized: false,
            IsMaximized: false);
        var monitor = new MonitorInfo(
            "primary",
            "DISPLAY1",
            new RectInt(0, 0, 1920, 1080),
            new RectInt(0, 0, 1920, 1040),
            IsPrimary: true,
            DpiScale: 1);

        Assert.True(WindowFilter.CoversMonitorBounds(window, monitor));
    }

    private static WindowFilterInfo CreateWindow(
        string processName = "notepad",
        int? processId = 200,
        string className = "Notepad",
        RectInt? bounds = null,
        bool isVisible = true,
        bool isMinimized = false)
    {
        return new WindowFilterInfo(
            new WindowHandle(1234),
            processName,
            processId,
            className,
            bounds ?? new RectInt(10, 10, 800, 600),
            isVisible,
            isMinimized);
    }
}
