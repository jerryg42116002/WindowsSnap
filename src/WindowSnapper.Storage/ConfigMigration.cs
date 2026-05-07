namespace WindowSnapper.Storage;

/// <summary>
/// Applies schema migrations and default values to settings.
/// </summary>
public sealed class ConfigMigration
{
    /// <summary>
    /// Gets the current settings schema version.
    /// </summary>
    public const int CurrentVersion = 2;

    private readonly DefaultSettingsFactory defaultSettingsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigMigration"/> class.
    /// </summary>
    public ConfigMigration()
        : this(new DefaultSettingsFactory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigMigration"/> class.
    /// </summary>
    public ConfigMigration(DefaultSettingsFactory defaultSettingsFactory)
    {
        this.defaultSettingsFactory = defaultSettingsFactory ?? throw new ArgumentNullException(nameof(defaultSettingsFactory));
    }

    /// <summary>
    /// Migrates settings to the current schema and fills missing defaults.
    /// </summary>
    public AppSettings Migrate(AppSettings? settings)
    {
        var defaults = defaultSettingsFactory.Create();
        if (settings is null)
        {
            return defaults;
        }

        return settings with
        {
            Version = CurrentVersion,
            Theme = UseDefaultWhenBlank(settings.Theme, defaults.Theme),
            Language = UseDefaultWhenBlank(settings.Language, defaults.Language),
            OverlayOpacity = settings.OverlayOpacity <= 0 ? defaults.OverlayOpacity : settings.OverlayOpacity,
            DefaultGap = settings.DefaultGap < 0 ? defaults.DefaultGap : settings.DefaultGap,
            DefaultMargin = settings.DefaultMargin < 0 ? defaults.DefaultMargin : settings.DefaultMargin,
            IgnoredProcesses = UseDefaultWhenEmpty(settings.IgnoredProcesses, defaults.IgnoredProcesses),
            IgnoredWindowClasses = UseDefaultWhenEmpty(settings.IgnoredWindowClasses, defaults.IgnoredWindowClasses)
        };
    }

    private static string UseDefaultWhenBlank(string value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private static IReadOnlyList<string> UseDefaultWhenEmpty(IReadOnlyList<string>? values, IReadOnlyList<string> defaultValues)
    {
        return values is { Count: > 0 } ? values : defaultValues;
    }
}
