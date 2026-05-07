namespace WindowSnapper.Layouts.Tests;

public sealed class BuiltinLayoutsTests
{
    [Fact]
    public void BuiltInLayoutIdsAreNotEmptyAndUnique()
    {
        var ids = BuiltinLayouts.All.Select(layout => layout.Id).ToArray();

        Assert.All(ids, id => Assert.False(string.IsNullOrWhiteSpace(id)));
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("left-half")]
    [InlineData("right-half")]
    [InlineData("top-half")]
    [InlineData("bottom-half")]
    [InlineData("quad-top-left")]
    [InlineData("quad-top-right")]
    [InlineData("quad-bottom-left")]
    [InlineData("quad-bottom-right")]
    [InlineData("left-two-thirds")]
    [InlineData("right-one-third")]
    [InlineData("left-one-third")]
    [InlineData("right-two-thirds")]
    public void RequiredBuiltInLayoutExists(string id)
    {
        Assert.NotNull(BuiltinLayouts.FindById(id));
    }
}
