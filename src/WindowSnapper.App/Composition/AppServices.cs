using System.Diagnostics;
using WindowSnapper.App.Hotkeys;
using WindowSnapper.App.Logging;
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
    private bool disposed;

    private AppServices(MainWindow mainWindow, AppSettings settings)
    {
        MainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        ArgumentNullException.ThrowIfNull(settings);

        Logger = new TraceWindowSnapLogger();
        WindowSnapService = CreateWindowSnapService(settings);

        hotkeyRegistrar = new WpfHotkeyRegistrar(MainWindow, new Win32HotkeyRegistrar());
        HotkeyManager = new HotkeyManager(hotkeyRegistrar);
    }

    public MainWindow MainWindow { get; }

    public HotkeyManager HotkeyManager { get; }

    public WindowSnapService WindowSnapService { get; private set; }

    public TraceWindowSnapLogger Logger { get; }

    public static AppServices Create(MainWindow mainWindow, AppSettings settings)
    {
        return new AppServices(mainWindow, settings);
    }

    public void ApplySettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        WindowSnapService = CreateWindowSnapService(settings);
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

    private WindowSnapService CreateWindowSnapService(AppSettings settings)
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
            Logger);
    }
}
