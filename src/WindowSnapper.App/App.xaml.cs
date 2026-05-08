using System.Diagnostics;
using System.Windows;
using WindowSnapper.App.Controllers;

namespace WindowSnapper.App;

public partial class App : Application
{
    private AppController? controller;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            controller = new AppController(this);
            await controller.InitializeAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError("WindowSnapper startup failed: {0}", ex.Message);
            MessageBox.Show(
                $"WindowSnapper 启动失败，请稍后重试。{Environment.NewLine}{Environment.NewLine}诊断信息：{ex.Message}",
                "WindowSnapper",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        controller?.Dispose();
        controller = null;

        base.OnExit(e);
    }
}
