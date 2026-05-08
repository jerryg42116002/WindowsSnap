using System.Globalization;
using WindowSnapper.Core.Geometry;
using WindowSnapper.Core.Monitors;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Time;
using WindowSnapper.Core.Windows;
using WindowSnapper.Core.Workspaces;

namespace WindowSnapper.Workspaces;

/// <summary>
/// Saves and restores workspace snapshots without depending on UI or platform implementations.
/// </summary>
public sealed class WorkspaceSnapshotService
{
    private const int SnapshotVersion = 1;

    private readonly IWindowEnumerator windowEnumerator;
    private readonly IWindowManager windowManager;
    private readonly IMonitorManager monitorManager;
    private readonly IWorkspaceSnapshotStore snapshotStore;
    private readonly IClock clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceSnapshotService"/> class.
    /// </summary>
    public WorkspaceSnapshotService(
        IWindowEnumerator windowEnumerator,
        IWindowManager windowManager,
        IMonitorManager monitorManager,
        IWorkspaceSnapshotStore snapshotStore,
        IClock? clock = null)
    {
        this.windowEnumerator = windowEnumerator ?? throw new ArgumentNullException(nameof(windowEnumerator));
        this.windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        this.monitorManager = monitorManager ?? throw new ArgumentNullException(nameof(monitorManager));
        this.snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        this.clock = clock ?? SystemClock.Instance;
    }

    /// <summary>
    /// Saves a snapshot of current manageable windows.
    /// </summary>
    public async Task<Result<WorkspaceSnapshot>> SaveCurrentAsync(
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var windows = windowEnumerator.GetWindows();
        if (windows.IsFailure)
        {
            return Result<WorkspaceSnapshot>.Failure(windows.ErrorCode, windows.ErrorMessage);
        }

        var entries = new List<WorkspaceWindowSnapshot>();
        foreach (var window in windows.Value)
        {
            var manageable = windowManager.IsWindowManageable(window);
            if (manageable.IsFailure || !manageable.Value)
            {
                continue;
            }

            var monitor = monitorManager.GetMonitorForWindow(window.Handle);
            if (monitor.IsFailure)
            {
                continue;
            }

            entries.Add(new WorkspaceWindowSnapshot(
                window.ProcessName,
                window.ClassName,
                monitor.Value.DeviceName,
                WorkspaceGeometryMapper.ToRelative(window.Bounds, monitor.Value.WorkArea),
                GetWindowState(window)));
        }

        var createdAt = clock.UtcNow;
        var snapshot = new WorkspaceSnapshot(
            SnapshotVersion,
            CreateSnapshotId(createdAt),
            string.IsNullOrWhiteSpace(name) ? CreateSnapshotName(createdAt) : name,
            createdAt,
            entries);

        var save = await snapshotStore.SaveAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return save.IsFailure
            ? Result<WorkspaceSnapshot>.Failure(save.ErrorCode, save.ErrorMessage)
            : Result<WorkspaceSnapshot>.Success(snapshot);
    }

    /// <summary>
    /// Restores the newest workspace snapshot.
    /// </summary>
    public async Task<Result<WorkspaceRestoreResult>> RestoreLatestAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await snapshotStore.LoadLatestAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.IsFailure
            ? Result<WorkspaceRestoreResult>.Failure(snapshot.ErrorCode, snapshot.ErrorMessage)
            : Restore(snapshot.Value);
    }

    /// <summary>
    /// Restores a workspace snapshot by id.
    /// </summary>
    public async Task<Result<WorkspaceRestoreResult>> RestoreAsync(
        string snapshotId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await snapshotStore.LoadAsync(snapshotId, cancellationToken).ConfigureAwait(false);
        return snapshot.IsFailure
            ? Result<WorkspaceRestoreResult>.Failure(snapshot.ErrorCode, snapshot.ErrorMessage)
            : Restore(snapshot.Value);
    }

    private Result<WorkspaceRestoreResult> Restore(WorkspaceSnapshot snapshot)
    {
        var monitors = monitorManager.GetMonitors();
        if (monitors.IsFailure)
        {
            return Result<WorkspaceRestoreResult>.Failure(monitors.ErrorCode, monitors.ErrorMessage);
        }

        var windows = windowEnumerator.GetWindows();
        if (windows.IsFailure)
        {
            return Result<WorkspaceRestoreResult>.Failure(windows.ErrorCode, windows.ErrorMessage);
        }

        var matcher = new WorkspaceWindowMatcher();
        var restored = 0;
        var missing = 0;
        var failed = 0;

        foreach (var windowSnapshot in snapshot.Windows)
        {
            var window = matcher.FindMatch(windowSnapshot, windows.Value);
            if (window is null)
            {
                missing++;
                continue;
            }

            var monitor = FindMonitor(monitors.Value, windowSnapshot.MonitorDeviceName);
            if (monitor is null)
            {
                failed++;
                continue;
            }

            var targetBounds = WorkspaceGeometryMapper.ToAbsolute(windowSnapshot.RelativeRect, monitor.WorkArea);
            if (targetBounds.Width <= 0 || targetBounds.Height <= 0)
            {
                failed++;
                continue;
            }

            if (window.IsMaximized)
            {
                var restore = windowManager.RestoreWindow(window.Handle);
                if (restore.IsFailure)
                {
                    failed++;
                    continue;
                }
            }

            var move = windowManager.MoveWindow(window.Handle, targetBounds);
            if (move.IsFailure)
            {
                failed++;
                continue;
            }

            restored++;
        }

        return Result<WorkspaceRestoreResult>.Success(new WorkspaceRestoreResult(restored, missing, failed));
    }

    private static MonitorInfo? FindMonitor(IReadOnlyList<MonitorInfo> monitors, string deviceName)
    {
        return monitors.FirstOrDefault(monitor => string.Equals(monitor.DeviceName, deviceName, StringComparison.Ordinal)) ??
            monitors.FirstOrDefault(monitor => monitor.IsPrimary) ??
            monitors.FirstOrDefault();
    }

    private static WorkspaceWindowState GetWindowState(WindowInfo window)
    {
        if (window.IsMinimized)
        {
            return WorkspaceWindowState.Minimized;
        }

        return window.IsMaximized
            ? WorkspaceWindowState.Maximized
            : WorkspaceWindowState.Normal;
    }

    private static string CreateSnapshotId(DateTimeOffset createdAt)
    {
        return createdAt.UtcDateTime.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
    }

    private static string CreateSnapshotName(DateTimeOffset createdAt)
    {
        return $"Workspace {createdAt.LocalDateTime:yyyy-MM-dd HH:mm:ss}";
    }
}
