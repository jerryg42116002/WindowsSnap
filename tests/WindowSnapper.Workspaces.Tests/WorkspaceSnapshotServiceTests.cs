using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Time;
using WindowSnapper.Core.Windows;
using WindowSnapper.Core.Workspaces;
using WindowSnapper.Workspaces;
using Xunit;

namespace WindowSnapper.Workspaces.Tests;

public sealed class WorkspaceSnapshotServiceTests
{
    [Fact]
    public async Task SaveCurrentAsyncPersistsOnlySnapshotSafeWindowData()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var window = CreateWindow(
            1,
            "Sensitive Browser Title https://example.local/private",
            "browser.exe",
            "Chrome_WidgetWin_1",
            new RectInt(960, 0, 960, 1080));
        var store = new FakeWorkspaceSnapshotStore();
        var service = CreateService(
            new[] { window },
            new[] { monitor },
            store: store,
            clock: new FakeClock(new DateTimeOffset(2026, 5, 7, 12, 30, 0, TimeSpan.Zero)));

        var result = await service.SaveCurrentAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(store.SavedSnapshot);
        var savedSnapshot = store.SavedSnapshot!;
        var savedWindow = Assert.Single(savedSnapshot.Windows);
        Assert.Equal("browser.exe", savedWindow.ProcessName);
        Assert.Equal("Chrome_WidgetWin_1", savedWindow.ClassName);
        Assert.Equal(@"\\.\DISPLAY1", savedWindow.MonitorDeviceName);
        Assert.Equal(new RelativeRect(0.5, 0, 0.5, 1), savedWindow.RelativeRect);
    }

    [Fact]
    public async Task RestoreLatestAsyncMatchesWindowAndMovesToAbsoluteRect()
    {
        var monitor = CreateMonitor(new RectInt(-1920, 0, 1920, 1080));
        var window = CreateWindow(7, "ignored", "editor.exe", "EditorWindow", new RectInt(0, 0, 800, 600));
        var snapshot = CreateSnapshot(
            new WorkspaceWindowSnapshot(
                "editor.exe",
                "EditorWindow",
                @"\\.\DISPLAY1",
                new RelativeRect(0.5, 0, 0.5, 1),
                WorkspaceWindowState.Normal));
        var windowManager = new FakeWindowManager();
        var service = CreateService(
            new[] { window },
            new[] { monitor },
            windowManager,
            new FakeWorkspaceSnapshotStore { SnapshotToLoad = snapshot });

        var result = await service.RestoreLatestAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new WorkspaceRestoreResult(1, 0, 0), result.Value);
        Assert.True(windowManager.MovedWindows.TryGetValue(window.Handle, out var targetBounds));
        Assert.Equal(new RectInt(-960, 0, 960, 1080), targetBounds);
    }

    [Fact]
    public async Task RestoreLatestAsyncReturnsMissingCountWhenWindowDoesNotMatch()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var snapshot = CreateSnapshot(
            new WorkspaceWindowSnapshot(
                "missing.exe",
                "MissingWindow",
                @"\\.\DISPLAY1",
                new RelativeRect(0, 0, 0.5, 1),
                WorkspaceWindowState.Normal));
        var windowManager = new FakeWindowManager();
        var service = CreateService(
            new[] { CreateWindow(4, "ignored", "other.exe", "OtherWindow", new RectInt(0, 0, 800, 600)) },
            new[] { monitor },
            windowManager,
            new FakeWorkspaceSnapshotStore { SnapshotToLoad = snapshot });

        var result = await service.RestoreLatestAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(new WorkspaceRestoreResult(0, 1, 0), result.Value);
        Assert.Empty(windowManager.MovedWindows);
    }

    [Fact]
    public async Task RestoreLatestAsyncRestoresMaximizedWindowBeforeMove()
    {
        var monitor = CreateMonitor(new RectInt(0, 0, 1920, 1080));
        var window = CreateWindow(
            9,
            "ignored",
            "editor.exe",
            "EditorWindow",
            new RectInt(0, 0, 1920, 1080),
            isMaximized: true);
        var snapshot = CreateSnapshot(
            new WorkspaceWindowSnapshot(
                "editor.exe",
                "EditorWindow",
                @"\\.\DISPLAY1",
                new RelativeRect(0, 0, 0.5, 1),
                WorkspaceWindowState.Maximized));
        var windowManager = new FakeWindowManager();
        var service = CreateService(
            new[] { window },
            new[] { monitor },
            windowManager,
            new FakeWorkspaceSnapshotStore { SnapshotToLoad = snapshot });

        var result = await service.RestoreLatestAsync();

        Assert.True(result.IsSuccess);
        Assert.Contains(window.Handle, windowManager.RestoredWindows);
        Assert.Equal(new RectInt(0, 0, 960, 1080), windowManager.MovedWindows[window.Handle]);
    }

    private static WorkspaceSnapshotService CreateService(
        IReadOnlyList<WindowInfo> windows,
        IReadOnlyList<MonitorInfo> monitors,
        FakeWindowManager? windowManager = null,
        FakeWorkspaceSnapshotStore? store = null,
        IClock? clock = null)
    {
        return new WorkspaceSnapshotService(
            new FakeWindowEnumerator(windows),
            windowManager ?? new FakeWindowManager(),
            new FakeMonitorManager(monitors),
            store ?? new FakeWorkspaceSnapshotStore(),
            clock);
    }

    private static MonitorInfo CreateMonitor(RectInt workArea)
    {
        return new MonitorInfo("display-1", @"\\.\DISPLAY1", workArea, workArea, true, 1);
    }

    private static WindowInfo CreateWindow(
        int handle,
        string title,
        string processName,
        string className,
        RectInt bounds,
        bool isMaximized = false)
    {
        return new WindowInfo(
            new WindowHandle(handle),
            title,
            processName,
            className,
            bounds,
            true,
            false,
            isMaximized);
    }

    private static WorkspaceSnapshot CreateSnapshot(params WorkspaceWindowSnapshot[] windows)
    {
        return new WorkspaceSnapshot(
            1,
            "snapshot-1",
            "Snapshot 1",
            new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero),
            windows);
    }

    private sealed class FakeWindowEnumerator(IReadOnlyList<WindowInfo> windows) : IWindowEnumerator
    {
        public Result<IReadOnlyList<WindowInfo>> GetWindows()
        {
            return Result<IReadOnlyList<WindowInfo>>.Success(windows);
        }
    }

    private sealed class FakeWindowManager : IWindowManager
    {
        public Dictionary<WindowHandle, RectInt> MovedWindows { get; } = new();

        public List<WindowHandle> RestoredWindows { get; } = new();

        public Result<WindowHandle> GetActiveWindow()
        {
            return Result<WindowHandle>.Success(new WindowHandle(1));
        }

        public Result<WindowInfo> GetWindowInfo(WindowHandle handle)
        {
            return Result<WindowInfo>.Failure(ResultErrorCode.NotFound, "Not implemented by this test fake.");
        }

        public Result<bool> IsWindowManageable(WindowInfo windowInfo)
        {
            return Result<bool>.Success(windowInfo is { IsVisible: true, IsMinimized: false } &&
                windowInfo.Bounds.Width > 0 &&
                windowInfo.Bounds.Height > 0);
        }

        public Result RestoreWindow(WindowHandle handle)
        {
            RestoredWindows.Add(handle);
            return Result.Success();
        }

        public Result MoveWindow(WindowHandle handle, RectInt targetBounds)
        {
            MovedWindows[handle] = targetBounds;
            return Result.Success();
        }
    }

    private sealed class FakeMonitorManager(IReadOnlyList<MonitorInfo> monitors) : IMonitorManager
    {
        public Result<IReadOnlyList<MonitorInfo>> GetMonitors()
        {
            return Result<IReadOnlyList<MonitorInfo>>.Success(monitors);
        }

        public Result<MonitorInfo> GetMonitorForWindow(WindowHandle handle)
        {
            return Result<MonitorInfo>.Success(monitors[0]);
        }

        public Result<MonitorInfo> GetMonitorForPoint(PointInt point)
        {
            return Result<MonitorInfo>.Success(monitors[0]);
        }
    }

    private sealed class FakeWorkspaceSnapshotStore : IWorkspaceSnapshotStore
    {
        public WorkspaceSnapshot? SavedSnapshot { get; private set; }

        public WorkspaceSnapshot? SnapshotToLoad { get; init; }

        public Task<Result> SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            SavedSnapshot = snapshot;
            return Task.FromResult(Result.Success());
        }

        public Task<Result<WorkspaceSnapshot>> LoadAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LoadSnapshot());
        }

        public Task<Result<WorkspaceSnapshot>> LoadLatestAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LoadSnapshot());
        }

        public Task<Result<IReadOnlyList<WorkspaceSnapshot>>> ListAsync(CancellationToken cancellationToken = default)
        {
            var snapshot = SnapshotToLoad;
            IReadOnlyList<WorkspaceSnapshot> snapshots = snapshot is null
                ? Array.Empty<WorkspaceSnapshot>()
                : [snapshot];
            return Task.FromResult(Result<IReadOnlyList<WorkspaceSnapshot>>.Success(snapshots));
        }

        private Result<WorkspaceSnapshot> LoadSnapshot()
        {
            var snapshot = SnapshotToLoad;
            return snapshot is null
                ? Result<WorkspaceSnapshot>.Failure(ResultErrorCode.NotFound, "No snapshot.")
                : Result<WorkspaceSnapshot>.Success(snapshot);
        }
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;

        public DateTimeOffset LocalNow => UtcNow;
    }
}
