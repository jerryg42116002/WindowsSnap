namespace WindowSnapper.Layouts.Tests;

public sealed class LayoutValidatorTests
{
    private readonly LayoutValidator validator = new();

    [Fact]
    public void FailsWhenZoneRightExceedsOne()
    {
        var layout = CreateLayout(new ZoneDefinition("bad", "Bad", 0.8, 0, 0.3, 1));

        var errors = validator.GetErrors(layout);
        var result = validator.Validate(layout);

        Assert.False(result.IsSuccess);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneRightOutOfRange);
    }

    [Fact]
    public void FailsWhenZoneBottomExceedsOne()
    {
        var layout = CreateLayout(new ZoneDefinition("bad", "Bad", 0, 0.8, 1, 0.3));

        var errors = validator.GetErrors(layout);
        var result = validator.Validate(layout);

        Assert.False(result.IsSuccess);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneBottomOutOfRange);
    }

    [Fact]
    public void ValidatesRequiredLayoutAndZoneFields()
    {
        var layout = new LayoutDefinition(
            string.Empty,
            string.Empty,
            Version: 1,
            Gap: -1,
            Margin: -1,
            [new ZoneDefinition(string.Empty, string.Empty, -0.1, -0.1, 0, 0)]);

        var errors = validator.GetErrors(layout);

        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.LayoutIdRequired);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.LayoutNameRequired);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.GapCannotBeNegative);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.MarginCannotBeNegative);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneIdRequired);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneNameRequired);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneXCannotBeNegative);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneYCannotBeNegative);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneWidthMustBePositive);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZoneHeightMustBePositive);
    }

    [Fact]
    public void FailsWhenNoZonesExist()
    {
        var layout = new LayoutDefinition("empty", "Empty", Version: 1, Gap: 0, Margin: 0, []);

        var errors = validator.GetErrors(layout);
        var result = validator.Validate(layout);

        Assert.False(result.IsSuccess);
        Assert.Contains(errors, error => error.Code == LayoutValidationErrorCode.ZonesRequired);
    }

    private static LayoutDefinition CreateLayout(ZoneDefinition zone)
    {
        return new LayoutDefinition("test-layout", "Test Layout", Version: 1, Gap: 0, Margin: 0, [zone]);
    }
}
