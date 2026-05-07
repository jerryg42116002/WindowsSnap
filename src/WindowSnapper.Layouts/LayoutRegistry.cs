namespace WindowSnapper.Layouts;

/// <summary>
/// Combines built-in layouts with user-defined layouts without file-system access.
/// </summary>
public sealed class LayoutRegistry
{
    private readonly Dictionary<string, LayoutDefinition> layoutsById;

    private LayoutRegistry(
        IReadOnlyList<LayoutDefinition> layouts,
        IReadOnlyList<LayoutRegistryIssue> issues)
    {
        Layouts = layouts;
        Issues = issues;
        layoutsById = layouts.ToDictionary(layout => layout.Id, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the registered layouts, with built-ins first.
    /// </summary>
    public IReadOnlyList<LayoutDefinition> Layouts { get; }

    /// <summary>
    /// Gets non-fatal issues found while registering user layouts.
    /// </summary>
    public IReadOnlyList<LayoutRegistryIssue> Issues { get; }

    /// <summary>
    /// Creates a registry from built-in layouts and user layouts.
    /// </summary>
    public static LayoutRegistry Create(IEnumerable<LayoutDefinition> userLayouts)
    {
        ArgumentNullException.ThrowIfNull(userLayouts);

        return Create(userLayouts.Select(layout => new LayoutRegistrationCandidate(layout)));
    }

    /// <summary>
    /// Creates a registry from built-in layouts and user layout candidates.
    /// </summary>
    public static LayoutRegistry Create(IEnumerable<LayoutRegistrationCandidate> userLayoutCandidates)
    {
        ArgumentNullException.ThrowIfNull(userLayoutCandidates);

        var layouts = new List<LayoutDefinition>(BuiltinLayouts.All);
        var issues = new List<LayoutRegistryIssue>();
        var builtInIds = new HashSet<string>(BuiltinLayouts.All.Select(layout => layout.Id), StringComparer.Ordinal);
        var userIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in userLayoutCandidates)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ArgumentNullException.ThrowIfNull(candidate.Layout);

            var layout = candidate.Layout;
            if (builtInIds.Contains(layout.Id))
            {
                issues.Add(new LayoutRegistryIssue(
                    LayoutRegistryIssueCode.BuiltinConflict,
                    layout.Id,
                    candidate.SourceName,
                    $"User layout '{layout.Id}' conflicts with a built-in layout and was skipped."));
                continue;
            }

            if (!userIds.Add(layout.Id))
            {
                issues.Add(new LayoutRegistryIssue(
                    LayoutRegistryIssueCode.DuplicateUserLayout,
                    layout.Id,
                    candidate.SourceName,
                    $"Duplicate user layout '{layout.Id}' was skipped."));
                continue;
            }

            layouts.Add(layout);
        }

        return new LayoutRegistry(layouts, issues);
    }

    /// <summary>
    /// Finds a registered layout by id.
    /// </summary>
    public LayoutDefinition? FindById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return layoutsById.GetValueOrDefault(id);
    }
}
