using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.App.ViewModels;

internal sealed record WindowRestoreTarget(
    WindowHandle Handle,
    RectInt Bounds,
    bool WasVisible,
    bool WasMinimized);
