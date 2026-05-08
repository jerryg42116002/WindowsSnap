using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;
using WindowSnapper.Hotkeys;
using WindowSnapper.Layouts;

namespace WindowSnapper.Snap;

/// <summary>
/// Coordinates the MVP window snapping workflow.
/// </summary>
public sealed class WindowSnapService
{
    /// <summary>
    /// User-facing message for recoverable window movement failures.
    /// </summary>
    public const string UserFriendlyMoveFailureMessage = "无法移动该窗口。它可能是系统窗口、管理员权限窗口，或当前不允许调整大小。";

    private readonly IWindowManager windowManager;
    private readonly IMonitorManager monitorManager;
    private readonly LayoutEngine layoutEngine;
    private readonly LayoutRegistry layoutRegistry;
    private readonly IWindowSnapLogger logger;
    private readonly IOverlayPreviewService overlayPreviewService;
    private readonly OverlayPreviewOptions overlayPreviewOptions;
    private readonly RepeatHotkeyCycleService repeatHotkeyCycleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSnapService"/> class.
    /// </summary>
    public WindowSnapService(
        IWindowManager windowManager,
        IMonitorManager monitorManager,
        LayoutEngine layoutEngine,
        IWindowSnapLogger? logger = null,
        LayoutRegistry? layoutRegistry = null,
        IOverlayPreviewService? overlayPreviewService = null,
        OverlayPreviewOptions? overlayPreviewOptions = null,
        RepeatHotkeyCycleService? repeatHotkeyCycleService = null)
    {
        this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
        this.layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
        this.layoutRegistry = layoutRegistry ?? LayoutRegistry.Create(Array.Empty<LayoutDefinition>());
        this.logger = logger ?? NullWindowSnapLogger.Instance;
        this.overlayPreviewService = overlayPreviewService ?? NullOverlayPreviewService.Instance;
        this.overlayPreviewOptions = overlayPreviewOptions ?? OverlayPreviewOptions.Disabled;
        this.repeatHotkeyCycleService = repeatHotkeyCycleService ?? new RepeatHotkeyCycleService();
    }

    /// <summary>
    /// Moves the current active window into the snap target selected for a hotkey command.
    /// </summary>
    public Result SnapActiveWindow(HotkeyCommand command)
    {
        if (command == HotkeyCommand.None)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Hotkey command is required.");
        }

        if (command == HotkeyCommand.OpenLayoutSelector)
        {
            return Result.Failure(ResultErrorCode.NotSupported, "The layout selector command does not move a window.");
        }

        var activeWindow = windowManager.GetActiveWindow();
        if (activeWindow.IsFailure)
        {
            return Result.Failure(activeWindow.ErrorCode, UserFriendlyMoveFailureMessage);
        }

        var selection = repeatHotkeyCycleService.Select(command, activeWindow.Value);
        if (selection.IsFailure)
        {
            return Result.Failure(selection.ErrorCode, selection.ErrorMessage);
        }

        logger.SnapStarted(selection.Value.Command);
        return SnapActiveWindow(selection.Value.Command, activeWindow.Value);
    }

    /// <summary>
    /// Moves the current active window into the requested snap target.
    /// </summary>
    public Result SnapActiveWindow(SnapCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        logger.SnapStarted(command);

        var activeWindow = windowManager.GetActiveWindow();
        if (activeWindow.IsFailure)
        {
            return Fail(command, activeWindow.ErrorCode, activeWindow.ErrorMessage);
        }

        return SnapActiveWindow(command, activeWindow.Value);
    }

    private Result SnapActiveWindow(SnapCommand command, WindowHandle activeWindow)
    {
        var windowInfo = windowManager.GetWindowInfo(activeWindow);
        if (windowInfo.IsFailure)
        {
            return Fail(command, windowInfo.ErrorCode, windowInfo.ErrorMessage);
        }

        var manageable = windowManager.IsWindowManageable(windowInfo.Value);
        if (manageable.IsFailure)
        {
            return Fail(command, manageable.ErrorCode, manageable.ErrorMessage);
        }

        if (!manageable.Value)
        {
            return Fail(command, ResultErrorCode.WindowNotManageable, "Window is not manageable by WindowSnapper.");
        }

        var monitor = monitorManager.GetMonitorForWindow(activeWindow);
        if (monitor.IsFailure)
        {
            return Fail(command, monitor.ErrorCode, monitor.ErrorMessage);
        }

        var layout = layoutRegistry.FindById(command.LayoutId);
        if (layout is null)
        {
            return Fail(command, ResultErrorCode.NotFound, $"Layout '{command.LayoutId}' was not found.");
        }

        var targetRect = layoutEngine.CalculateTargetRect(monitor.Value, layout, command.ZoneId);
        if (targetRect.IsFailure)
        {
            return Fail(command, targetRect.ErrorCode, targetRect.ErrorMessage);
        }

        ShowOverlayPreviewIfEnabled(command, monitor.Value, targetRect.Value);

        if (windowInfo.Value.IsMaximized)
        {
            var restore = windowManager.RestoreWindow(activeWindow);
            if (restore.IsFailure)
            {
                return Fail(command, restore.ErrorCode, restore.ErrorMessage);
            }
        }

        var move = windowManager.MoveWindow(activeWindow, targetRect.Value);
        if (move.IsFailure)
        {
            return Fail(command, move.ErrorCode, move.ErrorMessage);
        }

        logger.SnapSucceeded(command);
        return Result.Success();
    }

    private Result Fail(SnapCommand command, ResultErrorCode errorCode, string diagnosticMessage)
    {
        logger.SnapFailed(command, errorCode, diagnosticMessage);
        return Result.Failure(errorCode, UserFriendlyMoveFailureMessage);
    }

    private void ShowOverlayPreviewIfEnabled(SnapCommand command, MonitorInfo monitor, RectInt targetBounds)
    {
        if (!overlayPreviewOptions.IsEnabled)
        {
            return;
        }

        try
        {
            var preview = overlayPreviewService.ShowPreview(
                monitor,
                targetBounds,
                overlayPreviewOptions.EffectiveOpacity);
            if (preview.IsFailure)
            {
                logger.SnapFailed(
                    command,
                    preview.ErrorCode,
                    preview.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            logger.SnapFailed(
                command,
                ResultErrorCode.Unknown,
                ex.Message);
        }
    }
}
