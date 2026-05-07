using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Core.Tests.Geometry;

public sealed class RectIntTests
{
    [Fact]
    public void RightAndBottomUseOriginPlusSize()
    {
        var rect = new RectInt(10, 20, 300, 400);

        Assert.Equal(310, rect.Right);
        Assert.Equal(420, rect.Bottom);
    }

    [Fact]
    public void RightAndBottomSupportNegativeScreenCoordinates()
    {
        var rect = new RectInt(-1920, -100, 960, 500);

        Assert.Equal(-960, rect.Right);
        Assert.Equal(400, rect.Bottom);
    }
}
