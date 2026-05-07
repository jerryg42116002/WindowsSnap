namespace WindowSnapper.Snap.Tests;

public sealed class WindowSnapServiceTests
{
    [Fact]
    public void SnapActiveWindowReturnsFailureWhenWindowIsNotManageable()
    {
        var windowManager = new FakeWindowManager
        {
            ManageableResult = Result<bool>.Success(false)
        };
        var service = CreateService(windowManager);

        var result = service.SnapActiveWindow(CreateCommand(BuiltinLayouts.LeftHalfId));

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.WindowNotManageable, result.ErrorCode);
        Assert.Equal(WindowSnapService.UserFriendlyMoveFailureMessage, result.ErrorMessage);
        Assert.False(windowManager.MoveWindowCalled);
    }

    [Fact]
    public void SnapActiveWindowPassesCalculatedLayoutRectToWindowManager()
    {
        var windowManager = new FakeWindowManager();
        var monitorManager = new FakeMonitorManager
        {
            MonitorResult = Result<MonitorInfo>.Success(new MonitorInfo(
                "secondary",
                "secondary",
                new RectInt(-1920, 0, 1920, 1080),
                new RectInt(-1920, 40, 1920, 1040),
                false,
                1.0))
        };
        var service = CreateService(windowManager, monitorManager);

        var result = service.SnapActiveWindow(CreateCommand(BuiltinLayouts.LeftHalfId));

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.MoveWindowCalled);
        Assert.Equal(new RectInt(-1920, 40, 960, 1040), windowManager.LastMoveTarget.GetValueOrDefault());
    }

    [Fact]
    public void SnapActiveWindowCanUseCustomLayoutFromRegistry()
    {
        var windowManager = new FakeWindowManager();
        var customLayout = new LayoutDefinition(
            "dev-layout",
            "Dev Layout",
            1,
            Gap: 0,
            Margin: 0,
            [new ZoneDefinition("code", "Code", 0, 0, 0.6, 1)]);
        var service = new WindowSnapService(
            windowManager,
            new FakeMonitorManager(),
            new LayoutEngine(),
            layoutRegistry: LayoutRegistry.Create([customLayout]));

        var result = service.SnapActiveWindow(new SnapCommand("dev-layout", "code"));

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(0, 0, 1152, 1080), windowManager.LastMoveTarget.GetValueOrDefault());
    }

    [Fact]
    public void SnapActiveWindowRestoresMaximizedWindowBeforeMove()
    {
        var windowManager = new FakeWindowManager
        {
            WindowInfoResult = Result<WindowInfo>.Success(CreateWindowInfo(isMaximized: true))
        };
        var service = CreateService(windowManager);

        var result = service.SnapActiveWindow(CreateCommand(BuiltinLayouts.RightHalfId));

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.RestoreWindowCalled);
        Assert.True(windowManager.MoveWindowCalled);
    }

    [Fact]
    public void SnapActiveWindowReturnsFriendlyFailureWhenMoveFails()
    {
        var windowManager = new FakeWindowManager
        {
            MoveWindowResult = Result.Failure(ResultErrorCode.PermissionDenied, "SetWindowPos failed.")
        };
        var service = CreateService(windowManager);

        var result = service.SnapActiveWindow(CreateCommand(BuiltinLayouts.LeftHalfId));

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.PermissionDenied, result.ErrorCode);
        Assert.Equal(WindowSnapService.UserFriendlyMoveFailureMessage, result.ErrorMessage);
    }

    private static WindowSnapService CreateService(
        FakeWindowManager? windowManager = null,
        FakeMonitorManager? monitorManager = null)
    {
        return new WindowSnapService(
            windowManager ?? new FakeWindowManager(),
            monitorManager ?? new FakeMonitorManager(),
            new LayoutEngine());
    }

    private static SnapCommand CreateCommand(string layoutId)
    {
        return new SnapCommand(layoutId, BuiltinLayouts.MainZoneId);
    }

    private static WindowInfo CreateWindowInfo(bool isMaximized = false)
    {
        return new WindowInfo(
            WindowHandle.FromIntPtr(new IntPtr(123)),
            string.Empty,
            "notepad",
            "Notepad",
            new RectInt(10, 10, 800, 600),
            true,
            false,
            isMaximized);
    }

    private sealed class FakeWindowManager : IWindowManager
    {
        private readonly WindowHandle handle = WindowHandle.FromIntPtr(new IntPtr(123));

        public Result<WindowHandle> ActiveWindowResult { get; init; }

        public Result<WindowInfo> WindowInfoResult { get; init; }

        public Result<bool> ManageableResult { get; init; } = Result<bool>.Success(true);

        public Result RestoreWindowResult { get; init; } = Result.Success();

        public Result MoveWindowResult { get; init; } = Result.Success();

        public bool RestoreWindowCalled { get; private set; }

        public bool MoveWindowCalled { get; private set; }

        public RectInt? LastMoveTarget { get; private set; }

        public FakeWindowManager()
        {
            ActiveWindowResult = Result<WindowHandle>.Success(handle);
            WindowInfoResult = Result<WindowInfo>.Success(CreateWindowInfo());
        }

        public Result<WindowHandle> GetActiveWindow()
        {
            return ActiveWindowResult;
        }

        public Result<WindowInfo> GetWindowInfo(WindowHandle handle)
        {
            return WindowInfoResult;
        }

        public Result<bool> IsWindowManageable(WindowInfo windowInfo)
        {
            return ManageableResult;
        }

        public Result RestoreWindow(WindowHandle handle)
        {
            RestoreWindowCalled = true;
            return RestoreWindowResult;
        }

        public Result MoveWindow(WindowHandle handle, RectInt targetBounds)
        {
            MoveWindowCalled = true;
            LastMoveTarget = targetBounds;
            return MoveWindowResult;
        }
    }

    private sealed class FakeMonitorManager : IMonitorManager
    {
        public Result<MonitorInfo> MonitorResult { get; init; } = Result<MonitorInfo>.Success(new MonitorInfo(
            "primary",
            "primary",
            new RectInt(0, 0, 1920, 1080),
            new RectInt(0, 0, 1920, 1080),
            true,
            1.0));

        public Result<IReadOnlyList<MonitorInfo>> GetMonitors()
        {
            return Result<IReadOnlyList<MonitorInfo>>.Success([MonitorResult.Value]);
        }

        public Result<MonitorInfo> GetMonitorForWindow(WindowHandle handle)
        {
            return MonitorResult;
        }

        public Result<MonitorInfo> GetMonitorForPoint(PointInt point)
        {
            return MonitorResult;
        }
    }
}
