using WindowSnapper.Core.Windows;

namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Matches snapshot entries to current windows without using window titles.
/// </summary>
public sealed class WorkspaceWindowMatcher
{
    private readonly HashSet<WindowHandle> usedWindows = [];

    /// <summary>
    /// Finds the first unused live window matching the snapshot process and class.
    /// </summary>
    public WindowInfo? FindMatch(
        WorkspaceWindowSnapshot snapshot,
        IReadOnlyList<WindowInfo> windows)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(windows);

        var key = WorkspaceWindowMatchKey.FromSnapshot(snapshot);
        foreach (var window in windows)
        {
            if (usedWindows.Contains(window.Handle))
            {
                continue;
            }

            if (Matches(key, WorkspaceWindowMatchKey.FromWindow(window)))
            {
                usedWindows.Add(window.Handle);
                return window;
            }
        }

        return null;
    }

    private static bool Matches(WorkspaceWindowMatchKey expected, WorkspaceWindowMatchKey actual)
    {
        return string.Equals(expected.ProcessName, actual.ProcessName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(expected.ClassName, actual.ClassName, StringComparison.Ordinal);
    }
}
