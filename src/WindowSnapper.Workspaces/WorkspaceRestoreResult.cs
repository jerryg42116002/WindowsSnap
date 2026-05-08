namespace WindowSnapper.Workspaces;

/// <summary>
/// Summarizes a workspace restore operation.
/// </summary>
public sealed record WorkspaceRestoreResult(
    int RestoredCount,
    int MissingWindowCount,
    int FailedCount);
