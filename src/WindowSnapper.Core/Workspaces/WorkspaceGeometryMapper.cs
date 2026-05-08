using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Converts workspace rectangles between absolute pixels and monitor-relative coordinates.
/// </summary>
public static class WorkspaceGeometryMapper
{
    /// <summary>
    /// Converts an absolute rectangle to a rectangle relative to the monitor work area.
    /// </summary>
    public static RelativeRect ToRelative(RectInt absoluteRect, RectInt workArea)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workArea.Width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workArea.Height, 0);

        return new RelativeRect(
            (absoluteRect.X - workArea.X) / (double)workArea.Width,
            (absoluteRect.Y - workArea.Y) / (double)workArea.Height,
            absoluteRect.Width / (double)workArea.Width,
            absoluteRect.Height / (double)workArea.Height);
    }

    /// <summary>
    /// Converts a monitor-relative rectangle to absolute screen pixels.
    /// </summary>
    public static RectInt ToAbsolute(RelativeRect relativeRect, RectInt workArea)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workArea.Width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workArea.Height, 0);

        var x = workArea.X + Round(workArea.Width * relativeRect.X);
        var y = workArea.Y + Round(workArea.Height * relativeRect.Y);
        var width = Round(workArea.Width * relativeRect.Width);
        var height = Round(workArea.Height * relativeRect.Height);

        return new RectInt(x, y, width, height);
    }

    private static int Round(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
