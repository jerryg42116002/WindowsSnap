# BUG_PATTERNS.md

> 本文件记录已经踩过的坑和防复发规则。
> implement-agent / test-agent / review-agent / context-pack-agent 应在开始任务前读取本文件。

## Bug 模式记录

### 2026-05-07 - StoragePaths 构造参数新增后测试 helper 容易遗漏同步

**触发场景：**

- 为 Storage 新增路径字段，例如 `WorkspacesDirectoryPath`。

**错误表现：**

- 生产 `StoragePaths.CreateDefault()` 已更新，但测试中的 `TemporaryStorage.CreatePaths()` 或其他构造点仍使用旧参数列表，可能导致编译失败。

**根因：**

- `StoragePaths` 是 positional record，新增构造参数会影响所有显式构造点。

**修复方式：**

- 同步更新所有 `new StoragePaths(...)` 调用点，尤其是 Storage 测试 helper。

**防复发规则：**

- 修改 positional record 构造参数后，立即使用 `rg "StoragePaths\\(" src tests -n` 检查所有调用点。

**相关测试：**

- `tests/WindowSnapper.Storage.Tests/SettingsStorageTests.cs`
- `tests/WindowSnapper.Storage.Tests/LayoutStorageTests.cs`
- `tests/WindowSnapper.Storage.Tests/WorkspaceSnapshotStorageTests.cs`

### 2026-05-07 - implement 阶段容易误运行 dotnet 验证命令

**触发场景：**

- 用户使用 `$implement-agent` 要求实现或文档更新。

**错误表现：**

- agent 在实现阶段运行 `dotnet restore`、`dotnet build`、`dotnet test` 或 `dotnet publish`，破坏 implement/test 分离工作流。

**根因：**

- `AGENTS.md` 的一般提交前检查与 implement-agent 的阶段规则容易混淆。

**修复方式：**

- implement-agent 只做静态阅读、搜索检查、架构边界检查和 `git diff --check` 等轻量检查。
- 在 `.agent_handoff/TEST_PLAN.md` 写明推荐给 test-agent 的命令。

**防复发规则：**

- 看到 `$implement-agent` 时，先读取 `.agents/skills/implement-agent/SKILL.md`，明确“不运行 dotnet 验证命令”。

**相关测试：**

- 不适用；这是工作流规则。

### 2026-05-07 - 长期记忆和当前上下文包职责容易混用

**触发场景：**

- 阶段结束后想降低新会话 token 消耗，同时又想保存长期决策。

**错误表现：**

- 把完整当前任务上下文写进 `.agent_memory/`，导致长期记忆膨胀。
- 或把稳定架构决策只写进 `.agent_handoff/CONTEXT_PACK.md`，新任务后容易丢失。

**根因：**

- `.agent_handoff/` 和 `.agent_memory/` 生命周期不同。

**修复方式：**

- 当前任务交接写入 `.agent_handoff/`。
- 长期稳定信息写入 `.agent_memory/`。

**防复发规则：**

- 如果信息只对下一步任务有用，用 context-pack-agent。
- 如果信息跨任务仍有效，用 memory-agent。

**相关测试：**

- 不适用；这是文档工作流规则。
