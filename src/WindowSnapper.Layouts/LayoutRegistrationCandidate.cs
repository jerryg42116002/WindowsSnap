namespace WindowSnapper.Layouts;

/// <summary>
/// Describes a user layout candidate before it is registered.
/// </summary>
/// <param name="Layout">The layout definition.</param>
/// <param name="SourceName">A safe source name, such as a file name, when available.</param>
public sealed record LayoutRegistrationCandidate(LayoutDefinition Layout, string? SourceName = null);
