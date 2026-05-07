using System.Diagnostics;
using WindowSnapper.App.Hotkeys;
using WindowSnapper.App.Logging;
using WindowSnapper.App.Overlay;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Windows;
using WindowSnapper.Hotkeys;
using WindowSnapper.Layouts;
using WindowSnapper.Snap;
using WindowSnapper.Storage;
using WindowSnapper.Win32;

namespace WindowSnapper.App.Composition;

internal sealed class AppServices : IDisposable
{
    private readonly WpfHotkeyRegistrar hotkeyRegistrar;
    private readonly OverlayPreviewService overlayPreviewService;
    private LayoutRegistry layoutRegistry;
    private bool disposed;

    private AppServices(MainWindow mainWindow, AppSettings settings, LayoutRegistry layoutRegistry)
    {
        MainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(layoutRegistry);

        Logger = new TraceWindowSnapLogger();
        overlayPreviewService = new OverlayPreviewService(new Win32OverlayWindowStyleService());
        this.layoutRegistry = layoutRegistry;
        WindowSnapService = CreateWindowSnapService(settings, layoutRegistry);

        hotkeyRegistrar = new WpfHotkeyRegistrar(MainWindow, new Win32HotkeyRegistrar());
        HotkeyManager = new HotkeyManager(hotkeyRegistrar);
    }

    public MainWindow MainWindow { get; }

    public HotkeyManager HotkeyManager { get; }

    public WindowSnapService WindowSnapService { get; private set; }

    public TraceWindowSnapLogger Logger { get; }

    public static AppServices Create(MainWindow mainWindow, AppSettings settings, LayoutRegistry layoutRegistry)
    {
        return new AppServices(mainWindow, settings, layoutRegistry);
    }

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        WindowSnapService = CreateWindowSnapService(settings, layoutRegistry);
    }

    public void ApplyLayouts(LayoutRegistry layoutRegistry, AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(layoutRegistry);
        ArgumentNullException.ThrowIfNull(settings);

        this.layoutRegistry = layoutRegistry;
        WindowSnapService = CreateWindowSnapService(settings, layoutRegistry);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        HotkeyManager.Dispose();
        hotkeyRegistrar.Dispose();
        overlayPreviewService.Dispose();
        disposed = true;
    }

    private WindowSnapService CreateWindowSnapService(AppSettings settings, LayoutRegistry layoutRegistry)
    {
        var monitorManager = new Win32MonitorManager();
        using var currentProcess = Process.GetCurrentProcess();
        var windowFilter = new WindowFilter(
            currentProcess.ProcessName,
            Environment.ProcessId,
            settings.IgnoredWindowClasses,
            settings.IgnoredProcesses);
        IWindowManager windowManager = new Win32WindowManager(windowFilter, monitorManager);
        IMonitorManager monitorService = monitorManager;

        return new WindowSnapService(
            windowManager,
            monitorService,
            new LayoutEngine(),
            Logger,
            layoutRegistry,
            overlayPreviewService,
            new OverlayPreviewOptions(settings.ShowOverlayPreview, settings.OverlayOpacity));
    }
}
