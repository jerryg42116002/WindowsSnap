using System.Runtime.InteropServices;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Win32;

/// <summary>
/// Enumerates manageable desktop windows through Win32 APIs.
/// </summary>
public sealed class Win32WindowEnumerator : IWindowEnumerator
{
    private readonly Win32WindowManager windowManager;
    private readonly Win32MonitorManager monitorManager;
    private readonly bool includeMinimizedWindows;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowEnumerator"/> class.
    /// </summary>
    public Win32WindowEnumerator(
        Win32WindowManager windowManager,
        Win32MonitorManager monitorManager,
        bool includeMinimizedWindows = false)
    {
        this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
        this.includeMinimizedWindows = includeMinimizedWindows;
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<WindowInfo>> GetWindows()
    {
        var windows = new List<WindowInfo>();
        var succeeded = NativeMethods.EnumWindows(
            (window, _) =>
            {
                var handle = WindowHandle.FromIntPtr(window);
                var windowInfo = windowManager.GetWindowInfo(handle);
                if (windowInfo.IsFailure)
                {
                    return true;
                }

                var filterInfo = includeMinimizedWindows && windowInfo.Value.IsMinimized
                    ? windowInfo.Value with { IsMinimized = false }
                    : windowInfo.Value;
                var manageable = windowManager.IsWindowManageable(filterInfo);
                if (manageable.IsFailure || !manageable.Value)
                {
                    return true;
                }

                var monitor = monitorManager.GetMonitorForWindow(handle);
                if (!windowInfo.Value.IsMinimized &&
                    monitor.IsSuccess &&
                    WindowFilter.CoversMonitorBounds(windowInfo.Value, monitor.Value))
                {
                    return true;
                }

                windows.Add(windowInfo.Value);
                return true;
            },
            IntPtr.Zero);

        return succeeded
            ? Result<IReadOnlyList<WindowInfo>>.Success(windows)
            : Win32ErrorMapper.ToFailure<IReadOnlyList<WindowInfo>>(
                Marshal.GetLastPInvokeError(),
                nameof(NativeMethods.EnumWindows));
    }
}
