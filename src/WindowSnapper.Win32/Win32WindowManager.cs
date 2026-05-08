using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Win32;

/// <summary>
/// Provides window operations through Win32 APIs.
/// </summary>
public sealed class Win32WindowManager : IWindowManager
{
    private readonly Win32MonitorManager monitorManager;
    private readonly WindowFilter windowFilter;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowManager"/> class.
    /// </summary>
    public Win32WindowManager()
        : this(new WindowFilter(), new Win32MonitorManager())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowManager"/> class.
    /// </summary>
    public Win32WindowManager(WindowFilter windowFilter)
        : this(windowFilter, new Win32MonitorManager())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowManager"/> class.
    /// </summary>
    public Win32WindowManager(WindowFilter windowFilter, Win32MonitorManager monitorManager)
    {
        this.windowFilter = windowFilter ?? throw new ArgumentNullException(nameof(windowFilter));
        this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
    }

    /// <inheritdoc />
    public Result<WindowHandle> GetActiveWindow()
    {
        var handle = NativeMethods.GetForegroundWindow();
        return handle == IntPtr.Zero
            ? Result<WindowHandle>.Failure(ResultErrorCode.NotFound, "No foreground window is available.")
            : Result<WindowHandle>.Success(WindowHandle.FromIntPtr(handle));
    }

    /// <inheritdoc />
    public Result<WindowInfo> GetWindowInfo(WindowHandle handle)
    {
        if (handle.IsNone)
        {
            return Result<WindowInfo>.Failure(ResultErrorCode.InvalidArgument, "Window handle is empty.");
        }

        var nativeHandle = handle.ToIntPtr();
        if (!TryGetWindowBounds(nativeHandle, out var bounds))
        {
            return Win32ErrorMapper.ToFailure<WindowInfo>(
                Marshal.GetLastPInvokeError(),
                nameof(NativeMethods.GetWindowRect));
        }

        var processId = GetProcessId(nativeHandle);
        return Result<WindowInfo>.Success(new WindowInfo(
            handle,
            GetWindowText(nativeHandle),
            GetProcessName(processId),
            GetClassName(nativeHandle),
            bounds,
            NativeMethods.IsWindowVisible(nativeHandle),
            NativeMethods.IsIconic(nativeHandle),
            NativeMethods.IsZoomed(nativeHandle)));
    }

    /// <inheritdoc />
    public Result<bool> IsWindowManageable(WindowInfo windowInfo)
    {
        return windowFilter.IsWindowManageable(windowInfo);
    }

    /// <inheritdoc />
    public Result RestoreWindow(WindowHandle handle)
    {
        if (handle.IsNone)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is empty.");
        }

        NativeMethods.ShowWindow(handle.ToIntPtr(), NativeMethods.SwRestore);
        return Result.Success();
    }

    /// <inheritdoc />
    public Result MinimizeWindow(WindowHandle handle)
    {
        if (handle.IsNone)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is empty.");
        }

        NativeMethods.ShowWindow(handle.ToIntPtr(), NativeMethods.SwMinimize);
        return Result.Success();
    }

    /// <inheritdoc />
    public Result HideWindow(WindowHandle handle)
    {
        if (handle.IsNone)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is empty.");
        }

        NativeMethods.ShowWindow(handle.ToIntPtr(), NativeMethods.SwHide);
        return Result.Success();
    }

    /// <inheritdoc />
    public Result MoveWindow(WindowHandle handle, RectInt targetBounds)
    {
        if (handle.IsNone)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is empty.");
        }

        if (targetBounds.Width <= 0 || targetBounds.Height <= 0)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Target bounds must have positive width and height.");
        }

        var windowInfo = GetWindowInfo(handle);
        if (windowInfo.IsFailure)
        {
            return Result.Failure(windowInfo.ErrorCode, windowInfo.ErrorMessage);
        }

        var manageable = IsWindowManageable(windowInfo.Value);
        if (manageable.IsFailure)
        {
            return Result.Failure(manageable.ErrorCode, manageable.ErrorMessage);
        }

        if (!manageable.Value)
        {
            return Result.Failure(ResultErrorCode.WindowNotManageable, "Window is not manageable by WindowSnapper.");
        }

        var monitor = monitorManager.GetMonitorForWindow(handle);
        if (monitor.IsFailure)
        {
            return Result.Failure(monitor.ErrorCode, monitor.ErrorMessage);
        }

        if (WindowFilter.CoversMonitorBounds(windowInfo.Value, monitor.Value))
        {
            return Result.Failure(ResultErrorCode.WindowNotManageable, "Window appears to be full-screen and will not be managed.");
        }

        if (windowInfo.Value.IsMaximized)
        {
            var restore = RestoreWindow(handle);
            if (restore.IsFailure)
            {
                return restore;
            }
        }

        var nativeHandle = handle.ToIntPtr();
        var moveBounds = CalculateOuterBoundsForVisibleTarget(nativeHandle, targetBounds);
        var succeeded = NativeMethods.SetWindowPos(
            nativeHandle,
            IntPtr.Zero,
            moveBounds.X,
            moveBounds.Y,
            moveBounds.Width,
            moveBounds.Height,
            NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);

        return succeeded
            ? Result.Success()
            : Win32ErrorMapper.ToFailure(Marshal.GetLastPInvokeError(), nameof(NativeMethods.SetWindowPos));
    }

    internal static RectInt CalculateOuterBoundsForVisibleTarget(
        RectInt outerBounds,
        RectInt visibleBounds,
        RectInt targetVisibleBounds)
    {
        var leftFrame = visibleBounds.X - outerBounds.X;
        var topFrame = visibleBounds.Y - outerBounds.Y;
        var rightFrame = outerBounds.Right - visibleBounds.Right;
        var bottomFrame = outerBounds.Bottom - visibleBounds.Bottom;

        return new RectInt(
            targetVisibleBounds.X - leftFrame,
            targetVisibleBounds.Y - topFrame,
            targetVisibleBounds.Width + leftFrame + rightFrame,
            targetVisibleBounds.Height + topFrame + bottomFrame);
    }

    private static RectInt CalculateOuterBoundsForVisibleTarget(IntPtr window, RectInt targetVisibleBounds)
    {
        if (!TryGetOuterWindowBounds(window, out var outerBounds) ||
            !TryGetExtendedFrameBounds(window, out var visibleBounds))
        {
            return targetVisibleBounds;
        }

        var moveBounds = CalculateOuterBoundsForVisibleTarget(outerBounds, visibleBounds, targetVisibleBounds);
        return moveBounds.Width <= 0 || moveBounds.Height <= 0
            ? targetVisibleBounds
            : moveBounds;
    }

    private static bool TryGetWindowBounds(IntPtr window, out RectInt bounds)
    {
        if (TryGetExtendedFrameBounds(window, out bounds))
        {
            return true;
        }

        return TryGetOuterWindowBounds(window, out bounds);
    }

    private static bool TryGetExtendedFrameBounds(IntPtr window, out RectInt bounds)
    {
        var dwmResult = NativeMethods.DwmGetWindowAttribute(
            window,
            NativeMethods.DwmExtendedFrameBounds,
            out var extendedBounds,
            Marshal.SizeOf<NativeMethods.Rect>());

        if (dwmResult == 0)
        {
            bounds = Win32RectMapper.ToRectInt(extendedBounds);
            return true;
        }

        bounds = default;
        return false;
    }

    private static bool TryGetOuterWindowBounds(IntPtr window, out RectInt bounds)
    {
        if (NativeMethods.GetWindowRect(window, out var rect))
        {
            bounds = Win32RectMapper.ToRectInt(rect);
            return true;
        }

        bounds = default;
        return false;
    }

    private static uint GetProcessId(IntPtr window)
    {
        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        return processId;
    }

    private static string GetWindowText(IntPtr window)
    {
        var length = NativeMethods.GetWindowTextLength(window);
        if (length <= 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder(length + 1);
        _ = NativeMethods.GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    private static string GetClassName(IntPtr window)
    {
        var className = new StringBuilder(256);
        return NativeMethods.GetClassName(window, className, className.Capacity) <= 0
            ? string.Empty
            : className.ToString();
    }

    private static string GetProcessName(uint processId)
    {
        if (processId == 0)
        {
            return string.Empty;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
        catch (InvalidOperationException)
        {
            return string.Empty;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return string.Empty;
        }
    }
}
