using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Windows;
using WindowSnapper.Core.Workspaces;

namespace WindowSnapper.Core.Tests.Workspaces;

public sealed class WorkspaceWindowMatcherTests
{
    [Fact]
    public void FindsWindowByProcessNameAndClassName()
    {
        var matcher = new WorkspaceWindowMatcher();
        var snapshot = CreateSnapshot("notepad", "Notepad");
        var windows = new[] { CreateWindow(1, "notepad", "Notepad") };

        var match = matcher.FindMatch(snapshot, windows);

        Assert.NotNull(match);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(1)), match.Handle);
    }

    [Fact]
    public void DoesNotMatchDifferentClassName()
    {
        var matcher = new WorkspaceWindowMatcher();
        var snapshot = CreateSnapshot("notepad", "Notepad");
        var windows = new[] { CreateWindow(1, "notepad", "OtherClass") };

        var match = matcher.FindMatch(snapshot, windows);

        Assert.Null(match);
    }

    [Fact]
    public void RepeatedMatchesUseNextUnusedWindow()
    {
        var matcher = new WorkspaceWindowMatcher();
        var snapshot = CreateSnapshot("notepad", "Notepad");
        var windows = new[]
        {
            CreateWindow(1, "notepad", "Notepad"),
            CreateWindow(2, "notepad", "Notepad")
        };

        var first = matcher.FindMatch(snapshot, windows);
        var second = matcher.FindMatch(snapshot, windows);

        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(1)), first?.Handle);
        Assert.Equal(WindowHandle.FromIntPtr(new IntPtr(2)), second?.Handle);
    }

    private static WorkspaceWindowSnapshot CreateSnapshot(string processName, string className)
    {
        return new WorkspaceWindowSnapshot(
            processName,
            className,
            "DISPLAY1",
            new RelativeRect(0, 0, 1, 1),
            WorkspaceWindowState.Normal);
    }

    private static WindowInfo CreateWindow(int handle, string processName, string className)
    {
        return new WindowInfo(
            WindowHandle.FromIntPtr(new IntPtr(handle)),
            "Sensitive Title",
            processName,
            className,
            new RectInt(0, 0, 800, 600),
            true,
            false,
            false);
    }
}
