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
    public void SnapWindowMovesRequestedWindowHandle()
    {
        var selectedWindow = WindowHandle.FromIntPtr(new IntPtr(456));
        var windowManager = new FakeWindowManager();
        var service = CreateService(windowManager);

        var result = service.SnapWindow(selectedWindow, CreateCommand(BuiltinLayouts.RightHalfId));

        Assert.True(result.IsSuccess);
        Assert.Equal(selectedWindow, windowManager.LastMoveHandle.GetValueOrDefault());
        Assert.Equal(new RectInt(960, 0, 960, 1080), windowManager.LastMoveTarget.GetValueOrDefault());
    }

    [Fact]
    public void SnapActiveWindowDoesNotShowOverlayWhenPreviewIsDisabled()
    {
        var overlay = new FakeOverlayPreviewService();
        var service = new WindowSnapService(
            new FakeWindowManager(),
            new FakeMonitorManager(),
            new LayoutEngine(),
            overlayPreviewService: overlay,
            overlayPreviewOptions: OverlayPreviewOptions.Disabled);

        var result = service.SnapActiveWindow(CreateCommand(BuiltinLayouts.LeftHalfId));

        Assert.True(result.IsSuccess);
        Assert.False(overlay.WasCalled);
    }

    [Fact]
    public void SnapActiveWindowPassesTargetRectToOverlayPreview()
    {
        var overlay = new FakeOverlayPreviewService();
        var monitorManager = new FakeMonitorManager
        {
            MonitorResult = Result<MonitorInfo>.Success(new MonitorInfo(
                "primary",
                "primary",
                new RectInt(0, 0, 1920, 1080),
                new RectInt(0, 0, 1920, 1040),
                true,
                1.25))
        };
        var service = new WindowSnapService(
            new FakeWindowManager(),
            monitorManager,
            new LayoutEngine(),
            overlayPreviewService: overlay,
            overlayPreviewOptions: new OverlayPreviewOptions(IsEnabled: true, Opacity: 0.35));

        var result = service.SnapActiveWindow(CreateCommand(BuiltinLayouts.RightHalfId));

        Assert.True(result.IsSuccess);
        Assert.True(overlay.WasCalled);
        Assert.Equal(new RectInt(960, 0, 960, 1040), overlay.LastTargetBounds.GetValueOrDefault());
        Assert.Equal(1.25, overlay.LastMonitor?.DpiScale);
        Assert.Equal(0.35, overlay.LastOpacity.GetValueOrDefault());
    }

    [Fact]
    public void OverlayPreviewOptionsUseDefaultOpacity()
    {
        Assert.Equal(0.35, OverlayPreviewOptions.Default.Opacity);
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
    public void SnapWindowRestoresMinimizedWindowBeforeMove()
    {
        var windowManager = new FakeWindowManager
        {
            WindowInfoResult = Result<WindowInfo>.Success(CreateWindowInfo(isMinimized: true))
        };
        var service = CreateService(windowManager);

        var result = service.SnapWindow(WindowHandle.FromIntPtr(new IntPtr(123)), CreateCommand(BuiltinLayouts.LeftHalfId));

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.RestoreWindowCalled);
        Assert.True(windowManager.MoveWindowCalled);
    }

    [Fact]
    public void SnapWindowRestoresHiddenWindowBeforeMove()
    {
        var windowManager = new FakeWindowManager
        {
            WindowInfoResult = Result<WindowInfo>.Success(CreateWindowInfo(isVisible: false))
        };
        var service = CreateService(windowManager);

        var result = service.SnapWindow(WindowHandle.FromIntPtr(new IntPtr(123)), CreateCommand(BuiltinLayouts.LeftHalfId));

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.RestoreWindowCalled);
        Assert.True(windowManager.MoveWindowCalled);
    }

    [Fact]
    public void GetWindowBoundsReturnsCurrentWindowBounds()
    {
        var service = CreateService(new FakeWindowManager
        {
            WindowInfoResult = Result<WindowInfo>.Success(CreateWindowInfo(bounds: new RectInt(50, 60, 700, 500)))
        });

        var result = service.GetWindowBounds(WindowHandle.FromIntPtr(new IntPtr(123)));

        Assert.True(result.IsSuccess);
        Assert.Equal(new RectInt(50, 60, 700, 500), result.Value);
    }

    [Fact]
    public void RestoreWindowBoundsMovesWindowToCapturedBounds()
    {
        var targetBounds = new RectInt(20, 30, 900, 700);
        var windowManager = new FakeWindowManager();
        var service = CreateService(windowManager);

        var result = service.RestoreWindowBounds(WindowHandle.FromIntPtr(new IntPtr(123)), targetBounds);

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.MoveWindowCalled);
        Assert.Equal(targetBounds, windowManager.LastMoveTarget.GetValueOrDefault());
    }

    [Fact]
    public void RestoreWindowBoundsRestoresMinimizedStateAfterMoving()
    {
        var targetBounds = new RectInt(20, 30, 900, 700);
        var windowManager = new FakeWindowManager();
        var service = CreateService(windowManager);

        var result = service.RestoreWindowBounds(
            WindowHandle.FromIntPtr(new IntPtr(123)),
            targetBounds,
            wasVisible: true,
            wasMinimized: true);

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.MoveWindowCalled);
        Assert.True(windowManager.MinimizeWindowCalled);
        Assert.False(windowManager.HideWindowCalled);
    }

    [Fact]
    public void RestoreWindowBoundsRestoresHiddenStateAfterMoving()
    {
        var targetBounds = new RectInt(20, 30, 900, 700);
        var windowManager = new FakeWindowManager();
        var service = CreateService(windowManager);

        var result = service.RestoreWindowBounds(
            WindowHandle.FromIntPtr(new IntPtr(123)),
            targetBounds,
            wasVisible: false,
            wasMinimized: false);

        Assert.True(result.IsSuccess);
        Assert.True(windowManager.MoveWindowCalled);
        Assert.True(windowManager.HideWindowCalled);
        Assert.False(windowManager.MinimizeWindowCalled);
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

    [Fact]
    public void HotkeySnapActiveWindowUsesRepeatCycleTarget()
    {
        var clock = new FakeClock();
        var windowManager = new FakeWindowManager();
        var service = new WindowSnapService(
            windowManager,
            new FakeMonitorManager(),
            new LayoutEngine(),
            repeatHotkeyCycleService: new RepeatHotkeyCycleService(clock));

        var first = service.SnapActiveWindow(HotkeyCommand.SnapRightHalf);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = service.SnapActiveWindow(HotkeyCommand.SnapRightHalf);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(new RectInt(1280, 0, 640, 1080), windowManager.LastMoveTarget.GetValueOrDefault());
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

    private static WindowInfo CreateWindowInfo(
        bool isMaximized = false,
        bool isMinimized = false,
        bool isVisible = true,
        RectInt? bounds = null)
    {
        return new WindowInfo(
            WindowHandle.FromIntPtr(new IntPtr(123)),
            string.Empty,
            "notepad",
            "Notepad",
            bounds ?? new RectInt(10, 10, 800, 600),
            isVisible,
            isMinimized,
            isMaximized);
    }

    private sealed class FakeWindowManager : IWindowManager
    {
        private readonly WindowHandle handle = WindowHandle.FromIntPtr(new IntPtr(123));

        public Result<WindowHandle> ActiveWindowResult { get; init; }

        public Result<WindowInfo> WindowInfoResult { get; set; }

        public Result<bool> ManageableResult { get; init; } = Result<bool>.Success(true);

        public Result RestoreWindowResult { get; init; } = Result.Success();

        public Result MinimizeWindowResult { get; init; } = Result.Success();

        public Result HideWindowResult { get; init; } = Result.Success();

        public Result MoveWindowResult { get; init; } = Result.Success();

        public bool RestoreWindowCalled { get; private set; }

        public bool MinimizeWindowCalled { get; private set; }

        public bool HideWindowCalled { get; private set; }

        public bool MoveWindowCalled { get; private set; }

        public RectInt? LastMoveTarget { get; private set; }

        public WindowHandle? LastMoveHandle { get; private set; }

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
            if (WindowInfoResult.IsSuccess)
            {
                WindowInfoResult = Result<WindowInfo>.Success(WindowInfoResult.Value with
                {
                    IsVisible = true,
                    IsMinimized = false,
                    IsMaximized = false
                });
            }

            return RestoreWindowResult;
        }

        public Result MinimizeWindow(WindowHandle handle)
        {
            MinimizeWindowCalled = true;
            if (WindowInfoResult.IsSuccess)
            {
                WindowInfoResult = Result<WindowInfo>.Success(WindowInfoResult.Value with
                {
                    IsMinimized = true,
                    IsVisible = true
                });
            }

            return MinimizeWindowResult;
        }

        public Result HideWindow(WindowHandle handle)
        {
            HideWindowCalled = true;
            if (WindowInfoResult.IsSuccess)
            {
                WindowInfoResult = Result<WindowInfo>.Success(WindowInfoResult.Value with
                {
                    IsVisible = false,
                    IsMinimized = false
                });
            }

            return HideWindowResult;
        }

        public Result MoveWindow(WindowHandle handle, RectInt targetBounds)
        {
            MoveWindowCalled = true;
            LastMoveHandle = handle;
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

    private sealed class FakeOverlayPreviewService : IOverlayPreviewService
    {
        public bool WasCalled { get; private set; }

        public MonitorInfo? LastMonitor { get; private set; }

        public RectInt? LastTargetBounds { get; private set; }

        public double? LastOpacity { get; private set; }

        public Result ShowPreview(MonitorInfo monitor, RectInt targetBounds, double opacity)
        {
            WasCalled = true;
            LastMonitor = monitor;
            LastTargetBounds = targetBounds;
            LastOpacity = opacity;
            return Result.Success();
        }
    }

    private sealed class FakeClock : WindowSnapper.Core.Time.IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2026, 5, 7, 0, 0, 0, TimeSpan.Zero);

        public DateTimeOffset LocalNow => UtcNow.ToLocalTime();

        public void Advance(TimeSpan elapsed)
        {
            UtcNow += elapsed;
        }
    }
}
