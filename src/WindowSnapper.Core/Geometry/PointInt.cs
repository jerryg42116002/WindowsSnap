namespace WindowSnapper.Core.Geometry;

/// <summary>
/// Represents a point in integer screen coordinates.
/// </summary>
/// <param name="X">The horizontal coordinate.</param>
/// <param name="Y">The vertical coordinate.</param>
public readonly record struct PointInt(int X, int Y);
