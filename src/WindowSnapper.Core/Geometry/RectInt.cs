namespace WindowSnapper.Core.Geometry;

/// <summary>
/// Represents an integer rectangle in screen coordinates.
/// </summary>
/// <param name="X">The left coordinate.</param>
/// <param name="Y">The top coordinate.</param>
/// <param name="Width">The rectangle width.</param>
/// <param name="Height">The rectangle height.</param>
public readonly record struct RectInt(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Gets the right edge coordinate.
    /// </summary>
    public int Right => X + Width;

    /// <summary>
    /// Gets the bottom edge coordinate.
    /// </summary>
    public int Bottom => Y + Height;
}
