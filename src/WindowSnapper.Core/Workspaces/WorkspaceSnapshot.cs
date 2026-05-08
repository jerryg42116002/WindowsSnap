namespace WindowSnapper.Core.Workspaces;

/// <summary>
/// Describes a saved set of window positions.
/// </summary>
public sealed record WorkspaceSnapshot(
    int Version,
    string Id,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<WorkspaceWindowSnapshot> Windows);
