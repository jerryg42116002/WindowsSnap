using WindowSnapper.Core.Results;

namespace WindowSnapper.Layouts;

/// <summary>
/// Validates layout definitions before calculation or persistence.
/// </summary>
public sealed class LayoutValidator
{
    /// <summary>
    /// Validates a layout definition.
    /// </summary>
    public Result Validate(LayoutDefinition? layout)
    {
        var errors = GetErrors(layout);
        if (errors.Count == 0)
        {
            return Result.Success();
        }

        return Result.Failure(
            ResultErrorCode.InvalidArgument,
            string.Join("; ", errors.Select(error => error.Message)));
    }

    /// <summary>
    /// Gets all validation errors for a layout definition.
    /// </summary>
    public IReadOnlyList<LayoutValidationError> GetErrors(LayoutDefinition? layout)
    {
        if (layout is null)
        {
            return [new LayoutValidationError(LayoutValidationErrorCode.LayoutIdRequired, "Layout is required.")];
        }

        var errors = new List<LayoutValidationError>();

        if (string.IsNullOrWhiteSpace(layout.Id))
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.LayoutIdRequired, "Layout id is required."));
        }

        if (string.IsNullOrWhiteSpace(layout.Name))
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.LayoutNameRequired, "Layout name is required."));
        }

        if (layout.Gap < 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.GapCannotBeNegative, "Layout gap cannot be negative."));
        }

        if (layout.Margin < 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.MarginCannotBeNegative, "Layout margin cannot be negative."));
        }

        if (layout.Zones is null || layout.Zones.Count == 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZonesRequired, "Layout must contain at least one zone."));
            return errors;
        }

        foreach (var zone in layout.Zones)
        {
            AddZoneErrors(zone, errors);
        }

        return errors;
    }

    private static void AddZoneErrors(ZoneDefinition zone, List<LayoutValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(zone.Id))
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneIdRequired, "Zone id is required."));
        }

        if (string.IsNullOrWhiteSpace(zone.Name))
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneNameRequired, $"Zone '{zone.Id}' name is required."));
        }

        if (zone.Width <= 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneWidthMustBePositive, $"Zone '{zone.Id}' width must be greater than 0."));
        }

        if (zone.Height <= 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneHeightMustBePositive, $"Zone '{zone.Id}' height must be greater than 0."));
        }

        if (zone.X < 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneXCannotBeNegative, $"Zone '{zone.Id}' x cannot be negative."));
        }

        if (zone.Y < 0)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneYCannotBeNegative, $"Zone '{zone.Id}' y cannot be negative."));
        }

        if (zone.X + zone.Width > 1)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneRightOutOfRange, $"Zone '{zone.Id}' x + width cannot exceed 1."));
        }

        if (zone.Y + zone.Height > 1)
        {
            errors.Add(new LayoutValidationError(LayoutValidationErrorCode.ZoneBottomOutOfRange, $"Zone '{zone.Id}' y + height cannot exceed 1."));
        }
    }
}
