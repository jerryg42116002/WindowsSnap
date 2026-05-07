namespace WindowSnapper.Layouts;

/// <summary>
/// Identifies a specific layout validation failure.
/// </summary>
public enum LayoutValidationErrorCode
{
    LayoutIdRequired = 1,
    LayoutNameRequired = 2,
    GapCannotBeNegative = 3,
    MarginCannotBeNegative = 4,
    ZonesRequired = 5,
    ZoneIdRequired = 6,
    ZoneNameRequired = 7,
    ZoneWidthMustBePositive = 8,
    ZoneHeightMustBePositive = 9,
    ZoneXCannotBeNegative = 10,
    ZoneYCannotBeNegative = 11,
    ZoneRightOutOfRange = 12,
    ZoneBottomOutOfRange = 13
}
