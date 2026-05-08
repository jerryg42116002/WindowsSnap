namespace WindowSnapper.Snap;

/// <summary>
/// Configures repeat-hotkey cycle behavior.
/// </summary>
public sealed record RepeatHotkeyCycleOptions(TimeSpan ResetAfter)
{
    /// <summary>
    /// Gets the default repeat reset interval.
    /// </summary>
    public static RepeatHotkeyCycleOptions Default { get; } = new(TimeSpan.FromMilliseconds(1500));

    /// <summary>
    /// Gets the effective reset interval.
    /// </summary>
    public TimeSpan EffectiveResetAfter => ResetAfter > TimeSpan.Zero
        ? ResetAfter
        : Default.ResetAfter;
}
