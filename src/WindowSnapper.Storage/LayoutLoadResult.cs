namespace WindowSnapper.Storage;

/// <summary>
/// Contains successfully loaded layouts and non-fatal load issues.
/// </summary>
/// <param name="Layouts">The valid user layouts.</param>
/// <param name="Issues">The invalid or unreadable layout files.</param>
public sealed record LayoutLoadResult(
    IReadOnlyList<LoadedLayoutDefinition> Layouts,
    IReadOnlyList<LayoutLoadIssue> Issues);
