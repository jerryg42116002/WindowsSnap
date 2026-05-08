using System.Runtime.InteropServices;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Win32;

/// <summary>
/// Thin wrapper around the Win32 global hotkey registration APIs.
/// </summary>
public sealed class Win32HotkeyRegistrar
{
    /// <summary>
    /// Win32 message sent to the registered window when a hotkey is pressed.
    /// </summary>
    public const int HotkeyMessageId = NativeMethods.WmHotkey;

    /// <summary>
    /// Alt modifier flag used by RegisterHotKey.
    /// </summary>
    public const uint AltModifier = 0x0001;

    /// <summary>
    /// Control modifier flag used by RegisterHotKey.
    /// </summary>
    public const uint ControlModifier = 0x0002;

    /// <summary>
    /// Shift modifier flag used by RegisterHotKey.
    /// </summary>
    public const uint ShiftModifier = 0x0004;

    /// <summary>
    /// Windows modifier flag used by RegisterHotKey.
    /// </summary>
    public const uint WindowsModifier = 0x0008;

    /// <summary>
    /// Avoids repeated WM_HOTKEY messages while a key is held down.
    /// </summary>
    public const uint NoRepeatModifier = 0x4000;

    /// <summary>
    /// Registers a global hotkey for the specified window.
    /// </summary>
    public Result RegisterHotkey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is required to register a hotkey.");
        }

        if (id <= 0)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Hotkey id must be positive.");
        }

        if (virtualKey == 0)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Hotkey virtual key is required.");
        }

        return NativeMethods.RegisterHotKey(windowHandle, id, modifiers, virtualKey)
            ? Result.Success()
            : Win32ErrorMapper.ToFailure(Marshal.GetLastPInvokeError(), nameof(NativeMethods.RegisterHotKey));
    }

    /// <summary>
    /// Unregisters a global hotkey for the specified window.
    /// </summary>
    public Result UnregisterHotkey(IntPtr windowHandle, int id)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Window handle is required to unregister a hotkey.");
        }

        if (id <= 0)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Hotkey id must be positive.");
        }

        return NativeMethods.UnregisterHotKey(windowHandle, id)
            ? Result.Success()
            : Win32ErrorMapper.ToFailure(Marshal.GetLastPInvokeError(), nameof(NativeMethods.UnregisterHotKey));
    }
}
