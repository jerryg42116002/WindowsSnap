using System.Diagnostics;
using System.Windows;
using WindowSnapper.App.Composition;
using WindowSnapper.Hotkeys;
using WindowSnapper.Snap;

namespace WindowSnapper.App;

public partial class App : Application
{
    private AppServices? services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        services = AppServices.Create(mainWindow);
        services.HotkeyManager.HotkeyPressed += OnHotkeyPressed;

        mainWindow.Show();

        var registration = services.HotkeyManager.RegisterDefaultHotkeys();
        if (registration.IsFailure)
        {
            Trace.TraceWarning(
                "Default hotkey registration failed. ErrorCode={0}; Message={1}",
                registration.ErrorCode,
                registration.ErrorMessage);
            MessageBox.Show(
                "部分全局快捷键注册失败。请检查是否与其他程序快捷键冲突。",
                "WindowSnapper",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (services is not null)
        {
            services.HotkeyManager.HotkeyPressed -= OnHotkeyPressed;
            services.Dispose();
            services = null;
        }

        base.OnExit(e);
    }

    private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        if (services is null)
        {
            return;
        }

        var command = SnapCommand.FromHotkeyCommand(e.Command);
        if (command.IsFailure)
        {
            Trace.TraceInformation(
                "Hotkey command ignored. Command={0}; ErrorCode={1}; Message={2}",
                e.Command,
                command.ErrorCode,
                command.ErrorMessage);
            return;
        }

        var result = services.WindowSnapService.SnapActiveWindow(command.Value);
        if (result.IsFailure)
        {
            MessageBox.Show(
                result.ErrorMessage,
                "WindowSnapper",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
