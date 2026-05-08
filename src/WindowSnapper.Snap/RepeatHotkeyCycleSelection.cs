namespace WindowSnapper.Snap;

/// <summary>
/// Describes the snap target selected by repeat-hotkey cycling.
/// </summary>
public sealed record RepeatHotkeyCycleSelection(
    SnapCommand Command,
    int CycleIndex);
