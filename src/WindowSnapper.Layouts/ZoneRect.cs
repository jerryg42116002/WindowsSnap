using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Layouts;

/// <summary>
/// Represents the calculated rectangle for a layout zone.
/// </summary>
/// <param name="ZoneId">The source zone identifier.</param>
/// <param name="Bounds">The calculated target bounds.</param>
public sealed record ZoneRect(string ZoneId, RectInt Bounds);
