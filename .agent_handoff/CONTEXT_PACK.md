# CONTEXT_PACK.md

> 本文件由 context-pack-agent 生成。
> 目的：为下一轮 Codex/agent 对话提供低 token、高密度上下文。
> 使用方式：新对话开始后，要求对应 agent 先读取本文件。

## 1. 当前任务一句话摘要

为 WindowSnapper 实现 Workspace Snapshot MVP：手动保存当前所有可管理窗口的位置到本地 JSON，并支持之后手动恢复最近或指定 snapshot。

## 2. 当前阶段

```text
plan -> implement
```

## 3. 下一步建议调用的 agent

```text
$implement-agent
```

原因：

- `.agent_handoff/IMPLEMENT_PLAN.md` 已是 Workspace Snapshot MVP 计划。
- 下一步应按计划做最小实现，并在结束时更新 `.agent_handoff/TEST_PLAN.md`。
- implement 阶段不要运行 `dotnet restore/build/test/publish`。

## 4. 必须遵守的项目规则摘要

1. Core 不得依赖 WPF、Win32、Storage、文件系统或 UI。
2. Storage 负责本地 JSON 读写；不要把文件系统逻辑放进 Core 或 UI。
3. Win32 P/Invoke 必须集中在 `WindowSnapper.Win32/NativeMethods.cs`，UI 层不能直接调用 `NativeMethods`。
4. UI/code-behind 只做事件转发；业务逻辑应在服务层。
5. 不新增网络、遥测、账号、云同步。
6. 不自动申请管理员权限，不注入其他进程。
7. 不管理 UAC 安全桌面、全屏游戏、系统窗口、任务栏、桌面。
8. 默认不记录或持久化完整窗口标题、浏览器 URL、用户文件路径、命令行参数。
9. 涉及纯逻辑变更必须加单元测试。
10. 当前工作区已有上一轮 Repeat Hotkey Cycle 未验证改动；不要回退或重写，必要时最小兼容。

## 5. 当前任务目标

- 保存当前所有可管理窗口的 workspace snapshot。
- 恢复最近或指定 workspace snapshot。
- 使用本地 JSON：`%APPDATA%/WindowSnapper/workspaces/*.json`。
- Snapshot 保存字段：
  - `version`
  - `id`
  - `name`
  - `createdAt`
  - `windows`
  - `processName`
  - `className`
  - `monitorDeviceName`
  - `relativeRect`
  - `windowState`
- Snapshot 禁止默认保存：
  - 完整窗口标题
  - 浏览器 URL
  - 用户文件路径
  - 命令行参数
- MVP 用户入口建议：托盘菜单新增“保存工作区快照”和“恢复最近工作区快照”。
- 不做复杂 UI 编辑器，不新增全局快捷键。

## 6. 允许修改范围

```text
src/WindowSnapper.Core/
src/WindowSnapper.Storage/
src/WindowSnapper.Win32/
src/WindowSnapper.Tray/
src/WindowSnapper.App/Composition/
src/WindowSnapper.App/Controllers/
src/WindowSnapper.Workspaces/                 # 如新增项目
tests/WindowSnapper.Core.Tests/
tests/WindowSnapper.Storage.Tests/
tests/WindowSnapper.Win32.Tests/
tests/WindowSnapper.Workspaces.Tests/         # 如新增测试项目
WindowSnapper.sln                             # 仅当新增项目/测试项目时
.agent_handoff/TEST_PLAN.md
```

## 7. 禁止修改范围

```text
src/WindowSnapper.App/*.xaml
src/WindowSnapper.App/*Window.xaml.cs
src/WindowSnapper.App/Overlay/
src/WindowSnapper.Hotkeys/
src/WindowSnapper.Layouts/
tests/WindowSnapper.Layouts.Tests/
tests/WindowSnapper.Hotkeys.Tests/
README.md
docs/
Directory.Build.props
```

另外：

- 不要保存 title/URL/path/command-line。
- 不要把 workspace 恢复逻辑写进 WPF code-behind。
- 不要把 workspace JSON 和 layouts JSON 混用；workspace 路径必须独立。

## 8. 关键相关文件

| 文件 | 作用 | 当前注意事项 |
|---|---|---|
| `.agent_handoff/IMPLEMENT_PLAN.md` | Workspace Snapshot MVP 实现计划 | 当前有效计划，implement-agent 必读。 |
| `.agent_handoff/TEST_PLAN.md` | 上一轮 Repeat Hotkey Cycle 测试交接 | 已过时于当前任务，但说明工作区有未验证改动。 |
| `src/WindowSnapper.Core/Windows/IWindowManager.cs` | 当前窗口操作接口 | 只有 active/get info/manageable/restore/move；没有枚举窗口能力。 |
| `src/WindowSnapper.Core/Windows/WindowInfo.cs` | 窗口模型 | 包含 `Title`，但 WorkspaceSnapshot 禁止持久化该字段。 |
| `src/WindowSnapper.Core/Monitors/MonitorInfo.cs` | 显示器模型 | 恢复必须基于 `WorkArea`，不要用 `Bounds` 覆盖任务栏。 |
| `src/WindowSnapper.Storage/StoragePaths.cs` | 配置/layout/log 路径 | 需新增 `WorkspacesDirectoryPath`，并同步测试 helper。 |
| `src/WindowSnapper.Storage/AtomicJsonFile.cs` | 原子写 JSON | Workspace storage 应复用。 |
| `src/WindowSnapper.Storage/LayoutStorage.cs` | 多 JSON 文件读取模式 | 可参考错误处理和只暴露文件名的模式。 |
| `src/WindowSnapper.Win32/NativeMethods.cs` | P/Invoke 集中点 | 当前有 `EnumDisplayMonitors`，没有 `EnumWindows`；如需枚举窗口只能在这里加。 |
| `src/WindowSnapper.Win32/Win32WindowManager.cs` | Win32 窗口信息/移动 | 可复用 `GetWindowInfo`、`MoveWindow`、`RestoreWindow`。 |
| `src/WindowSnapper.Win32/WindowFilter.cs` | 可管理窗口规则 | 保存/恢复应复用过滤，不管理系统/自身/Overlay/无尺寸/不可见/最小化窗口。 |
| `src/WindowSnapper.Tray/NotifyIconTrayIcon.cs` | 托盘菜单 | 可新增保存/恢复最近菜单项；Tray 只发命令。 |
| `src/WindowSnapper.Tray/TrayMenuCommand.cs` | 托盘命令 enum | 可新增 save/restore workspace 命令。 |
| `src/WindowSnapper.App/Composition/AppServices.cs` | App 服务组合 | 注入 WorkspaceSnapshotService、Win32 枚举器、Storage store。 |
| `src/WindowSnapper.App/Controllers/AppController.cs` | App 事件转发 | 处理托盘命令，调用 Workspace 服务，显示友好提示。 |
| `tests/WindowSnapper.Storage.Tests/TemporaryStorage.cs` | Storage 测试路径 helper | `StoragePaths` 新字段会要求同步更新。 |

## 9. 关键接口 / 类 / 函数摘要

| 名称 | 位置 | 摘要 |
|---|---|---|
| `IWindowManager` | Core/Windows | 移动/还原/查询窗口；不负责枚举。建议新增 `IWindowEnumerator`。 |
| `WindowInfo` | Core/Windows | 有 `Title`，但 workspace 持久化只能用 process/class/bounds/state 等非敏感字段。 |
| `MonitorInfo.WorkArea` | Core/Monitors | relative/absolute 转换必须基于 WorkArea。 |
| `RectInt` | Core/Geometry | 绝对屏幕像素矩形，支持负坐标。 |
| `StoragePaths.CreateDefault` | Storage | 当前返回 config/layout/log，需增加 workspaces 路径。 |
| `AtomicJsonFile.WriteAsync` | Storage | 原子写：先 tmp 后 move。Workspace storage 应复用。 |
| `WindowFilter.IsWindowManageable` | Win32 | 判断可管理窗口；枚举保存时应复用。 |
| `NativeMethods` | Win32 | 唯一允许新增 `EnumWindows` P/Invoke 的位置。 |
| `WorkspaceGeometryMapper` | 待新增 Core/Workspaces | relativeRect 和 absolute RectInt 双向转换，核心测试对象。 |
| `WorkspaceWindowMatcher` | 待新增 Core/Workspaces | 用 `processName + className` 匹配当前窗口；不使用 title。 |
| `WorkspaceSnapshotStorage` | 待新增 Storage | 保存/读取指定/读取最近/列出 snapshot JSON。 |
| `WorkspaceSnapshotService` | 待新增 Workspaces | 保存当前、恢复最近、恢复指定；不依赖 WPF/Win32/Storage 具体类型。 |

## 10. 已知坑点

- `WindowInfo.Title` 很容易被误用；本任务禁止持久化标题。
- 只用 `processName + className` 无法完美区分多个同类窗口；MVP 接受有限匹配，不要用隐私字段补强。
- 多显示器坐标不一定从 `(0,0)` 开始；副屏可能负坐标。
- 任务栏位置不固定；转换必须基于 `MonitorInfo.WorkArea`。
- `StoragePaths` 新增字段会影响所有构造点和测试 helper。
- Win32 真实窗口枚举不能在单元测试中依赖桌面状态。
- 当前 Repeat Hotkey Cycle 改动尚未 test-agent 验证；不要回退相关文件。
- 恢复 maximized 状态可能需要新增 maximize 能力；MVP 可先保存状态，恢复 normal 位置为主。

## 11. 必须保留的行为

- 现有 Snap/Layouts/Hotkeys 行为不应被 Workspace Snapshot 改动破坏。
- Tray 菜单现有打开主窗口、设置、暂停/恢复快捷键、布局、退出行为保留。
- Win32 错误必须通过 Result 返回，不静默吞掉。
- 配置和布局路径保持不变；新增 workspaces 路径不能影响 existing config/layouts。
- 非法/损坏单个 workspace JSON 不应导致应用崩溃。
- UI 显示用户友好错误，不暴露原始 Win32 细节或本地完整路径。

## 12. 推荐命令

供 test-agent 使用；implement-agent 只写入 TEST_PLAN，不运行：

```bash
dotnet test tests/WindowSnapper.Core.Tests/WindowSnapper.Core.Tests.csproj
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
dotnet test tests/WindowSnapper.Workspaces.Tests/WindowSnapper.Workspaces.Tests.csproj
dotnet test tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj
dotnet build WindowSnapper.sln
dotnet test WindowSnapper.sln
```

如果没有新增 `WindowSnapper.Workspaces.Tests`，用实际服务测试项目替代。

## 13. 不建议自动运行的命令

```bash
dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release
```

原因：

- 本任务不需要发布产物。
- 真实桌面保存/恢复需人工 Windows 验证，不适合普通自动单元测试。
- implement-agent 阶段不得运行 `dotnet restore/build/test/publish`。

## 14. 最近交接文件状态

| 文件 | 状态 | 说明 |
|---|---|---|
| `TASK.md` | outdated/empty | 仍是模板，未记录当前 Workspace 任务。 |
| `IMPLEMENT_PLAN.md` | current | 当前 Workspace Snapshot MVP 计划，下一步 implement-agent 应优先读取。 |
| `TEST_PLAN.md` | outdated for current task | 内容是上一轮 Repeat Hotkey Cycle 的测试交接；可作为工作区未验证状态参考。 |
| `REVIEW_REPORT.md` | unknown/not used | 本任务未读取到有效 review 依赖。 |

## 15. 给下一位 agent 的简短指令

```text
先读取 .agent_handoff/CONTEXT_PACK.md 和 .agent_handoff/IMPLEMENT_PLAN.md。
实现 Workspace Snapshot MVP，只做允许范围内的最小修改。
重点遵守隐私限制：不要保存窗口标题、URL、文件路径、命令行参数。
Win32 枚举只能封装在 WindowSnapper.Win32；UI 只调用服务。
完成后更新 .agent_handoff/TEST_PLAN.md；implement 阶段不要运行 dotnet 验证命令。
```
