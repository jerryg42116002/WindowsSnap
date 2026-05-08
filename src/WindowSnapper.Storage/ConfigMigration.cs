namespace WindowSnapper.Storage;

/// <summary>
/// Applies schema migrations and default values to settings.
/// </summary>
public sealed class ConfigMigration
{
    /// <summary>
    /// Gets the current settings schema version.
    /// </summary>
    public const int CurrentVersion = 3;

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

        var defaultGap = settings.Version < 3 && settings.DefaultGap == 8
            ? defaults.DefaultGap
            : settings.DefaultGap;
        var defaultMargin = settings.Version < 3 && settings.DefaultMargin == 8
            ? defaults.DefaultMargin
            : settings.DefaultMargin;

        return settings with
        {
            Version = CurrentVersion,
            Theme = UseDefaultWhenBlank(settings.Theme, defaults.Theme),
            Language = UseDefaultWhenBlank(settings.Language, defaults.Language),
            OverlayOpacity = settings.OverlayOpacity is <= 0 or > 1 ? defaults.OverlayOpacity : settings.OverlayOpacity,
            DefaultGap = defaultGap < 0 ? defaults.DefaultGap : defaultGap,
            DefaultMargin = defaultMargin < 0 ? defaults.DefaultMargin : defaultMargin,
            IgnoredProcesses = UseDefaultWhenEmpty(settings.IgnoredProcesses, defaults.IgnoredProcesses),
            IgnoredWindowClasses = MergeRequiredDefaults(settings.IgnoredWindowClasses, defaults.IgnoredWindowClasses)
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

    private static IReadOnlyList<string> MergeRequiredDefaults(
        IReadOnlyList<string>? values,
        IReadOnlyList<string> defaultValues)
    {
        var merged = new List<string>(UseDefaultWhenEmpty(values, defaultValues));
        foreach (var defaultValue in defaultValues)
        {
            if (!merged.Contains(defaultValue, StringComparer.Ordinal))
            {
                merged.Add(defaultValue);
            }
        }

        return merged;
    }
}
