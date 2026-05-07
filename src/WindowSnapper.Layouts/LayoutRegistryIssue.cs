namespace WindowSnapper.Layouts;

/// <summary>
/// Describes a non-fatal layout registration issue.
/// </summary>
/// <param name="Code">The issue category.</param>
/// <param name="LayoutId">The layout id involved.</param>
/// <param name="SourceName">A safe source name, such as a file name, when available.</param>
/// <param name="Message">A user-safe diagnostic message.</param>
public sealed record LayoutRegistryIssue(
    LayoutRegistryIssueCode Code,
    string LayoutId,
    string? SourceName,
    string Message);
