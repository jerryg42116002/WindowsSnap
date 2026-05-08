using WindowSnapper.Core.Results;

namespace WindowSnapper.Core.Windows;

/// <summary>
/// Enumerates manageable desktop windows.
/// </summary>
public interface IWindowEnumerator
{
    /// <summary>
    /// Gets current manageable windows.
    /// </summary>
    Result<IReadOnlyList<WindowInfo>> GetWindows();
}
