namespace WindowSnapper.Layouts;

/// <summary>
/// Provides stable built-in layouts for common window snap targets.
/// </summary>
public static class BuiltinLayouts
{
    private const int Version = 1;
    private const string MainZoneName = "Main";

    /// <summary>
    /// Gets the stable zone id used by single-zone built-in layouts.
    /// </summary>
    public const string MainZoneId = "main";

    /// <summary>
    /// Stable id for the left-half layout.
    /// </summary>
    public const string LeftHalfId = "left-half";

    /// <summary>
    /// Stable id for the right-half layout.
    /// </summary>
    public const string RightHalfId = "right-half";

    /// <summary>
    /// Stable id for the top-half layout.
    /// </summary>
    public const string TopHalfId = "top-half";

    /// <summary>
    /// Stable id for the bottom-half layout.
    /// </summary>
    public const string BottomHalfId = "bottom-half";

    /// <summary>
    /// Stable id for the top-left quadrant layout.
    /// </summary>
    public const string QuadTopLeftId = "quad-top-left";

    /// <summary>
    /// Stable id for the top-right quadrant layout.
    /// </summary>
    public const string QuadTopRightId = "quad-top-right";

    /// <summary>
    /// Stable id for the bottom-left quadrant layout.
    /// </summary>
    public const string QuadBottomLeftId = "quad-bottom-left";

    /// <summary>
    /// Stable id for the bottom-right quadrant layout.
    /// </summary>
    public const string QuadBottomRightId = "quad-bottom-right";

    /// <summary>
    /// Stable id for the left-two-thirds layout.
    /// </summary>
    public const string LeftTwoThirdsId = "left-two-thirds";

    /// <summary>
    /// Stable id for the right-one-third layout.
    /// </summary>
    public const string RightOneThirdId = "right-one-third";

    /// <summary>
    /// Stable id for the left-one-third layout.
    /// </summary>
    public const string LeftOneThirdId = "left-one-third";

    /// <summary>
    /// Stable id for the right-two-thirds layout.
    /// </summary>
    public const string RightTwoThirdsId = "right-two-thirds";

    /// <summary>
    /// Gets all built-in layouts.
    /// </summary>
    public static IReadOnlyList<LayoutDefinition> All { get; } =
    [
        Create(LeftHalfId, "Left Half", 0, 0, 0.5, 1),
        Create(RightHalfId, "Right Half", 0.5, 0, 0.5, 1),
        Create(TopHalfId, "Top Half", 0, 0, 1, 0.5),
        Create(BottomHalfId, "Bottom Half", 0, 0.5, 1, 0.5),
        Create(QuadTopLeftId, "Top Left Quadrant", 0, 0, 0.5, 0.5),
        Create(QuadTopRightId, "Top Right Quadrant", 0.5, 0, 0.5, 0.5),
        Create(QuadBottomLeftId, "Bottom Left Quadrant", 0, 0.5, 0.5, 0.5),
        Create(QuadBottomRightId, "Bottom Right Quadrant", 0.5, 0.5, 0.5, 0.5),
        Create(LeftTwoThirdsId, "Left Two Thirds", 0, 0, 2.0 / 3.0, 1),
        Create(RightOneThirdId, "Right One Third", 2.0 / 3.0, 0, 1.0 / 3.0, 1),
        Create(LeftOneThirdId, "Left One Third", 0, 0, 1.0 / 3.0, 1),
        Create(RightTwoThirdsId, "Right Two Thirds", 1.0 / 3.0, 0, 2.0 / 3.0, 1)
    ];

    /// <summary>
    /// Finds a built-in layout by id.
    /// </summary>
    public static LayoutDefinition? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return All.FirstOrDefault(layout => string.Equals(layout.Id, id, StringComparison.Ordinal));
    }

    private static LayoutDefinition Create(string id, string name, double x, double y, double width, double height)
    {
        return new LayoutDefinition(
            id,
            name,
            Version,
            Gap: 0,
            Margin: 0,
            [new ZoneDefinition(MainZoneId, MainZoneName, x, y, width, height)]);
    }
}
