namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Stores non-sensitive information needed to restore a window position.
/// </summary>
public sealed record WorkspaceWindowSnapshot(
    string ProcessName,
    string ClassName,
    string MonitorDeviceName,
    RelativeRect RelativeRect,
    WorkspaceWindowState WindowState);
