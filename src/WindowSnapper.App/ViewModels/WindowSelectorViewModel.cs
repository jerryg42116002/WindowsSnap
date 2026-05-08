using System.Collections.ObjectModel;
using System.Windows.Input;
using WindowSnapper.App.Commands;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;
using WindowSnapper.Layouts;
using WindowSnapper.Snap;

namespace WindowSnapper.App.ViewModels;

internal sealed class WindowSelectorViewModel : ViewModelBase
{
    private readonly IWindowEnumerator windowEnumerator;
    private readonly Func<WindowHandle, SnapCommand, Result> snapWindow;
    private readonly Func<WindowHandle, Result<WindowInfo>> getWindowInfo;
    private readonly Func<WindowRestoreTarget, Result> restoreWindowTarget;
    private readonly List<WindowListItemViewModel> selectedWindows = [];
    private IReadOnlyList<WindowRestoreTarget> lastMoveSnapshot = [];
    private WindowListItemViewModel? selectedWindow;
    private LayoutChoiceViewModel? selectedLayout;
    private ZoneChoiceViewModel? selectedZone;
    private string statusMessage = "请选择一个窗口和目标区域。";
    private string errorMessage = string.Empty;

    public WindowSelectorViewModel(
        IWindowEnumerator windowEnumerator,
        LayoutRegistry layoutRegistry,
        Func<WindowHandle, SnapCommand, Result> snapWindow,
        Func<WindowHandle, Result<WindowInfo>>? getWindowInfo = null,
        Func<WindowRestoreTarget, Result>? restoreWindowTarget = null)
    {
        this.windowEnumerator = windowEnumerator ?? throw new ArgumentNullException(nameof(windowEnumerator));
        ArgumentNullException.ThrowIfNull(layoutRegistry);
        this.snapWindow = snapWindow ?? throw new ArgumentNullException(nameof(snapWindow));
        this.getWindowInfo = getWindowInfo ?? (_ => Result<WindowInfo>.Failure(ResultErrorCode.NotSupported, "Window restore is not available."));
        this.restoreWindowTarget = restoreWindowTarget ?? (_ => Result.Failure(ResultErrorCode.NotSupported, "Window restore is not available."));

        RefreshCommand = new RelayCommand(RefreshWindows);
        SelectTargetCommand = new RelayCommand(() => _ = SelectTargetForCurrentWindow());
        MoveCommand = new RelayCommand(() => _ = MoveSelectedWindow());
        RestoreCommand = new RelayCommand(() => _ = RestoreLastMove());
        CloseCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        UpdateLayouts(layoutRegistry.Layouts);
        RefreshWindows();
    }

    public event EventHandler? CloseRequested;

    public ObservableCollection<WindowListItemViewModel> Windows { get; } = [];

    public ObservableCollection<LayoutChoiceViewModel> Layouts { get; } = [];

    public ObservableCollection<ZoneChoiceViewModel> Zones { get; } = [];

    public IReadOnlyList<WindowListItemViewModel> SelectedWindows => selectedWindows;

    public WindowListItemViewModel? SelectedWindow
    {
        get => selectedWindow;
        set
        {
            if (SetProperty(ref selectedWindow, value) && selectedWindows.Count == 0 && value is not null)
            {
                UpdateSelectedWindows([value]);
            }
        }
    }

    public LayoutChoiceViewModel? SelectedLayout
    {
        get => selectedLayout;
        set
        {
            if (SetProperty(ref selectedLayout, value))
            {
                RebuildZones();
            }
        }
    }

    public ZoneChoiceViewModel? SelectedZone
    {
        get => selectedZone;
        set => SetProperty(ref selectedZone, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public string SelectedWindowCountText => selectedWindows.Count == 0
        ? "未选择窗口"
        : $"已选择 {selectedWindows.Count} 个窗口";

    public string AssignmentSummaryText
    {
        get
        {
            var assignmentCount = Windows.Count(window => window.HasAssignment);
            return assignmentCount == 0
                ? "未选定固定移动目标"
                : $"已选定 {assignmentCount} 个固定移动目标";
        }
    }

    public string RestoreSummaryText => lastMoveSnapshot.Count == 0
        ? "暂无可还原窗口"
        : $"可还原 {lastMoveSnapshot.Count} 个窗口";

    public ICommand RefreshCommand { get; }

    public ICommand SelectTargetCommand { get; }

    public ICommand MoveCommand { get; }

    public ICommand RestoreCommand { get; }

    public ICommand CloseCommand { get; }

    public void RefreshWindows()
    {
        ErrorMessage = string.Empty;

        var result = windowEnumerator.GetWindows();
        Windows.Clear();
        SetSelectedWindow(null);
        UpdateSelectedWindows([]);
        lastMoveSnapshot = [];
        OnPropertyChanged(nameof(AssignmentSummaryText));
        OnPropertyChanged(nameof(RestoreSummaryText));

        if (result.IsFailure)
        {
            StatusMessage = "窗口列表不可用。";
            ErrorMessage = "无法加载窗口列表，请稍后重试。";
            return;
        }

        foreach (var window in result.Value
            .OrderBy(window => window.ProcessName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(window => window.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.OrdinalIgnoreCase))
        {
            Windows.Add(WindowListItemViewModel.FromWindowInfo(window));
        }

        var firstWindow = Windows.FirstOrDefault();
        SetSelectedWindow(firstWindow);
        UpdateSelectedWindows(firstWindow is null ? [] : [firstWindow]);
        StatusMessage = Windows.Count == 0
            ? "当前没有可管理窗口。"
            : $"已加载 {Windows.Count} 个可管理窗口。";
        OnPropertyChanged(nameof(AssignmentSummaryText));
    }

    public void UpdateSelectedWindows(IEnumerable<WindowListItemViewModel> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        selectedWindows.Clear();
        foreach (var window in windows.DistinctBy(window => window.Handle))
        {
            selectedWindows.Add(window);
        }

        SetSelectedWindow(selectedWindows.FirstOrDefault());
        OnPropertyChanged(nameof(SelectedWindows));
        OnPropertyChanged(nameof(SelectedWindowCountText));
    }

    public Result SelectTargetForCurrentWindow()
    {
        ErrorMessage = string.Empty;

        if (selectedWindows.Count != 1)
        {
            ErrorMessage = "请选择一个窗口后再点击“选定”。";
            return Result.Failure(ResultErrorCode.InvalidArgument, ErrorMessage);
        }

        if (SelectedLayout is null || SelectedZone is null)
        {
            ErrorMessage = "请选择布局区域。";
            return Result.Failure(ResultErrorCode.InvalidArgument, ErrorMessage);
        }

        var window = selectedWindows[0];
        window.AssignTarget(
            SelectedLayout.Id,
            SelectedLayout.Name,
            SelectedZone.Id,
            SelectedZone.DisplayName);
        StatusMessage = $"已选定窗口目标：{SelectedZone.DisplayName}。";
        OnPropertyChanged(nameof(AssignmentSummaryText));
        return Result.Success();
    }

    public Result MoveSelectedWindow()
    {
        ErrorMessage = string.Empty;

        var assignedWindows = Windows.Where(window => window.HasAssignment).ToArray();
        if (assignedWindows.Length > 0)
        {
            return MoveAssignedWindows(assignedWindows);
        }

        var windowsToMove = selectedWindows.Count > 0
            ? selectedWindows.ToArray()
            : SelectedWindow is null ? [] : [SelectedWindow];
        if (windowsToMove.Length == 0)
        {
            ErrorMessage = "请选择一个窗口。";
            return Result.Failure(ResultErrorCode.InvalidArgument, ErrorMessage);
        }

        if (SelectedLayout is null || SelectedZone is null)
        {
            ErrorMessage = "请选择布局区域。";
            return Result.Failure(ResultErrorCode.InvalidArgument, ErrorMessage);
        }

        if (windowsToMove.Length == 1)
        {
            var snapshot = CaptureRestoreSnapshot(windowsToMove);
            if (snapshot.IsFailure)
            {
                ErrorMessage = snapshot.ErrorMessage;
                return snapshot;
            }

            var command = new SnapCommand(SelectedLayout.Id, SelectedZone.Id);
            var result = snapWindow(windowsToMove[0].Handle, command);
            if (result.IsFailure)
            {
                ErrorMessage = result.ErrorMessage;
                return result;
            }

            StatusMessage = "已移动选中的窗口。";
            return Result.Success();
        }

        var targetZones = Zones
            .SkipWhile(zone => !string.Equals(zone.Id, SelectedZone.Id, StringComparison.Ordinal))
            .ToArray();
        if (windowsToMove.Length > targetZones.Length)
        {
            ErrorMessage = "从当前起始区域开始，剩余区域数量不足以放置所选窗口。";
            return Result.Failure(ResultErrorCode.InvalidArgument, ErrorMessage);
        }

        var multiSnapshot = CaptureRestoreSnapshot(windowsToMove);
        if (multiSnapshot.IsFailure)
        {
            ErrorMessage = multiSnapshot.ErrorMessage;
            return multiSnapshot;
        }

        for (var index = 0; index < windowsToMove.Length; index++)
        {
            var command = new SnapCommand(SelectedLayout.Id, targetZones[index].Id);
            var result = snapWindow(windowsToMove[index].Handle, command);
            if (result.IsFailure)
            {
                ErrorMessage = result.ErrorMessage;
                return result;
            }
        }

        StatusMessage = $"已从第 {SelectedZone.SortedIndex} 区开始移动 {windowsToMove.Length} 个窗口。";
        return Result.Success();
    }

    private Result MoveAssignedWindows(IReadOnlyList<WindowListItemViewModel> windows)
    {
        var snapshot = CaptureRestoreSnapshot(windows);
        if (snapshot.IsFailure)
        {
            ErrorMessage = snapshot.ErrorMessage;
            return snapshot;
        }

        foreach (var window in windows)
        {
            var command = new SnapCommand(window.AssignedLayoutId, window.AssignedZoneId);
            var result = snapWindow(window.Handle, command);
            if (result.IsFailure)
            {
                ErrorMessage = result.ErrorMessage;
                return result;
            }
        }

        StatusMessage = $"已移动 {windows.Count} 个选定窗口。";
        return Result.Success();
    }

    public Result RestoreLastMove()
    {
        ErrorMessage = string.Empty;

        if (lastMoveSnapshot.Count == 0)
        {
            ErrorMessage = "暂无可还原窗口。";
            return Result.Failure(ResultErrorCode.NotFound, ErrorMessage);
        }

        foreach (var target in lastMoveSnapshot)
        {
            var restore = restoreWindowTarget(target);
            if (restore.IsFailure)
            {
                ErrorMessage = restore.ErrorMessage;
                return restore;
            }
        }

        var restoredCount = lastMoveSnapshot.Count;
        lastMoveSnapshot = [];
        OnPropertyChanged(nameof(RestoreSummaryText));
        StatusMessage = $"已还原 {restoredCount} 个窗口。";
        return Result.Success();
    }

    private Result CaptureRestoreSnapshot(IEnumerable<WindowListItemViewModel> windows)
    {
        var snapshot = new List<WindowRestoreTarget>();
        foreach (var window in windows.DistinctBy(window => window.Handle))
        {
            var windowInfo = getWindowInfo(window.Handle);
            if (windowInfo.IsFailure)
            {
                return Result.Failure(windowInfo.ErrorCode, "无法记录窗口移动前的位置。");
            }

            snapshot.Add(new WindowRestoreTarget(
                window.Handle,
                windowInfo.Value.Bounds,
                windowInfo.Value.IsVisible,
                windowInfo.Value.IsMinimized));
        }

        lastMoveSnapshot = snapshot;
        OnPropertyChanged(nameof(RestoreSummaryText));
        return Result.Success();
    }

    public void UpdateLayouts(IReadOnlyList<LayoutDefinition> layouts)
    {
        ArgumentNullException.ThrowIfNull(layouts);

        var selectedLayoutId = SelectedLayout?.Id;
        Layouts.Clear();
        foreach (var layout in layouts)
        {
            Layouts.Add(new LayoutChoiceViewModel(layout));
        }

        SelectedLayout = Layouts.FirstOrDefault(layout => string.Equals(layout.Id, selectedLayoutId, StringComparison.Ordinal))
            ?? Layouts.FirstOrDefault();
    }

    private void RebuildZones()
    {
        var selectedZoneId = SelectedZone?.Id;
        Zones.Clear();

        if (SelectedLayout is null)
        {
            SelectedZone = null;
            return;
        }

        var sortedZones = SelectedLayout.Layout.Zones
            .OrderBy(zone => zone.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(zone => zone.Id, StringComparer.Ordinal)
            .ToArray();
        for (var index = 0; index < sortedZones.Length; index++)
        {
            Zones.Add(new ZoneChoiceViewModel(sortedZones[index], sortedIndex: index + 1));
        }

        SelectedZone = Zones.FirstOrDefault(zone => string.Equals(zone.Id, selectedZoneId, StringComparison.Ordinal))
            ?? Zones.FirstOrDefault();
    }

    private void SetSelectedWindow(WindowListItemViewModel? window)
    {
        if (SetProperty(ref selectedWindow, window, nameof(SelectedWindow)))
        {
            OnPropertyChanged(nameof(SelectedWindowCountText));
        }
    }
}
