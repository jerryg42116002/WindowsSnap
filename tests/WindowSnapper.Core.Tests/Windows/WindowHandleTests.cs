using WindowSnapper.Core.Windows;

namespace WindowSnapper.Core.Tests.Windows;

public sealed class WindowHandleTests
{
    [Fact]
    public void NoneRepresentsEmptyHandle()
    {
        var handle = WindowHandle.None;

        Assert.Equal(0, handle.Value);
        Assert.True(handle.IsNone);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void NonZeroHandleIsValid()
    {
        var handle = new WindowHandle(1234);

        Assert.Equal(1234, handle.Value);
        Assert.False(handle.IsNone);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void HandlesUseValueEquality()
    {
        var first = new WindowHandle(1234);
        var second = new WindowHandle(1234);
        var third = new WindowHandle(5678);

        Assert.Equal(first, second);
        Assert.NotEqual(first, third);
    }

    [Fact]
    public void ConvertsToAndFromIntPtr()
    {
        var pointer = new IntPtr(1234);

        var handle = WindowHandle.FromIntPtr(pointer);

        Assert.Equal(1234, handle.Value);
        Assert.Equal(pointer, handle.ToIntPtr());
    }
}
