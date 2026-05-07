namespace WindowSnapper.Layouts;

/// <summary>
/// Provides stable built-in layouts for common window snap targets.
/// </summary>
public static class BuiltinLayouts
{
    private const int Version = 1;
    private const string MainZoneId = "main";
    private const string MainZoneName = "Main";

    /// <summary>
    /// Gets all built-in layouts.
    /// </summary>
    public static IReadOnlyList<LayoutDefinition> All { get; } =
    [
        Create("left-half", "Left Half", 0, 0, 0.5, 1),
        Create("right-half", "Right Half", 0.5, 0, 0.5, 1),
        Create("top-half", "Top Half", 0, 0, 1, 0.5),
        Create("bottom-half", "Bottom Half", 0, 0.5, 1, 0.5),
        Create("quad-top-left", "Top Left Quadrant", 0, 0, 0.5, 0.5),
        Create("quad-top-right", "Top Right Quadrant", 0.5, 0, 0.5, 0.5),
        Create("quad-bottom-left", "Bottom Left Quadrant", 0, 0.5, 0.5, 0.5),
        Create("quad-bottom-right", "Bottom Right Quadrant", 0.5, 0.5, 0.5, 0.5),
        Create("left-two-thirds", "Left Two Thirds", 0, 0, 2.0 / 3.0, 1),
        Create("right-one-third", "Right One Third", 2.0 / 3.0, 0, 1.0 / 3.0, 1),
        Create("left-one-third", "Left One Third", 0, 0, 1.0 / 3.0, 1),
        Create("right-two-thirds", "Right Two Thirds", 1.0 / 3.0, 0, 2.0 / 3.0, 1)
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
