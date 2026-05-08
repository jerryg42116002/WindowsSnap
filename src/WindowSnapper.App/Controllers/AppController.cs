using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using WindowSnapper.App.Commands;
using WindowSnapper.App.Composition;
using WindowSnapper.App.ViewModels;
using WindowSnapper.Core.Results;
using WindowSnapper.Hotkeys;
using WindowSnapper.Layouts;
using WindowSnapper.Snap;
using WindowSnapper.Storage;
using WindowSnapper.Tray;

namespace WindowSnapper.App.Controllers;

internal sealed class AppController : IDisposable
{
    private const string HotkeyRegistrationDiagnosticPrefix = "全局快捷键注册失败。可能与其他程序快捷键冲突。";
    private const string SettingsSaveFailureMessage = "设置保存失败，请稍后重试。";

    private readonly Application application;
    private readonly LayoutStorage layoutStorage;
    private readonly SettingsStorage settingsStorage;
    private readonly DefaultSettingsFactory defaultSettingsFactory = new();
    private AppSettings settings;
    private LayoutRegistry layoutRegistry = LayoutRegistry.Create(Array.Empty<LayoutDefinition>());
    private MainWindow? mainWindow;
    private SettingsWindow? settingsWindow;
    private LayoutEditorWindow? layoutEditorWindow;
    private MainWindowViewModel? mainWindowViewModel;
    private SettingsViewModel? settingsViewModel;
    private AppServices? services;
    private ITrayIcon? trayIcon;
    private bool disposed;
    private bool exiting;

    public AppController(Application application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        var storagePaths = StoragePaths.CreateDefault();
        settingsStorage = new SettingsStorage(storagePaths);
        layoutStorage = new LayoutStorage(storagePaths);
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

        layoutRegistry = await LoadLayoutRegistryAsync();

        mainWindowViewModel = CreateMainWindowViewModel();
        mainWindow = new MainWindow(mainWindowViewModel);
        mainWindow.Closing += OnMainWindowClosing;

        services = AppServices.Create(mainWindow, settings, layoutRegistry);
        services.HotkeyManager.HotkeyPressed += OnHotkeyPressed;

        trayIcon = new NotifyIconTrayIcon();
        trayIcon.CommandRequested += OnTrayCommandRequested;
        trayIcon.Show(CreateTrayMenuState());

        if (!settings.HotkeysPaused)
        {
            var registration = RegisterDefaultHotkeys();
            if (registration.IsFailure)
            {
                settings = settings with { HotkeysPaused = true };
                await SaveSettingsAsync(settings);
                ShowWarning(CreateHotkeyRegistrationFailureMessage(registration));
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
            OpenLayoutEditorCommand = new RelayCommand(OpenLayoutEditorWindow),
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
            case TrayMenuCommand.SnapLayoutZone:
                SnapFromTray(e.LayoutId, e.ZoneId);
                break;
            case TrayMenuCommand.SaveWorkspaceSnapshot:
                RunAsync(SaveWorkspaceSnapshotAsync);
                break;
            case TrayMenuCommand.RestoreLatestWorkspaceSnapshot:
                RunAsync(RestoreLatestWorkspaceSnapshotAsync);
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

    private void OpenLayoutEditorWindow()
    {
        if (layoutEditorWindow is not null)
        {
            layoutEditorWindow.Activate();
            return;
        }

        var viewModel = new LayoutEditorViewModel(SaveEditedLayoutAsync);
        layoutEditorWindow = new LayoutEditorWindow(viewModel);
        layoutEditorWindow.Closed += (_, _) => layoutEditorWindow = null;
        layoutEditorWindow.Show();
        layoutEditorWindow.Activate();
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
                return Result.Failure(hotkeyResult.ErrorCode, CreateHotkeyRegistrationFailureMessage(hotkeyResult));
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

        var result = services.WindowSnapService.SnapActiveWindow(e.Command);
        if (result.IsFailure)
        {
            if (result.ErrorCode == ResultErrorCode.NotSupported)
            {
                Trace.TraceInformation(
                    "Hotkey command ignored. Command={0}; ErrorCode={1}; Message={2}",
                    e.Command,
                    result.ErrorCode,
                    result.ErrorMessage);
                return;
            }

            ShowInfo(result.ErrorMessage);
        }
    }

    private void SnapFromTray(string? layoutId, string? zoneId)
    {
        if (services is null || settings.HotkeysPaused)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(layoutId) || string.IsNullOrWhiteSpace(zoneId))
        {
            ShowWarning("布局命令无效。");
            return;
        }

        var result = services.WindowSnapService.SnapActiveWindow(new SnapCommand(layoutId, zoneId));
        if (result.IsFailure)
        {
            ShowInfo(result.ErrorMessage);
        }
    }

    private async Task SaveWorkspaceSnapshotAsync()
    {
        if (services is null)
        {
            return;
        }

        var result = await services.WorkspaceSnapshotService.SaveCurrentAsync();
        if (result.IsFailure)
        {
            Trace.TraceWarning(
                "Workspace snapshot save failed. ErrorCode={0}; Message={1}",
                result.ErrorCode,
                result.ErrorMessage);
            ShowWarning("工作区快照保存失败，请稍后重试。");
            return;
        }

        ShowInfo("已保存工作区快照。");
    }

    private async Task RestoreLatestWorkspaceSnapshotAsync()
    {
        if (services is null)
        {
            return;
        }

        var result = await services.WorkspaceSnapshotService.RestoreLatestAsync();
        if (result.IsFailure)
        {
            Trace.TraceWarning(
                "Workspace snapshot restore failed. ErrorCode={0}; Message={1}",
                result.ErrorCode,
                result.ErrorMessage);
            ShowWarning("工作区快照恢复失败，请确认已保存快照且相关窗口仍在运行。");
            return;
        }

        ShowInfo("已恢复最近工作区快照。");
    }

    private async Task<Result> SaveEditedLayoutAsync(LayoutDefinition layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var save = await layoutStorage.SaveLayoutAsync(layout);
        if (save.IsFailure)
        {
            Trace.TraceWarning(
                "Layout editor save failed. LayoutId={0}; ErrorCode={1}; Message={2}",
                layout.Id,
                save.ErrorCode,
                save.ErrorMessage);
            return Result.Failure(save.ErrorCode, $"布局保存失败：{save.ErrorMessage}");
        }

        layoutRegistry = await LoadLayoutRegistryAsync();
        services?.ApplyLayouts(layoutRegistry, settings);
        UpdateShellState();
        return Result.Success();
    }

    private async Task<LayoutRegistry> LoadLayoutRegistryAsync()
    {
        var loadResult = await layoutStorage.LoadLayoutsAsync();
        if (loadResult.IsFailure)
        {
            Trace.TraceWarning(
                "Custom layouts could not be loaded. ErrorCode={0}; Message={1}",
                loadResult.ErrorCode,
                loadResult.ErrorMessage);
            ShowWarning("自定义布局加载失败，将仅使用内置布局。");
            return LayoutRegistry.Create(Array.Empty<LayoutDefinition>());
        }

        foreach (var issue in loadResult.Value.Issues)
        {
            Trace.TraceWarning(
                "Custom layout file failed. File={0}; ErrorCode={1}; Message={2}",
                issue.FileName,
                issue.ErrorCode,
                issue.Message);
        }

        var registry = LayoutRegistry.Create(loadResult.Value.Layouts.Select(loaded =>
            new LayoutRegistrationCandidate(loaded.Layout, loaded.FileName)));

        foreach (var issue in registry.Issues)
        {
            Trace.TraceWarning(
                "Custom layout skipped. Source={0}; LayoutId={1}; Code={2}; Message={3}",
                issue.SourceName ?? issue.LayoutId,
                issue.LayoutId,
                issue.Code,
                issue.Message);
        }

        ShowLayoutWarnings(loadResult.Value.Issues, registry.Issues);
        return registry;
    }

    private void ShowLayoutWarnings(
        IReadOnlyList<LayoutLoadIssue> loadIssues,
        IReadOnlyList<LayoutRegistryIssue> registryIssues)
    {
        if (loadIssues.Count == 0 && registryIssues.Count == 0)
        {
            return;
        }

        var issueNames = loadIssues
            .Select(issue => issue.FileName)
            .Concat(registryIssues.Select(issue => issue.SourceName ?? issue.LayoutId))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();

        var suffix = issueNames.Length == 0
            ? string.Empty
            : $"{Environment.NewLine}{string.Join(Environment.NewLine, issueNames)}";

        ShowWarning($"部分自定义布局未加载：{suffix}");
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

        trayIcon?.UpdateState(CreateTrayMenuState());
    }

    private TrayMenuState CreateTrayMenuState()
    {
        var layouts = layoutRegistry.Layouts
            .Select(layout => new TrayLayoutMenuItem(
                layout.Id,
                layout.Name,
                layout.Zones.Select(zone => new TrayZoneMenuItem(zone.Id, zone.Name)).ToArray()))
            .ToArray();

        return new TrayMenuState(settings.HotkeysPaused, layouts);
    }

    private static void ShowInfo(string message)
    {
        MessageBox.Show(message, "WindowSnapper", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void ShowWarning(string message)
    {
        MessageBox.Show(message, "WindowSnapper", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static string CreateHotkeyRegistrationFailureMessage(Result result)
    {
        return $"{HotkeyRegistrationDiagnosticPrefix}{Environment.NewLine}{Environment.NewLine}诊断信息：{result.ErrorMessage}";
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
