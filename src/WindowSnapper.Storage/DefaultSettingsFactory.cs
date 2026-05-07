namespace WindowSnapper.Storage;

/// <summary>
/// Creates default application settings.
/// </summary>
public sealed class DefaultSettingsFactory
{
    /// <summary>
    /// Creates a new default settings instance.
    /// </summary>
    public AppSettings Create() => new();
}
