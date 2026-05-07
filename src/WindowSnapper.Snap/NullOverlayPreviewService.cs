using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Snap;

/// <summary>
/// Overlay preview service used when preview is disabled or not available.
/// </summary>
public sealed class NullOverlayPreviewService : IOverlayPreviewService
{
    /// <summary>
    /// Gets the shared no-op overlay preview service.
    /// </summary>
    public static NullOverlayPreviewService Instance { get; } = new();

    private NullOverlayPreviewService()
    {
    }

    /// <inheritdoc />
    public Result ShowPreview(MonitorInfo monitor, RectInt targetBounds, double opacity)
    {
        return Result.Success();
    }
}
