using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Windows;

namespace WindowSnapper.Win32;

/// <summary>
/// Carries metadata used by <see cref="WindowFilter"/> without requiring real desktop state.
/// </summary>
public sealed record WindowFilterInfo(
    WindowHandle Handle,
    string ProcessName,
    int? ProcessId,
    string ClassName,
    RectInt Bounds,
    bool IsVisible,
    bool IsMinimized);
