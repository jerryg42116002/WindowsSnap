namespace WindowSnapper.Snap;

/// <summary>
/// Controls whether and how the target rectangle preview is shown.
/// </summary>
/// <param name="IsEnabled">Whether overlay preview is enabled.</param>
/// <param name="Opacity">The requested overlay opacity.</param>
public sealed record OverlayPreviewOptions(bool IsEnabled, double Opacity)
{
    /// <summary>
    /// Gets the default overlay opacity.
    /// </summary>
    public const double DefaultOpacity = 0.35;

    /// <summary>
    /// Gets the default enabled overlay preview options.
    /// </summary>
    public static OverlayPreviewOptions Default { get; } = new(IsEnabled: true, DefaultOpacity);

    /// <summary>
    /// Gets disabled overlay preview options.
    /// </summary>
    public static OverlayPreviewOptions Disabled { get; } = new(IsEnabled: false, DefaultOpacity);

    /// <summary>
    /// Gets the opacity clamped to a valid WPF opacity range.
    /// </summary>
    public double EffectiveOpacity => Math.Clamp(Opacity, 0.05, 1.0);
}
