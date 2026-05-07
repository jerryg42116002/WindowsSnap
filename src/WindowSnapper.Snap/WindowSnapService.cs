using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSnapService"/> class.
    /// </summary>
    public WindowSnapService(
        IWindowManager windowManager,
        IMonitorManager monitorManager,
        LayoutEngine layoutEngine,
        IWindowSnapLogger? logger = null,
        LayoutRegistry? layoutRegistry = null)
    {
        this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
        this.layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
        this.layoutRegistry = layoutRegistry ?? LayoutRegistry.Create(Array.Empty<LayoutDefinition>());
        this.logger = logger ?? NullWindowSnapLogger.Instance;
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

        var windowInfo = windowManager.GetWindowInfo(activeWindow.Value);
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

        var monitor = monitorManager.GetMonitorForWindow(activeWindow.Value);
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

        if (windowInfo.Value.IsMaximized)
        {
            var restore = windowManager.RestoreWindow(activeWindow.Value);
            if (restore.IsFailure)
            {
                return Fail(command, restore.ErrorCode, restore.ErrorMessage);
            }
        }

        var move = windowManager.MoveWindow(activeWindow.Value, targetRect.Value);
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
}
