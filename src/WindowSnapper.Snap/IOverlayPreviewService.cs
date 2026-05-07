using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Snap;

/// <summary>
/// Shows a transient preview for a calculated snap target.
/// </summary>
public interface IOverlayPreviewService
{
    /// <summary>
    /// Shows the preview rectangle.
    /// </summary>
    Result ShowPreview(MonitorInfo monitor, RectInt targetBounds, double opacity);
}
