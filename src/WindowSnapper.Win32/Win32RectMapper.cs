using WindowSnapper.Core.Geometry;

namespace WindowSnapper.Win32;

internal static class Win32RectMapper
{
    public static RectInt ToRectInt(NativeMethods.Rect rect)
    {
        return new RectInt(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }
}
