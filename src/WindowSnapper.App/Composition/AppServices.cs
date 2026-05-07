using WindowSnapper.App.Hotkeys;
using WindowSnapper.App.Logging;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Windows;
using WindowSnapper.Hotkeys;
using WindowSnapper.Layouts;
using WindowSnapper.Snap;
using WindowSnapper.Win32;

namespace WindowSnapper.App.Composition;

internal sealed class AppServices : IDisposable
{
    private readonly WpfHotkeyRegistrar hotkeyRegistrar;
    private bool disposed;

    private AppServices(MainWindow mainWindow)
    {
        MainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

        var monitorManager = new Win32MonitorManager();
        IWindowManager windowManager = new Win32WindowManager(new WindowFilter(), monitorManager);
        IMonitorManager monitorService = monitorManager;

        Logger = new TraceWindowSnapLogger();
        WindowSnapService = new WindowSnapService(
            windowManager,
            monitorService,
            new LayoutEngine(),
            Logger);

        hotkeyRegistrar = new WpfHotkeyRegistrar(MainWindow, new Win32HotkeyRegistrar());
        HotkeyManager = new HotkeyManager(hotkeyRegistrar);
    }

    public MainWindow MainWindow { get; }

    public HotkeyManager HotkeyManager { get; }

    public WindowSnapService WindowSnapService { get; }

    public TraceWindowSnapLogger Logger { get; }

    public static AppServices Create(MainWindow mainWindow)
    {
        return new AppServices(mainWindow);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        HotkeyManager.Dispose();
        hotkeyRegistrar.Dispose();
        disposed = true;
    }
}
