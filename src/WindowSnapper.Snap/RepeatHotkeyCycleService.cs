using WindowSnapper.Core.Results;
using WindowSnapper.Core.Time;
using WindowSnapper.Core.Windows;
using WindowSnapper.Hotkeys;
using WindowSnapper.Layouts;

namespace WindowSnapper.Snap;

/// <summary>
/// Selects snap targets for repeated hotkey presses.
/// </summary>
public sealed class RepeatHotkeyCycleService
{
    private static readonly IReadOnlyDictionary<HotkeyCommand, IReadOnlyList<SnapCommand>> DefaultCycles =
        new Dictionary<HotkeyCommand, IReadOnlyList<SnapCommand>>
        {
            [HotkeyCommand.SnapLeftHalf] =
            [
                Builtin(BuiltinLayouts.LeftHalfId),
                Builtin(BuiltinLayouts.LeftOneThirdId),
                Builtin(BuiltinLayouts.LeftTwoThirdsId),
                Builtin(BuiltinLayouts.QuadTopLeftId),
                Builtin(BuiltinLayouts.QuadBottomLeftId)
            ],
            [HotkeyCommand.SnapRightHalf] =
            [
                Builtin(BuiltinLayouts.RightHalfId),
                Builtin(BuiltinLayouts.RightOneThirdId),
                Builtin(BuiltinLayouts.RightTwoThirdsId),
                Builtin(BuiltinLayouts.QuadTopRightId),
                Builtin(BuiltinLayouts.QuadBottomRightId)
            ],
            [HotkeyCommand.SnapTopHalf] =
            [
                Builtin(BuiltinLayouts.TopHalfId),
                Builtin(BuiltinLayouts.QuadTopLeftId),
                Builtin(BuiltinLayouts.QuadTopRightId)
            ],
            [HotkeyCommand.SnapBottomHalf] =
            [
                Builtin(BuiltinLayouts.BottomHalfId),
                Builtin(BuiltinLayouts.QuadBottomLeftId),
                Builtin(BuiltinLayouts.QuadBottomRightId)
            ],
            [HotkeyCommand.SnapZone1] = [Builtin(BuiltinLayouts.QuadTopLeftId)],
            [HotkeyCommand.SnapZone2] = [Builtin(BuiltinLayouts.QuadTopRightId)],
            [HotkeyCommand.SnapZone3] = [Builtin(BuiltinLayouts.QuadBottomLeftId)],
            [HotkeyCommand.SnapZone4] = [Builtin(BuiltinLayouts.QuadBottomRightId)]
        };

    private readonly IClock clock;
    private readonly RepeatHotkeyCycleOptions options;
    private WindowHandle? lastWindow;
    private HotkeyCommand? lastCommand;
    private DateTimeOffset? lastTriggeredUtc;
    private int lastCycleIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepeatHotkeyCycleService"/> class.
    /// </summary>
    public RepeatHotkeyCycleService(
        IClock? clock = null,
        RepeatHotkeyCycleOptions? options = null)
    {
        this.clock = clock ?? SystemClock.Instance;
        this.options = options ?? RepeatHotkeyCycleOptions.Default;
    }

    /// <summary>
    /// Selects the next snap command for the specified hotkey and window.
    /// </summary>
    public Result<RepeatHotkeyCycleSelection> Select(HotkeyCommand command, WindowHandle window)
    {
        var targets = GetTargets(command);
        if (targets.IsFailure)
        {
            return Result<RepeatHotkeyCycleSelection>.Failure(targets.ErrorCode, targets.ErrorMessage);
        }

        if (window.IsNone)
        {
            return Result<RepeatHotkeyCycleSelection>.Failure(
                ResultErrorCode.InvalidArgument,
                "Window handle is empty.");
        }

        var now = clock.UtcNow;
        var cycleIndex = ShouldReset(command, window, now, targets.Value.Count)
            ? 0
            : (lastCycleIndex + 1) % targets.Value.Count;

        lastWindow = window;
        lastCommand = command;
        lastTriggeredUtc = now;
        lastCycleIndex = cycleIndex;

        return Result<RepeatHotkeyCycleSelection>.Success(new RepeatHotkeyCycleSelection(
            targets.Value[cycleIndex],
            cycleIndex));
    }

    private static Result<IReadOnlyList<SnapCommand>> GetTargets(HotkeyCommand command)
    {
        if (command == HotkeyCommand.None)
        {
            return Result<IReadOnlyList<SnapCommand>>.Failure(
                ResultErrorCode.InvalidArgument,
                "Hotkey command is required.");
        }

        if (!DefaultCycles.TryGetValue(command, out var targets))
        {
            return Result<IReadOnlyList<SnapCommand>>.Failure(
                ResultErrorCode.NotSupported,
                $"Hotkey command '{command}' is not mapped to a snap target.");
        }

        return Result<IReadOnlyList<SnapCommand>>.Success(targets);
    }

    private bool ShouldReset(
        HotkeyCommand command,
        WindowHandle window,
        DateTimeOffset now,
        int targetCount)
    {
        if (targetCount <= 1)
        {
            return true;
        }

        if (lastWindow is null || lastCommand is null || lastTriggeredUtc is null)
        {
            return true;
        }

        if (lastWindow.Value != window || lastCommand.Value != command)
        {
            return true;
        }

        var elapsed = now - lastTriggeredUtc.Value;
        return elapsed < TimeSpan.Zero || elapsed > options.EffectiveResetAfter;
    }

    private static SnapCommand Builtin(string layoutId)
    {
        return new SnapCommand(layoutId, BuiltinLayouts.MainZoneId);
    }
}
