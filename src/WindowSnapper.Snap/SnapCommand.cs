using WindowSnapper.Core.Results;
using WindowSnapper.Hotkeys;
using WindowSnapper.Layouts;

namespace WindowSnapper.Snap;

/// <summary>
/// Describes a request to move the active window into a layout zone.
/// </summary>
public sealed record SnapCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapCommand"/> class.
    /// </summary>
    public SnapCommand(string layoutId, string zoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

        LayoutId = layoutId;
        ZoneId = zoneId;
    }

    /// <summary>
    /// Gets the layout id to use.
    /// </summary>
    public string LayoutId { get; }

    /// <summary>
    /// Gets the zone id inside the layout.
    /// </summary>
    public string ZoneId { get; }

    /// <summary>
    /// Maps a hotkey command to a snap command.
    /// </summary>
    public static Result<SnapCommand> FromHotkeyCommand(HotkeyCommand command)
    {
        return command switch
        {
            HotkeyCommand.SnapLeftHalf => MoveToBuiltin(BuiltinLayouts.LeftHalfId),
            HotkeyCommand.SnapRightHalf => MoveToBuiltin(BuiltinLayouts.RightHalfId),
            HotkeyCommand.SnapTopHalf => MoveToBuiltin(BuiltinLayouts.TopHalfId),
            HotkeyCommand.SnapBottomHalf => MoveToBuiltin(BuiltinLayouts.BottomHalfId),
            HotkeyCommand.SnapZone1 => MoveToBuiltin(BuiltinLayouts.QuadTopLeftId),
            HotkeyCommand.SnapZone2 => MoveToBuiltin(BuiltinLayouts.QuadTopRightId),
            HotkeyCommand.SnapZone3 => MoveToBuiltin(BuiltinLayouts.QuadBottomLeftId),
            HotkeyCommand.SnapZone4 => MoveToBuiltin(BuiltinLayouts.QuadBottomRightId),
            HotkeyCommand.OpenLayoutSelector => Result<SnapCommand>.Failure(
                ResultErrorCode.NotSupported,
                "The layout selector command does not move a window."),
            HotkeyCommand.None => Result<SnapCommand>.Failure(
                ResultErrorCode.InvalidArgument,
                "Hotkey command is required."),
            _ => Result<SnapCommand>.Failure(
                ResultErrorCode.NotSupported,
                $"Hotkey command '{command}' is not mapped to a snap target.")
        };
    }

    private static Result<SnapCommand> MoveToBuiltin(string layoutId)
    {
        return Result<SnapCommand>.Success(new SnapCommand(layoutId, BuiltinLayouts.MainZoneId));
    }
}
