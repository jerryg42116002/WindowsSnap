using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Layouts;

/// <summary>
/// Calculates target rectangles for layout zones.
/// </summary>
public sealed class LayoutEngine
{
    private const double BoundaryTolerance = 0.000000001;

    private readonly LayoutValidator validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutEngine"/> class.
    /// </summary>
    public LayoutEngine()
        : this(new LayoutValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutEngine"/> class.
    /// </summary>
    public LayoutEngine(LayoutValidator validator)
    {
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    /// <summary>
    /// Calculates the target rectangle for the requested zone.
    /// </summary>
    public Result<RectInt> CalculateTargetRect(MonitorInfo monitor, LayoutDefinition layout, string zoneId)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

        var validation = validator.Validate(layout);
        if (validation.IsFailure)
        {
            return Result<RectInt>.Failure(validation.ErrorCode, validation.ErrorMessage);
        }

        var zone = layout.Zones.FirstOrDefault(candidate => string.Equals(candidate.Id, zoneId, StringComparison.Ordinal));
        if (zone is null)
        {
            return Result<RectInt>.Failure(
                ResultErrorCode.NotFound,
                $"Zone '{zoneId}' was not found in layout '{layout.Id}'.");
        }

        var target = CalculateFromWorkArea(monitor.WorkArea, layout, zone);
        if (target.Width <= 0 || target.Height <= 0)
        {
            return Result<RectInt>.Failure(
                ResultErrorCode.InvalidArgument,
                $"Zone '{zone.Id}' produced an empty target rectangle after applying margin and gap.");
        }

        return Result<RectInt>.Success(target);
    }

    /// <summary>
    /// Calculates the requested zone with its id and target rectangle.
    /// </summary>
    public Result<ZoneRect> CalculateZoneRect(MonitorInfo monitor, LayoutDefinition layout, string zoneId)
    {
        var target = CalculateTargetRect(monitor, layout, zoneId);
        if (target.IsFailure)
        {
            return Result<ZoneRect>.Failure(target.ErrorCode, target.ErrorMessage);
        }

        return Result<ZoneRect>.Success(new ZoneRect(zoneId, target.Value));
    }

    private static RectInt CalculateFromWorkArea(RectInt workArea, LayoutDefinition layout, ZoneDefinition zone)
    {
        var rawLeft = ScaleCoordinate(workArea.X, workArea.Width, zone.X);
        var rawTop = ScaleCoordinate(workArea.Y, workArea.Height, zone.Y);
        var rawRight = ScaleCoordinate(workArea.X, workArea.Width, zone.X + zone.Width);
        var rawBottom = ScaleCoordinate(workArea.Y, workArea.Height, zone.Y + zone.Height);

        var leftInset = GetStartInset(zone.X, layout.Margin, layout.Gap);
        var topInset = GetStartInset(zone.Y, layout.Margin, layout.Gap);
        var rightInset = GetEndInset(zone.X + zone.Width, layout.Margin, layout.Gap);
        var bottomInset = GetEndInset(zone.Y + zone.Height, layout.Margin, layout.Gap);

        var left = rawLeft + leftInset;
        var top = rawTop + topInset;
        var right = rawRight - rightInset;
        var bottom = rawBottom - bottomInset;

        return new RectInt(left, top, right - left, bottom - top);
    }

    private static int ScaleCoordinate(int origin, int size, double ratio)
    {
        return origin + (int)Math.Round(size * ratio, MidpointRounding.AwayFromZero);
    }

    private static int GetStartInset(double normalizedStart, int margin, int gap)
    {
        return IsAtMinimum(normalizedStart) ? margin : gap / 2;
    }

    private static int GetEndInset(double normalizedEnd, int margin, int gap)
    {
        return IsAtMaximum(normalizedEnd) ? margin : gap - (gap / 2);
    }

    private static bool IsAtMinimum(double value)
    {
        return Math.Abs(value) <= BoundaryTolerance;
    }

    private static bool IsAtMaximum(double value)
    {
        return Math.Abs(1.0 - value) <= BoundaryTolerance;
    }
}
