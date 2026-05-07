using WindowSnapper.Core.Results;

namespace WindowSnapper.Snap;

/// <summary>
/// Discards snap operation logs.
/// </summary>
public sealed class NullWindowSnapLogger : IWindowSnapLogger
{
    /// <summary>
    /// Gets the shared null logger instance.
    /// </summary>
    public static NullWindowSnapLogger Instance { get; } = new();

    private NullWindowSnapLogger()
    {
    }

    /// <inheritdoc />
    public void SnapStarted(SnapCommand command)
    {
    }

    /// <inheritdoc />
    public void SnapSucceeded(SnapCommand command)
    {
    }

    /// <inheritdoc />
    public void SnapFailed(SnapCommand command, ResultErrorCode errorCode, string diagnosticMessage)
    {
    }
}
