using System.Runtime.InteropServices;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Win32;

/// <summary>
/// Applies overlay window styles through Win32 APIs.
/// </summary>
public sealed class Win32OverlayWindowStyleService
{
    /// <summary>
    /// Applies non-activating, transparent, tool-window styles to an overlay window.
    /// </summary>
    public Result ApplyOverlayStyles(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is required.");
        }

        Marshal.SetLastPInvokeError(0);
        var currentStyle = NativeMethods.GetWindowLong(windowHandle, NativeMethods.GwlExStyle);
        var getError = Marshal.GetLastPInvokeError();
        if (currentStyle == 0 && getError != 0)
        {
            return Win32ErrorMapper.ToFailure(getError, nameof(NativeMethods.GetWindowLong));
        }

        var overlayStyle = currentStyle |
            NativeMethods.WsExNoActivate |
            NativeMethods.WsExTransparent |
            NativeMethods.WsExToolWindow;

        Marshal.SetLastPInvokeError(0);
        var previousStyle = NativeMethods.SetWindowLong(windowHandle, NativeMethods.GwlExStyle, overlayStyle);
        var setError = Marshal.GetLastPInvokeError();
        if (previousStyle == 0 && setError != 0)
        {
            return Win32ErrorMapper.ToFailure(setError, nameof(NativeMethods.SetWindowLong));
        }

        return Result.Success();
    }
}
