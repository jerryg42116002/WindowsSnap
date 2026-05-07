using WindowSnapper.Core.Results;

namespace WindowSnapper.Snap;

/// <summary>
/// Logs snap operations without recording window titles or user content.
/// </summary>
public interface IWindowSnapLogger
{
    /// <summary>
    /// Records that a snap operation started.
    /// </summary>
    void SnapStarted(SnapCommand command);

    /// <summary>
    /// Records that a snap operation completed successfully.
    /// </summary>
    void SnapSucceeded(SnapCommand command);

    /// <summary>
    /// Records that a snap operation failed.
    /// </summary>
    void SnapFailed(SnapCommand command, ResultErrorCode errorCode, string diagnosticMessage);
}
