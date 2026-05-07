using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Win32;

/// <summary>
/// Provides monitor discovery through Win32 APIs.
/// </summary>
public sealed class Win32MonitorManager : IMonitorManager
{
    /// <inheritdoc />
    public Result<IReadOnlyList<MonitorInfo>> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        Result<MonitorInfo>? monitorFailure = null;
        var succeeded = NativeMethods.EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (monitor, _, _, _) =>
            {
                var info = CreateMonitorInfo(monitor, dpiScale: 1.0);
                if (info.IsFailure)
                {
                    monitorFailure = info;
                    return false;
                }

                monitors.Add(info.Value);
                return true;
            },
            IntPtr.Zero);

        if (monitorFailure is not null)
        {
            return Result<IReadOnlyList<MonitorInfo>>.Failure(monitorFailure.ErrorCode, monitorFailure.ErrorMessage);
        }

        if (!succeeded)
        {
            return Win32ErrorMapper.ToFailure<IReadOnlyList<MonitorInfo>>(
                Environment.GetLastPInvokeError(),
                nameof(NativeMethods.EnumDisplayMonitors));
        }

        return Result<IReadOnlyList<MonitorInfo>>.Success(monitors);
    }

    /// <inheritdoc />
    public Result<MonitorInfo> GetMonitorForWindow(WindowHandle handle)
    {
        if (handle.IsNone)
        {
            return Result<MonitorInfo>.Failure(ResultErrorCode.InvalidArgument, "Window handle is empty.");
        }

        var nativeHandle = handle.ToIntPtr();
        var monitor = NativeMethods.MonitorFromWindow(nativeHandle, NativeMethods.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return Win32ErrorMapper.ToFailure<MonitorInfo>(
                Environment.GetLastPInvokeError(),
                nameof(NativeMethods.MonitorFromWindow));
        }

        return CreateMonitorInfo(monitor, GetDpiScaleForWindow(nativeHandle));
    }

    /// <inheritdoc />
    public Result<MonitorInfo> GetMonitorForPoint(PointInt point)
    {
        var monitor = NativeMethods.MonitorFromPoint(
            new NativeMethods.Point(point.X, point.Y),
            NativeMethods.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return Win32ErrorMapper.ToFailure<MonitorInfo>(
                Environment.GetLastPInvokeError(),
                nameof(NativeMethods.MonitorFromPoint));
        }

        return CreateMonitorInfo(monitor, dpiScale: 1.0);
    }

    /// <summary>
    /// Selects the monitor whose bounds contain a point.
    /// </summary>
    public static Result<MonitorInfo> SelectMonitorContainingPoint(
        IReadOnlyList<MonitorInfo> monitors,
        PointInt point)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        var monitor = monitors.FirstOrDefault(candidate =>
            point.X >= candidate.Bounds.X &&
            point.X < candidate.Bounds.Right &&
            point.Y >= candidate.Bounds.Y &&
            point.Y < candidate.Bounds.Bottom);

        return monitor is null
            ? Result<MonitorInfo>.Failure(ResultErrorCode.NotFound, "No monitor contains the specified point.")
            : Result<MonitorInfo>.Success(monitor);
    }

    private static Result<MonitorInfo> CreateMonitorInfo(IntPtr monitor, double dpiScale)
    {
        var nativeInfo = NativeMethods.MonitorInfoEx.Create();
        if (!NativeMethods.GetMonitorInfo(monitor, ref nativeInfo))
        {
            return Win32ErrorMapper.ToFailure<MonitorInfo>(
                Environment.GetLastPInvokeError(),
                nameof(NativeMethods.GetMonitorInfo));
        }

        return Result<MonitorInfo>.Success(new MonitorInfo(
            nativeInfo.DeviceName,
            nativeInfo.DeviceName,
            Win32RectMapper.ToRectInt(nativeInfo.Monitor),
            Win32RectMapper.ToRectInt(nativeInfo.WorkArea),
            (nativeInfo.Flags & NativeMethods.MonitorInfoPrimary) == NativeMethods.MonitorInfoPrimary,
            dpiScale));
    }

    private static double GetDpiScaleForWindow(IntPtr window)
    {
        var dpi = NativeMethods.GetDpiForWindow(window);
        return dpi == 0 ? 1.0 : dpi / 96.0;
    }
}
