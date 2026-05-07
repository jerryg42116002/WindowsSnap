namespace WindowSnapper.Layouts;

/// <summary>
/// Defines a normalized region inside a layout.
/// </summary>
/// <param name="Id">The stable zone identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="X">The normalized left coordinate, from 0.0 to 1.0.</param>
/// <param name="Y">The normalized top coordinate, from 0.0 to 1.0.</param>
/// <param name="Width">The normalized width, from greater than 0.0 to 1.0.</param>
/// <param name="Height">The normalized height, from greater than 0.0 to 1.0.</param>
public sealed record ZoneDefinition(
    string Id,
    string Name,
    double X,
    double Y,
    double Width,
    double Height);
