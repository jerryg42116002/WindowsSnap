namespace WindowSnapper.Core.Time;

/// <summary>
/// Provides time for services that need deterministic tests.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Gets the current local time.
    /// </summary>
    DateTimeOffset LocalNow { get; }
}
