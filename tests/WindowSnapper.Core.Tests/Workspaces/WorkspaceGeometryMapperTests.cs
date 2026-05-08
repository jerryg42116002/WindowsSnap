using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Workspaces;

namespace WindowSnapper.Core.Tests.Workspaces;

public sealed class WorkspaceGeometryMapperTests
{
    [Fact]
    public void ConvertsAbsoluteRectToRelativeRect()
    {
        var workArea = new RectInt(0, 0, 1920, 1080);
        var absolute = new RectInt(960, 0, 960, 1080);

        var relative = WorkspaceGeometryMapper.ToRelative(absolute, workArea);

        Assert.Equal(0.5, relative.X);
        Assert.Equal(0, relative.Y);
        Assert.Equal(0.5, relative.Width);
        Assert.Equal(1, relative.Height);
    }

    [Fact]
    public void ConvertsRelativeRectToAbsoluteRectWithNegativeWorkArea()
    {
        var workArea = new RectInt(-1920, 40, 1920, 1040);
        var relative = new RelativeRect(0.5, 0, 0.5, 1);

        var absolute = WorkspaceGeometryMapper.ToAbsolute(relative, workArea);

        Assert.Equal(new RectInt(-960, 40, 960, 1040), absolute);
    }

    [Fact]
    public void ConvertsRelativeRectToAbsoluteRectForPortraitWorkArea()
    {
        var workArea = new RectInt(0, 0, 1080, 1920);
        var relative = new RelativeRect(0, 0.5, 1, 0.5);

        var absolute = WorkspaceGeometryMapper.ToAbsolute(relative, workArea);

        Assert.Equal(new RectInt(0, 960, 1080, 960), absolute);
    }
}
