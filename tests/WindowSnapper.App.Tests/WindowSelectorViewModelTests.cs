using WindowSnapper.App.ViewModels;
using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;
using WindowSnapper.Layouts;
using WindowSnapper.Snap;
using Xunit;

namespace WindowSnapper.App.Tests;

public sealed class WindowSelectorViewModelTests
{
    [Fact]
    public void RefreshWindowsPopulatesManageableWindows()
    {
        var viewModel = CreateViewModel([
            CreateWindow(100, "notepad", "Notepad", "Notes"),
            CreateWindow(200, "devenv", "HwndWrapper", "Visual Studio")
        ]);

        Assert.Equal(2, viewModel.Windows.Count);
        Assert.NotNull(viewModel.SelectedWindow);
        Assert.Single(viewModel.SelectedWindows);
        Assert.Contains("已加载 2 个可管理窗口", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void RefreshWindowsShowsFriendlyFailureWhenEnumeratorFails()
    {
        var viewModel = CreateViewModel(
            Result<IReadOnlyList<WindowInfo>>.Failure(ResultErrorCode.PlatformCallFailed, "EnumWindows failed."));

        Assert.Empty(viewModel.Windows);
        Assert.Null(viewModel.SelectedWindow);
        Assert.Empty(viewModel.SelectedWindows);
        Assert.Equal("无法加载窗口列表，请稍后重试。", viewModel.ErrorMessage);
    }

    [Fact]
    public void SelectedLayoutUpdatesZoneChoices()
    {
        var customLayout = CreateCustomLayout();
        var viewModel = CreateViewModel([CreateWindow(100)], LayoutRegistry.Create([customLayout]));

        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);

        Assert.Equal(2, viewModel.Zones.Count);
        Assert.Equal("browser", viewModel.SelectedZone?.Id);
    }

    [Fact]
    public void MoveSelectedWindowCallsSnapDelegateWithSelectedWindowAndZone()
    {
        var customLayout = CreateCustomLayout();
        var selectedHandle = WindowHandle.None;
        SnapCommand? selectedCommand = null;
        var viewModel = CreateViewModel(
            [CreateWindow(300, "terminal", "ConsoleWindowClass", "Terminal")],
            LayoutRegistry.Create([customLayout]),
            (handle, command) =>
            {
                selectedHandle = handle;
                selectedCommand = command;
                return Result.Success();
            });

        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "browser");

        var result = viewModel.MoveSelectedWindow();

        Assert.True(result.IsSuccess);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(300)), selectedHandle);
        Assert.Equal("dev-layout", selectedCommand?.LayoutId);
        Assert.Equal("browser", selectedCommand?.ZoneId);
        Assert.Equal("已移动选中的窗口。", viewModel.StatusMessage);
    }

    [Fact]
    public void MoveSelectedWindowMovesMultipleWindowsByZoneNameOrder()
    {
        var customLayout = CreateCustomLayout();
        var commands = new List<(WindowHandle Handle, SnapCommand Command)>();
        var viewModel = CreateViewModel(
            [
                CreateWindow(300, "terminal", "ConsoleWindowClass", "Terminal"),
                CreateWindow(400, "notepad", "Notepad", "Notes")
            ],
            LayoutRegistry.Create([customLayout]),
            (handle, command) =>
            {
                commands.Add((handle, command));
                return Result.Success();
            });

        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.UpdateSelectedWindows(viewModel.Windows);

        var result = viewModel.MoveSelectedWindow();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, commands.Count);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(300)), commands[0].Handle);
        Assert.Equal("browser", commands[0].Command.ZoneId);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(400)), commands[1].Handle);
        Assert.Equal("code", commands[1].Command.ZoneId);
        Assert.Equal("已从第 1 区开始移动 2 个窗口。", viewModel.StatusMessage);
    }

    [Fact]
    public void MoveSelectedWindowCanStartFromManuallySelectedSortedZone()
    {
        var customLayout = CreateThreeZoneLayout();
        var commands = new List<(WindowHandle Handle, SnapCommand Command)>();
        var viewModel = CreateViewModel(
            [
                CreateWindow(300),
                CreateWindow(400)
            ],
            LayoutRegistry.Create([customLayout]),
            (handle, command) =>
            {
                commands.Add((handle, command));
                return Result.Success();
            });

        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "beta");
        viewModel.UpdateSelectedWindows(viewModel.Windows);

        var result = viewModel.MoveSelectedWindow();

        Assert.True(result.IsSuccess);
        Assert.Equal("beta", commands[0].Command.ZoneId);
        Assert.Equal("gamma", commands[1].Command.ZoneId);
        Assert.Equal("已从第 2 区开始移动 2 个窗口。", viewModel.StatusMessage);
    }

    [Fact]
    public void MoveSelectedWindowFailsWhenSelectedWindowsExceedRemainingZoneCount()
    {
        var customLayout = CreateCustomLayout();
        var viewModel = CreateViewModel(
            [
                CreateWindow(300),
                CreateWindow(400),
                CreateWindow(500)
            ],
            LayoutRegistry.Create([customLayout]));

        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.UpdateSelectedWindows(viewModel.Windows);

        var result = viewModel.MoveSelectedWindow();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Equal("从当前起始区域开始，剩余区域数量不足以放置所选窗口。", viewModel.ErrorMessage);
    }

    [Fact]
    public void SelectTargetForCurrentWindowStoresAssignmentOnWindowRow()
    {
        var customLayout = CreateThreeZoneLayout();
        var viewModel = CreateViewModel([CreateWindow(300)], LayoutRegistry.Create([customLayout]));

        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "beta");

        var result = viewModel.SelectTargetForCurrentWindow();

        Assert.True(result.IsSuccess);
        Assert.True(viewModel.Windows[0].HasAssignment);
        Assert.Equal("three-zone-layout", viewModel.Windows[0].AssignedLayoutId);
        Assert.Equal("beta", viewModel.Windows[0].AssignedZoneId);
        Assert.Contains("第 2 区", viewModel.Windows[0].AssignmentText, StringComparison.Ordinal);
        Assert.Equal("已选定 1 个固定移动目标", viewModel.AssignmentSummaryText);
    }

    [Fact]
    public void SelectTargetForCurrentWindowFailsWhenMultipleWindowsAreSelected()
    {
        var viewModel = CreateViewModel([
            CreateWindow(300),
            CreateWindow(400)
        ]);
        viewModel.UpdateSelectedWindows(viewModel.Windows);

        var result = viewModel.SelectTargetForCurrentWindow();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Equal("请选择一个窗口后再点击“选定”。", viewModel.ErrorMessage);
    }

    [Fact]
    public void MoveSelectedWindowUsesManualAssignmentsWhenPresent()
    {
        var customLayout = CreateThreeZoneLayout();
        var commands = new List<(WindowHandle Handle, SnapCommand Command)>();
        var viewModel = CreateViewModel(
            [
                CreateWindow(300),
                CreateWindow(400)
            ],
            LayoutRegistry.Create([customLayout]),
            (handle, command) =>
            {
                commands.Add((handle, command));
                return Result.Success();
            });
        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);

        viewModel.UpdateSelectedWindows([viewModel.Windows[0]]);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "beta");
        _ = viewModel.SelectTargetForCurrentWindow();
        viewModel.UpdateSelectedWindows([viewModel.Windows[1]]);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "gamma");
        _ = viewModel.SelectTargetForCurrentWindow();

        var result = viewModel.MoveSelectedWindow();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, commands.Count);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(300)), commands[0].Handle);
        Assert.Equal("beta", commands[0].Command.ZoneId);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(400)), commands[1].Handle);
        Assert.Equal("gamma", commands[1].Command.ZoneId);
        Assert.Equal("已移动 2 个选定窗口。", viewModel.StatusMessage);
    }

    [Fact]
    public void RestoreLastMoveReturnsMovedWindowsToCapturedBounds()
    {
        var customLayout = CreateCustomLayout();
        var restored = new List<WindowRestoreTarget>();
        var originalBounds = new RectInt(10, 20, 800, 600);
        var viewModel = CreateViewModel(
            [CreateWindow(300)],
            LayoutRegistry.Create([customLayout]),
            snapWindow: (_, _) => Result.Success(),
            getWindowInfo: _ => Result<WindowInfo>.Success(CreateWindow(300, bounds: originalBounds)),
            restoreWindowTarget: target =>
            {
                restored.Add(target);
                return Result.Success();
            });
        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "browser");

        var move = viewModel.MoveSelectedWindow();
        var restore = viewModel.RestoreLastMove();

        Assert.True(move.IsSuccess);
        Assert.True(restore.IsSuccess);
        Assert.Single(restored);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(300)), restored[0].Handle);
        Assert.Equal(originalBounds, restored[0].Bounds);
        Assert.True(restored[0].WasVisible);
        Assert.False(restored[0].WasMinimized);
        Assert.Equal("已还原 1 个窗口。", viewModel.StatusMessage);
        Assert.Equal("暂无可还原窗口", viewModel.RestoreSummaryText);
    }

    [Fact]
    public void RestoreLastMovePreservesMinimizedState()
    {
        var customLayout = CreateCustomLayout();
        var restored = new List<WindowRestoreTarget>();
        var originalBounds = new RectInt(10, 20, 800, 600);
        var viewModel = CreateViewModel(
            [CreateWindow(300, isMinimized: true)],
            LayoutRegistry.Create([customLayout]),
            snapWindow: (_, _) => Result.Success(),
            getWindowInfo: _ => Result<WindowInfo>.Success(CreateWindow(300, bounds: originalBounds, isMinimized: true)),
            restoreWindowTarget: target =>
            {
                restored.Add(target);
                return Result.Success();
            });
        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "browser");

        var move = viewModel.MoveSelectedWindow();
        var restore = viewModel.RestoreLastMove();

        Assert.True(move.IsSuccess);
        Assert.True(restore.IsSuccess);
        Assert.Single(restored);
        Assert.True(restored[0].WasVisible);
        Assert.True(restored[0].WasMinimized);
    }

    [Fact]
    public void RestoreLastMovePreservesHiddenState()
    {
        var customLayout = CreateCustomLayout();
        var restored = new List<WindowRestoreTarget>();
        var originalBounds = new RectInt(10, 20, 800, 600);
        var viewModel = CreateViewModel(
            [CreateWindow(300, isVisible: false)],
            LayoutRegistry.Create([customLayout]),
            snapWindow: (_, _) => Result.Success(),
            getWindowInfo: _ => Result<WindowInfo>.Success(CreateWindow(300, bounds: originalBounds, isVisible: false)),
            restoreWindowTarget: target =>
            {
                restored.Add(target);
                return Result.Success();
            });
        viewModel.SelectedLayout = viewModel.Layouts.Single(layout => layout.Id == customLayout.Id);
        viewModel.SelectedZone = viewModel.Zones.Single(zone => zone.Id == "browser");

        var move = viewModel.MoveSelectedWindow();
        var restore = viewModel.RestoreLastMove();

        Assert.True(move.IsSuccess);
        Assert.True(restore.IsSuccess);
        Assert.Single(restored);
        Assert.False(restored[0].WasVisible);
        Assert.False(restored[0].WasMinimized);
    }

    [Fact]
    public void RestoreLastMoveFailsWhenNoSnapshotExists()
    {
        var viewModel = CreateViewModel([CreateWindow(300)]);

        var result = viewModel.RestoreLastMove();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.NotFound, result.ErrorCode);
        Assert.Equal("暂无可还原窗口。", viewModel.ErrorMessage);
    }

    [Fact]
    public void MoveSelectedWindowFailsWhenNoWindowIsSelected()
    {
        var viewModel = CreateViewModel([]);

        var result = viewModel.MoveSelectedWindow();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Equal("请选择一个窗口。", viewModel.ErrorMessage);
    }

    private static WindowSelectorViewModel CreateViewModel(
        IReadOnlyList<WindowInfo> windows,
        LayoutRegistry? layoutRegistry = null,
        Func<WindowHandle, SnapCommand, Result>? snapWindow = null,
        Func<WindowHandle, Result<WindowInfo>>? getWindowInfo = null,
        Func<WindowRestoreTarget, Result>? restoreWindowTarget = null)
    {
        return CreateViewModel(
            Result<IReadOnlyList<WindowInfo>>.Success(windows),
            layoutRegistry,
            snapWindow,
            getWindowInfo,
            restoreWindowTarget);
    }

    private static WindowSelectorViewModel CreateViewModel(
        Result<IReadOnlyList<WindowInfo>> windowResult,
        LayoutRegistry? layoutRegistry = null,
        Func<WindowHandle, SnapCommand, Result>? snapWindow = null,
        Func<WindowHandle, Result<WindowInfo>>? getWindowInfo = null,
        Func<WindowRestoreTarget, Result>? restoreWindowTarget = null)
    {
        return new WindowSelectorViewModel(
            new FakeWindowEnumerator(windowResult),
            layoutRegistry ?? LayoutRegistry.Create(Array.Empty<LayoutDefinition>()),
            snapWindow ?? ((_, _) => Result.Success()),
            getWindowInfo ?? (_ => Result<WindowInfo>.Success(CreateWindow(300))),
            restoreWindowTarget ?? (_ => Result.Success()));
    }

    private static WindowInfo CreateWindow(
        int handle,
        string processName = "notepad",
        string className = "Notepad",
        string title = "Untitled",
        bool isMinimized = false,
        bool isVisible = true,
        RectInt? bounds = null)
    {
        return new WindowInfo(
            WindowHandle.FromIntPtr(new IntPtr(handle)),
            title,
            processName,
            className,
            bounds ?? new RectInt(10, 20, 800, 600),
            isVisible,
            isMinimized,
            false);
    }

    private static LayoutDefinition CreateCustomLayout()
    {
        return new LayoutDefinition(
            "dev-layout",
            "Dev Layout",
            1,
            Gap: 8,
            Margin: 8,
            [
                new ZoneDefinition("code", "Code", 0, 0, 0.6, 1),
                new ZoneDefinition("browser", "Browser", 0.6, 0, 0.4, 1)
            ]);
    }

    private static LayoutDefinition CreateThreeZoneLayout()
    {
        return new LayoutDefinition(
            "three-zone-layout",
            "Three Zone Layout",
            1,
            Gap: 8,
            Margin: 8,
            [
                new ZoneDefinition("gamma", "Gamma", 0.66, 0, 0.34, 1),
                new ZoneDefinition("alpha", "Alpha", 0, 0, 0.33, 1),
                new ZoneDefinition("beta", "Beta", 0.33, 0, 0.33, 1)
            ]);
    }

    private sealed class FakeWindowEnumerator : IWindowEnumerator
    {
        private readonly Result<IReadOnlyList<WindowInfo>> result;

        public FakeWindowEnumerator(Result<IReadOnlyList<WindowInfo>> result)
        {
            this.result = result;
        }

        public Result<IReadOnlyList<WindowInfo>> GetWindows()
        {
            return result;
        }
    }
}
