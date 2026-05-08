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

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowEnumerator"/> class.
    /// </summary>
    public Win32WindowEnumerator(Win32WindowManager windowManager, Win32MonitorManager monitorManager)
    {
        this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
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

                var manageable = windowManager.IsWindowManageable(windowInfo.Value);
                if (manageable.IsFailure || !manageable.Value)
                {
                    return true;
                }

                var monitor = monitorManager.GetMonitorForWindow(handle);
                if (monitor.IsSuccess &&
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
