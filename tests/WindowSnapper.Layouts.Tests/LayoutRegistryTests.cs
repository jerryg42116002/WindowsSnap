namespace WindowSnapper.Layouts.Tests;

public sealed class LayoutRegistryTests
{
    [Fact]
    public void EmptyUserLayoutsStillExposeBuiltInLayouts()
    {
        var registry = LayoutRegistry.Create(Array.Empty<LayoutDefinition>());

        Assert.NotEmpty(registry.Layouts);
        Assert.NotNull(registry.FindById(BuiltinLayouts.LeftHalfId));
        Assert.Empty(registry.Issues);
    }

    [Fact]
    public void UserLayoutIsAppendedAfterBuiltIns()
    {
        var userLayout = CreateLayout("dev-layout");

        var registry = LayoutRegistry.Create([new LayoutRegistrationCandidate(userLayout, "dev-layout.json")]);

        Assert.Contains(registry.Layouts, layout => layout.Id == BuiltinLayouts.LeftHalfId);
        Assert.Same(userLayout, registry.FindById("dev-layout"));
        Assert.Equal(userLayout, registry.Layouts.Last());
    }

    [Fact]
    public void UserLayoutCannotOverrideBuiltInLayout()
    {
        var conflictingLayout = CreateLayout(BuiltinLayouts.LeftHalfId);

        var registry = LayoutRegistry.Create([new LayoutRegistrationCandidate(conflictingLayout, "left-half.json")]);

        Assert.NotSame(conflictingLayout, registry.FindById(BuiltinLayouts.LeftHalfId));
        var issue = Assert.Single(registry.Issues);
        Assert.Equal(LayoutRegistryIssueCode.BuiltinConflict, issue.Code);
        Assert.Equal(BuiltinLayouts.LeftHalfId, issue.LayoutId);
        Assert.Equal("left-half.json", issue.SourceName);
    }

    [Fact]
    public void DuplicateUserLayoutKeepsFirstAndSkipsLaterDuplicate()
    {
        var first = CreateLayout("dev-layout", "First");
        var second = CreateLayout("dev-layout", "Second");

        var registry = LayoutRegistry.Create(
        [
            new LayoutRegistrationCandidate(first, "first.json"),
            new LayoutRegistrationCandidate(second, "second.json")
        ]);

        Assert.Same(first, registry.FindById("dev-layout"));
        var issue = Assert.Single(registry.Issues);
        Assert.Equal(LayoutRegistryIssueCode.DuplicateUserLayout, issue.Code);
        Assert.Equal("second.json", issue.SourceName);
    }

    private static LayoutDefinition CreateLayout(string id, string name = "Dev Layout")
    {
        return new LayoutDefinition(
            id,
            name,
            1,
            Gap: 0,
            Margin: 0,
            [new ZoneDefinition("main", "Main", 0, 0, 1, 1)]);
    }
}
