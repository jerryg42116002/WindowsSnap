using WindowSnapper.Core.Results;

namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Persists workspace snapshots without exposing storage details to services.
/// </summary>
public interface IWorkspaceSnapshotStore
{
    /// <summary>
    /// Saves a workspace snapshot.
    /// </summary>
    Task<Result> SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a workspace snapshot by id.
    /// </summary>
    Task<Result<WorkspaceSnapshot>> LoadAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the newest available workspace snapshot.
    /// </summary>
    Task<Result<WorkspaceSnapshot>> LoadLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists valid workspace snapshots.
    /// </summary>
    Task<Result<IReadOnlyList<WorkspaceSnapshot>>> ListAsync(CancellationToken cancellationToken = default);
}
