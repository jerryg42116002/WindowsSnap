namespace WindowSnapper.Layouts;

/// <summary>
/// Describes a layout validation failure.
/// </summary>
/// <param name="Code">The structured validation error code.</param>
/// <param name="Message">A readable validation message.</param>
public sealed record LayoutValidationError(LayoutValidationErrorCode Code, string Message);
