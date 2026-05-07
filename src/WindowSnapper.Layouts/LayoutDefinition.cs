namespace WindowSnapper.Layouts;

/// <summary>
/// Defines a named collection of normalized window zones.
/// </summary>
/// <param name="Id">The stable layout identifier.</param>
/// <param name="Name">The display name.</param>
/// <param name="Version">The layout schema version.</param>
/// <param name="Gap">The pixel gap between internal zone edges.</param>
/// <param name="Margin">The pixel margin at monitor work-area edges.</param>
/// <param name="Zones">The zones contained in this layout.</param>
public sealed record LayoutDefinition(
    string Id,
    string Name,
    int Version,
    int Gap,
    int Margin,
    IReadOnlyList<ZoneDefinition> Zones);
