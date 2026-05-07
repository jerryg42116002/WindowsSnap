using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Snap;
using WindowSnapper.Win32;

namespace WindowSnapper.App.Overlay;

internal sealed class OverlayPreviewService : IOverlayPreviewService, IDisposable
{
    private static readonly TimeSpan PreviewDuration = TimeSpan.FromMilliseconds(350);

    private readonly Win32OverlayWindowStyleService styleService;
    private OverlayWindow? currentWindow;
    private DispatcherTimer? closeTimer;

    public OverlayPreviewService(Win32OverlayWindowStyleService styleService)
    {
        this.styleService = styleService ?? throw new ArgumentNullException(nameof(styleService));
    }

    public Result ShowPreview(MonitorInfo monitor, RectInt targetBounds, double opacity)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        if (targetBounds.Width <= 0 || targetBounds.Height <= 0)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Overlay preview target must have positive size.");
        }

        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (!dispatcher.CheckAccess())
        {
            return dispatcher.Invoke(() => ShowPreview(monitor, targetBounds, opacity));
        }

        try
        {
            CloseCurrentWindow();

            currentWindow = new OverlayWindow(styleService);
            currentWindow.Configure(targetBounds, monitor.DpiScale, opacity);
            currentWindow.Show();

            closeTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = PreviewDuration
            };
            closeTimer.Tick += OnCloseTimerTick;
            closeTimer.Start();

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning("Overlay preview failed: {0}", ex.Message);
            return Result.Failure(ResultErrorCode.PlatformCallFailed, "Overlay preview could not be shown.");
        }
    }

    public void Dispose()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(CloseCurrentWindow);
            return;
        }

        CloseCurrentWindow();
    }

    private void OnCloseTimerTick(object? sender, EventArgs e)
    {
        CloseCurrentWindow();
    }

    private void CloseCurrentWindow()
    {
        if (closeTimer is not null)
        {
            closeTimer.Stop();
            closeTimer.Tick -= OnCloseTimerTick;
            closeTimer = null;
        }

        if (currentWindow is null)
        {
            return;
        }

        currentWindow.Close();
        currentWindow = null;
    }
}
