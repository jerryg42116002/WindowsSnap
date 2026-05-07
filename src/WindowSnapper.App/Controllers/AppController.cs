using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using WindowSnapper.App.Commands;
using WindowSnapper.App.Composition;
using WindowSnapper.App.ViewModels;
using WindowSnapper.Core.Results;
using WindowSnapper.Hotkeys;
using WindowSnapper.Snap;
using WindowSnapper.Storage;
using WindowSnapper.Tray;

namespace WindowSnapper.App.Controllers;

internal sealed class AppController : IDisposable
{
    private const string HotkeyRegistrationFailureMessage = "全局快捷键注册失败。请检查是否与其他程序快捷键冲突。";
    private const string SettingsSaveFailureMessage = "设置保存失败，请稍后重试。";

    private readonly Application application;
    private readonly SettingsStorage settingsStorage;
    private readonly DefaultSettingsFactory defaultSettingsFactory = new();
    private AppSettings settings;
    private MainWindow? mainWindow;
    private SettingsWindow? settingsWindow;
    private MainWindowViewModel? mainWindowViewModel;
    private SettingsViewModel? settingsViewModel;
    private AppServices? services;
    private ITrayIcon? trayIcon;
    private bool disposed;
    private bool exiting;

    public AppController(Application application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        settingsStorage = new SettingsStorage(StoragePaths.CreateDefault());
        settings = defaultSettingsFactory.Create();
    }

    public async Task InitializeAsync()
    {
        Trace.TraceInformation("WindowSnapper starting.");

        var loadedSettings = await settingsStorage.LoadOrCreateAsync();
        if (loadedSettings.IsSuccess)
        {
            settings = loadedSettings.Value;
        }
        else
        {
            Trace.TraceWarning(
                "Settings load failed. ErrorCode={0}; Message={1}",
                loadedSettings.ErrorCode,
                loadedSettings.ErrorMessage);
            settings = defaultSettingsFactory.Create();
            ShowWarning("配置加载失败，已使用默认设置。");
        }

        mainWindowViewModel = CreateMainWindowViewModel();
        mainWindow = new MainWindow(mainWindowViewModel);
        mainWindow.Closing += OnMainWindowClosing;

        services = AppServices.Create(mainWindow, settings);
        services.HotkeyManager.HotkeyPressed += OnHotkeyPressed;

        trayIcon = new NotifyIconTrayIcon();
        trayIcon.CommandRequested += OnTrayCommandRequested;
        trayIcon.Show(new TrayMenuState(settings.HotkeysPaused));

        if (!settings.HotkeysPaused)
        {
            var registration = RegisterDefaultHotkeys();
            if (registration.IsFailure)
            {
                settings = settings with { HotkeysPaused = true };
                await SaveSettingsAsync(settings);
                ShowWarning(HotkeyRegistrationFailureMessage);
            }
        }

        UpdateShellState();

        if (!settings.MinimizeToTray)
        {
            ShowMainWindow();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (trayIcon is not null)
        {
            trayIcon.CommandRequested -= OnTrayCommandRequested;
            trayIcon.Dispose();
            trayIcon = null;
        }

        if (services is not null)
        {
            services.HotkeyManager.HotkeyPressed -= OnHotkeyPressed;
            services.Dispose();
            services = null;
        }

        if (mainWindow is not null)
        {
            mainWindow.Closing -= OnMainWindowClosing;
        }

        Trace.TraceInformation("WindowSnapper exiting.");
        disposed = true;
    }

    public void ExitApplication()
    {
        if (exiting)
        {
            return;
        }

        exiting = true;
        Dispose();
        application.Shutdown();
    }

    private MainWindowViewModel CreateMainWindowViewModel()
    {
        return new MainWindowViewModel
        {
            OpenSettingsCommand = new RelayCommand(OpenSettingsWindow),
            ToggleHotkeysCommand = new AsyncRelayCommand(ToggleHotkeysPausedAsync),
            ExitCommand = new RelayCommand(ExitApplication)
        };
    }

    private SettingsViewModel CreateSettingsViewModel()
    {
        return new SettingsViewModel(settings, SaveSettingsAsync, SetHotkeysPausedAsync);
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (exiting)
        {
            return;
        }

        e.Cancel = true;
        if (settings.MinimizeToTray)
        {
            mainWindow?.Hide();
            return;
        }

        ExitApplication();
    }

    private void OnTrayCommandRequested(object? sender, TrayMenuCommandEventArgs e)
    {
        switch (e.Command)
        {
            case TrayMenuCommand.OpenMainWindow:
                ShowMainWindow();
                break;
            case TrayMenuCommand.OpenSettings:
                OpenSettingsWindow();
                break;
            case TrayMenuCommand.ToggleHotkeysPaused:
                RunAsync(ToggleHotkeysPausedAsync);
                break;
            case TrayMenuCommand.Exit:
                ExitApplication();
                break;
        }
    }

    private void ShowMainWindow()
    {
        if (mainWindow is null)
        {
            return;
        }

        mainWindow.Show();
        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }

        mainWindow.Activate();
    }

    private void OpenSettingsWindow()
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Activate();
            return;
        }

        settingsViewModel ??= CreateSettingsViewModel();
        settingsViewModel.Refresh(settings);
        settingsWindow = new SettingsWindow(settingsViewModel);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    private async Task ToggleHotkeysPausedAsync()
    {
        var desiredPaused = !settings.HotkeysPaused;
        var result = await SetHotkeysPausedAsync(desiredPaused, settings with { HotkeysPaused = desiredPaused });
        settingsViewModel?.Refresh(settings);

        if (result.IsFailure)
        {
            ShowWarning(result.ErrorMessage);
        }
    }

    private async Task<Result> SetHotkeysPausedAsync(bool paused, AppSettings updatedSettings)
    {
        ArgumentNullException.ThrowIfNull(updatedSettings);

        Result hotkeyResult = Result.Success();
        if (paused)
        {
            hotkeyResult = services?.HotkeyManager.UnregisterAll() ?? Result.Success();
        }
        else
        {
            hotkeyResult = RegisterDefaultHotkeys();
            if (hotkeyResult.IsFailure)
            {
                settings = updatedSettings with { HotkeysPaused = true };
                _ = services?.HotkeyManager.UnregisterAll();
                await SaveSettingsAsync(settings);
                return Result.Failure(hotkeyResult.ErrorCode, HotkeyRegistrationFailureMessage);
            }
        }

        settings = updatedSettings with { HotkeysPaused = paused };
        var saveResult = await SaveSettingsAsync(settings);
        if (hotkeyResult.IsFailure)
        {
            return Result.Failure(hotkeyResult.ErrorCode, "快捷键状态更新失败，请稍后重试。");
        }

        return saveResult;
    }

    private async Task<Result> SaveSettingsAsync(AppSettings updatedSettings)
    {
        ArgumentNullException.ThrowIfNull(updatedSettings);

        settings = updatedSettings;
        services?.ApplySettings(settings);
        UpdateShellState();

        var saveResult = await settingsStorage.SaveAsync(settings);
        if (saveResult.IsFailure)
        {
            Trace.TraceWarning(
                "Settings save failed. ErrorCode={0}; Message={1}",
                saveResult.ErrorCode,
                saveResult.ErrorMessage);
            return Result.Failure(saveResult.ErrorCode, SettingsSaveFailureMessage);
        }

        return Result.Success();
    }

    private Result RegisterDefaultHotkeys()
    {
        if (services is null)
        {
            return Result.Failure(ResultErrorCode.NotFound, "Application services are not initialized.");
        }

        _ = services.HotkeyManager.UnregisterAll();
        var result = services.HotkeyManager.RegisterDefaultHotkeys();
        if (result.IsFailure)
        {
            Trace.TraceWarning(
                "Default hotkey registration failed. ErrorCode={0}; Message={1}",
                result.ErrorCode,
                result.ErrorMessage);
            _ = services.HotkeyManager.UnregisterAll();
        }
        else
        {
            Trace.TraceInformation("Default hotkeys registered.");
        }

        return result;
    }

    private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
    {
        if (services is null || settings.HotkeysPaused)
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
            ShowInfo(result.ErrorMessage);
        }
    }

    private void UpdateShellState()
    {
        if (mainWindowViewModel is not null)
        {
            mainWindowViewModel.HotkeysPaused = settings.HotkeysPaused;
            mainWindowViewModel.MinimizeToTray = settings.MinimizeToTray;
            mainWindowViewModel.StatusMessage = settings.HotkeysPaused
                ? "WindowSnapper 正在运行，快捷键已暂停"
                : "WindowSnapper 正在运行，快捷键已启用";
        }

        trayIcon?.UpdateState(new TrayMenuState(settings.HotkeysPaused));
    }

    private static void ShowInfo(string message)
    {
        MessageBox.Show(message, "WindowSnapper", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void ShowWarning(string message)
    {
        MessageBox.Show(message, "WindowSnapper", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static void RunAsync(Func<Task> action)
    {
        _ = RunCoreAsync(action);
    }

    private static async Task RunCoreAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Trace.TraceError("Background UI command failed: {0}", ex.Message);
            ShowWarning("操作失败，请稍后重试。");
        }
    }
}
