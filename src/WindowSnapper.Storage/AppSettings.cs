namespace WindowSnapper.Storage;

/// <summary>
/// Represents the persisted application settings.
/// </summary>
public sealed record AppSettings
{
    /// <summary>
    /// Gets the configuration schema version.
    /// </summary>
    public int Version { get; init; } = ConfigMigration.CurrentVersion;

    /// <summary>
    /// Gets the application theme.
    /// </summary>
    public string Theme { get; init; } = "system";

    /// <summary>
    /// Gets the UI language.
    /// </summary>
    public string Language { get; init; } = "zh-CN";

    /// <summary>
    /// Gets whether the app starts with Windows.
    /// </summary>
    public bool StartWithWindows { get; init; }

    /// <summary>
    /// Gets whether closing the app should minimize it to the tray.
    /// </summary>
    public bool MinimizeToTray { get; init; } = true;

    /// <summary>
    /// Gets whether overlay preview is enabled.
    /// </summary>
    public bool ShowOverlayPreview { get; init; } = true;

    /// <summary>
    /// Gets whether global hotkeys are paused.
    /// </summary>
    public bool HotkeysPaused { get; init; }

    /// <summary>
    /// Gets the overlay opacity.
    /// </summary>
    public double OverlayOpacity { get; init; } = 0.35;

    /// <summary>
    /// Gets the default layout gap in pixels.
    /// </summary>
    public int DefaultGap { get; init; }

    /// <summary>
    /// Gets the default layout margin in pixels.
    /// </summary>
    public int DefaultMargin { get; init; }

    /// <summary>
    /// Gets process names ignored by window management.
    /// </summary>
    public IReadOnlyList<string> IgnoredProcesses { get; init; } =
    [
        "explorer.exe",
        "ApplicationFrameHost.exe"
    ];

    /// <summary>
    /// Gets window class names ignored by window management.
    /// </summary>
    public IReadOnlyList<string> IgnoredWindowClasses { get; init; } =
    [
        "Shell_TrayWnd",
        "Progman",
        "WorkerW",
        "WindowSnapperOverlayWindow"
    ];
}
