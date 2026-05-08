namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Represents a rectangle relative to a monitor work area.
/// </summary>
public readonly record struct RelativeRect(
    double X,
    double Y,
    double Width,
    double Height);
