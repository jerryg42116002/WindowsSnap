using System.Windows;
using System.Windows.Interop;
using WindowSnapper.Core.Geometry;
using WindowSnapper.Win32;

namespace WindowSnapper.App.Overlay;

public partial class OverlayWindow : Window
{
    public const string IgnoredWindowClassName = "WindowSnapperOverlayWindow";

    private readonly Win32OverlayWindowStyleService styleService;
    private bool stylesApplied;

    public OverlayWindow(Win32OverlayWindowStyleService styleService)
    {
        this.styleService = styleService ?? throw new ArgumentNullException(nameof(styleService));

        InitializeComponent();
        Title = IgnoredWindowClassName;
        SourceInitialized += OnSourceInitialized;
    }

    public void Configure(RectInt targetBounds, double dpiScale, double opacity)
    {
        var scale = dpiScale <= 0 ? 1.0 : dpiScale;
        Left = targetBounds.X / scale;
        Top = targetBounds.Y / scale;
        Width = Math.Max(1, targetBounds.Width / scale);
        Height = Math.Max(1, targetBounds.Height / scale);
        Opacity = Math.Clamp(opacity, 0.05, 1.0);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (stylesApplied)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        _ = styleService.ApplyOverlayStyles(handle);
        stylesApplied = true;
    }
}
