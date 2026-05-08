namespace WindowSnapper.Core.Time;

/// <summary>
/// Provides system time for production services.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <summary>
    /// Gets the shared system clock instance.
    /// </summary>
    public static SystemClock Instance { get; } = new();

    private SystemClock()
    {
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset LocalNow => DateTimeOffset.Now;
}
