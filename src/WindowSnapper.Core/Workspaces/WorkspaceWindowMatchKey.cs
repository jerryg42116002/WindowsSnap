using WindowSnapper.Core.Windows;

namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Identifies a window without using privacy-sensitive title or path data.
/// </summary>
public sealed record WorkspaceWindowMatchKey(string ProcessName, string ClassName)
{
    /// <summary>
    /// Creates a match key from a live window.
    /// </summary>
    public static WorkspaceWindowMatchKey FromWindow(WindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return new WorkspaceWindowMatchKey(window.ProcessName, window.ClassName);
    }

    /// <summary>
    /// Creates a match key from a snapshot entry.
    /// </summary>
    public static WorkspaceWindowMatchKey FromSnapshot(WorkspaceWindowSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new WorkspaceWindowMatchKey(snapshot.ProcessName, snapshot.ClassName);
    }
}
