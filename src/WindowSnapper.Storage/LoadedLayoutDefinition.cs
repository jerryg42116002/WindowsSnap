using WindowSnapper.Layouts;

namespace WindowSnapper.Storage;

/// <summary>
/// Describes a layout loaded from a user layout file.
/// </summary>
/// <param name="Layout">The validated layout.</param>
/// <param name="FileName">The safe file name that produced the layout.</param>
public sealed record LoadedLayoutDefinition(LayoutDefinition Layout, string FileName);
