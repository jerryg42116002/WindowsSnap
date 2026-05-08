# IMPLEMENT_PLAN.md

> 本文件由 plan-agent 生成，供 implement-agent 执行。
> implement-agent 应优先读取本文件，再开始修改代码。

## 1. 任务摘要

- 任务名称：Workspace Snapshot MVP
- 任务类型：新功能 + 本地存储 + 单元测试
- 用户目标：用户可以手动保存当前所有可管理窗口的位置，并在之后手动恢复最近或指定 workspace snapshot。
- 最小可交付结果：
  - 定义 workspace snapshot 相关 Core 模型。
  - 实现可测试的相对坐标/绝对坐标转换和窗口匹配规则。
  - 实现本地 JSON 保存/读取 workspace snapshot。
  - Win32 层封装窗口枚举能力，不让 UI 直接调用 Win32。
  - 新增 WorkspaceSnapshotService，支持保存当前可管理窗口 snapshot、恢复最近 snapshot、恢复指定 snapshot。
  - MVP 暴露最小用户入口，建议通过托盘菜单新增“保存工作区快照”“恢复最近工作区快照”，不做复杂 UI 编辑器。
  - 默认不保存完整窗口标题、浏览器 URL、用户文件路径、命令行参数。

## 2. 当前假设

- 保存路径为 `%APPDATA%/WindowSnapper/workspaces/*.json`。
- `StoragePaths` 需要新增 `WorkspacesDirectoryPath` 字段；测试里的 `TemporaryStorage.CreatePaths()` 同步更新。
- Snapshot 文件名使用 snapshot `id`，建议 `yyyyMMdd-HHmmss` 或 GUID-safe id；不要使用用户输入的任意文件名。
- MVP 的 `name` 可以自动生成，例如 `Workspace 2026-05-07 21:30:00`，无需 UI 输入。
- `createdAt` 使用 UTC 时间，格式由 `System.Text.Json` 默认处理。
- `relativeRect` 表示窗口相对其所在显示器 `WorkArea` 的比例坐标，建议使用 double：`x/y/width/height`。
- 恢复时根据当前显示器 `DeviceName` 匹配原显示器；找不到时可回退到 primary 或当前显示器列表第一个，并记录/返回部分失败信息。
- `windowState` MVP 只需表达 `normal` / `maximized` / `minimized`。保存时可跳过 minimized 窗口，或保存但恢复时不主动最小化；建议保存可管理窗口时自然排除 minimized，因为 `WindowFilter` 已默认不管理最小化窗口。
- 窗口匹配规则 MVP 使用 `processName + className`，必要时按顺序匹配未使用的候选窗口；不使用标题、URL、路径、命令行参数。
- 如果同一进程和 class 有多个窗口，MVP 可采用稳定但有限的顺序匹配；不承诺完美恢复每一个同类窗口。
- 不自动启动应用、不云同步、不新增遥测、不申请管理员权限、不管理系统窗口/UAC/全屏游戏。
- 当前工作区内已有上一轮 Repeat Hotkey Cycle 未验证改动；implement-agent 不应回退或重写这些改动，只在必要时与之兼容。

## 3. 影响范围

| 模块 | 是否影响 | 原因 |
|---|---:|---|
| Core | 是 | 新增 WorkspaceSnapshot 模型、relative rect 模型、window state、窗口枚举/存储抽象可选接口、纯转换/匹配类型。 |
| Layouts | 否 | 本功能不改变布局 schema、内置布局或 LayoutEngine。 |
| Storage | 是 | 新增 WorkspaceSnapshotStorage，扩展 StoragePaths，使用本地 JSON 原子写入。 |
| Win32 | 是 | 如需枚举窗口，必须在 Win32 新增 `EnumWindows` P/Invoke 和封装方法，不暴露给 UI。 |
| Hotkeys | 否 | MVP 不新增全局快捷键。 |
| Snap | 否 | 不改变窗口分屏/快捷键 cycle 逻辑；只可查看以复用移动流程认知。 |
| Workspaces | 是 | 建议新增 `WindowSnapper.Workspaces` 项目承载 WorkspaceSnapshotService 和纯业务逻辑，避免放入 WPF App。 |
| Tray | 是 | MVP 可通过托盘菜单增加保存/恢复入口；Tray 只发命令，不做业务逻辑。 |
| App | 是 | 组合 WorkspaceSnapshotService、Storage、Win32 实现，并处理托盘命令，显示用户友好提示。 |
| Tests | 是 | 新增 Core/Storage/Workspaces/Win32 相关单元测试。 |
| Docs | 可选 | 本轮不强制更新 README/docs，避免扩大范围；可在后续 docs 任务补充。 |
| Build/CI | 是 | 如新增 Workspaces 项目/测试项目，需要更新 `.sln` 和项目引用。 |

## 4. 允许修改范围

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

可查看但不建议修改：

```text
src/WindowSnapper.Snap/
tests/WindowSnapper.Snap.Tests/
src/WindowSnapper.Layouts/
docs/
README.md
```

## 5. 禁止修改范围

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

禁止事项：

- 不保存完整窗口标题。
- 不保存浏览器 URL。
- 不保存用户文件路径。
- 不保存命令行参数。
- 不新增网络、遥测、云同步。
- 不自动申请管理员权限。
- 不注入其他进程。
- 不强制管理 UAC 安全桌面、全屏游戏、系统窗口。
- 不把 Win32 P/Invoke 写进 App/WPF/UI 层。
- 不把 workspace 恢复逻辑写进 WPF code-behind。
- 不做复杂 UI 编辑器。
- 不回退上一轮未验证的 Repeat Hotkey Cycle 改动。

## 6. 推荐实现步骤

1. Core：定义 workspace snapshot 模型和纯逻辑
   - 修改位置：`src/WindowSnapper.Core/Workspaces/`
   - 建议新增：
     - `WorkspaceSnapshot`
     - `WorkspaceWindowSnapshot`
     - `WorkspaceWindowState`
     - `RelativeRect`
     - `WorkspaceWindowMatchKey`
     - `WorkspaceGeometryMapper`
     - `WorkspaceWindowMatcher`
   - 预期结果：
     - Snapshot 能表达 `version/id/name/createdAt/windows`。
     - Window snapshot 能表达 `processName/className/monitorDeviceName/relativeRect/windowState`。
     - 不包含 title、URL、路径、命令行。
     - relative/absolute 转换可独立测试。

2. Core：定义必要抽象接口
   - 修改位置：`src/WindowSnapper.Core/Windows/` 和/或 `src/WindowSnapper.Core/Workspaces/`
   - 建议新增或扩展：
     - `IWindowEnumerator`：返回当前可管理窗口 `IReadOnlyList<WindowInfo>`。
     - `IWorkspaceSnapshotStore` 或等价接口：保存、读取指定、读取最近、列出 snapshots。
   - 预期结果：
     - Workspaces 服务不直接依赖 Storage 具体实现。
     - App 可注入 Win32 枚举器和 Storage 存储。

3. Storage：扩展路径并实现 JSON 存储
   - 修改位置：`src/WindowSnapper.Storage/`
   - 建议：
     - `StoragePaths` 新增 `WorkspacesDirectoryPath`。
     - 新增 `WorkspaceSnapshotStorage` 实现保存/读取/读取最近/列出。
     - 使用现有 `JsonStorageOptions` 和 `AtomicJsonFile.WriteAsync`。
   - 预期结果：
     - 自动创建 `%APPDATA%/WindowSnapper/workspaces`。
     - 文件损坏时单个 snapshot 读取失败应返回明确错误，不影响读取其他 snapshot 列表。
     - 读取失败错误只暴露文件名，不暴露过多本地路径。

4. Win32：封装窗口枚举
   - 修改位置：`src/WindowSnapper.Win32/`
   - 建议：
     - 在 `NativeMethods` 新增 `EnumWindows` P/Invoke 和 delegate。
     - 在 `Win32WindowManager` 或新增 `Win32WindowEnumerator` 实现 `IWindowEnumerator`。
     - 枚举时复用 `GetWindowInfo`、`WindowFilter.IsWindowManageable` 和全屏检测规则。
   - 预期结果：
     - UI 层不会直接调用 `EnumWindows`。
     - 不可见、最小化、无尺寸、系统窗口、本程序窗口、Overlay、疑似全屏窗口默认跳过。

5. Workspaces：实现 WorkspaceSnapshotService
   - 修改位置：建议新增 `src/WindowSnapper.Workspaces/`
   - 依赖方向：
     - `WindowSnapper.Workspaces -> WindowSnapper.Core`
     - 不依赖 WPF、Win32、Storage。
   - 服务职责：
     - `SaveCurrentAsync(name?)`
     - `RestoreLatestAsync()`
     - `RestoreAsync(snapshotId)`
   - 保存流程：
     - 获取 monitors。
     - 枚举可管理窗口。
     - 找到每个窗口所在 monitor。
     - 将 `WindowInfo.Bounds` 转为相对 `MonitorInfo.WorkArea` 的 `RelativeRect`。
     - 保存 processName/className/monitorDeviceName/relativeRect/windowState。
   - 恢复流程：
     - 读取 snapshot。
     - 获取 monitors 和当前可管理窗口。
     - 用 `processName + className` 匹配窗口。
     - 将 `RelativeRect` 转回当前 monitor workArea 上的 `RectInt`。
     - 最大化窗口先 restore，再 move；需要最大化时 move 后再 maximize 的能力若当前 Core/Win32 不支持，MVP 可先恢复 normal 位置并记录 `windowState=maximized` 为未来扩展。
   - 预期结果：
     - 纯服务可用 fake enumerator/window manager/store/monitor manager 单测。

6. Tray/App：增加最小手动入口
   - 修改位置：
     - `src/WindowSnapper.Tray/TrayMenuCommand.cs`
     - `src/WindowSnapper.Tray/NotifyIconTrayIcon.cs`
     - `src/WindowSnapper.App/Composition/AppServices.cs`
     - `src/WindowSnapper.App/Controllers/AppController.cs`
   - 建议托盘菜单新增：
     - `保存工作区快照`
     - `恢复最近工作区快照`
   - App 只调用 `WorkspaceSnapshotService`，显示成功/失败提示。
   - 预期结果：
     - 没有复杂 UI 编辑器。
     - UI 层不直接 Win32。

7. Tests：补充单元测试
   - 修改位置：
     - `tests/WindowSnapper.Core.Tests/`
     - `tests/WindowSnapper.Storage.Tests/`
     - `tests/WindowSnapper.Workspaces.Tests/` 如新增项目
     - `tests/WindowSnapper.Win32.Tests/` 仅测试纯过滤/映射，不依赖真实桌面。
   - 预期结果：
     - 覆盖 relativeRect/absoluteRect 转换。
     - 覆盖 Storage 保存、读取指定、读取最近、损坏文件错误。
     - 覆盖窗口匹配规则：同 process/class 匹配、不同 process/class 不匹配、重复窗口按未使用候选匹配。
     - 覆盖服务保存时不保存标题。

8. 更新测试交接文件
   - 修改位置：`.agent_handoff/TEST_PLAN.md`
   - 预期结果：
     - 写清新增项目、推荐测试命令、人工 Windows 验证项和已知限制。

## 7. 验收标准

- [ ] Core snapshot 模型包含 `version/id/name/createdAt/windows`。
- [ ] 每个 window snapshot 包含 `processName/className/monitorDeviceName/relativeRect/windowState`。
- [ ] 默认不会保存完整窗口标题、浏览器 URL、用户文件路径、命令行参数。
- [ ] `RelativeRect -> RectInt -> RelativeRect` 在 1920x1080 WorkArea 下误差在合理范围内。
- [ ] 负坐标 WorkArea 下 absolute/relative 转换保持正确 X/Y。
- [ ] 竖屏 WorkArea 下 absolute/relative 转换保持正确宽高比例。
- [ ] `WorkspaceSnapshotStorage.SaveAsync` 将 JSON 写入 workspaces 目录。
- [ ] `WorkspaceSnapshotStorage.LoadAsync(id)` 可以读取指定 snapshot。
- [ ] `WorkspaceSnapshotStorage.LoadLatestAsync()` 返回 `createdAt` 最新的合法 snapshot。
- [ ] 损坏 snapshot 文件返回明确错误或 issue，不导致读取其他 snapshot 崩溃。
- [ ] Win32 窗口枚举封装只存在于 `WindowSnapper.Win32`，UI 层不直接调用 `NativeMethods`。
- [ ] WorkspaceSnapshotService 保存时只枚举可管理窗口。
- [ ] WorkspaceSnapshotService 恢复时使用 `processName + className` 匹配当前窗口。
- [ ] 匹配不到窗口时不崩溃，并返回部分失败或明确结果。
- [ ] 恢复时基于目标 monitor 的 `WorkArea` 计算绝对位置，不覆盖任务栏。
- [ ] 托盘菜单可以手动触发保存和恢复最近 snapshot。
- [ ] 不自动启动应用，不云同步，不申请管理员权限。

## 8. 必跑测试

```bash
dotnet test tests/WindowSnapper.Core.Tests/WindowSnapper.Core.Tests.csproj
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
dotnet test tests/WindowSnapper.Workspaces.Tests/WindowSnapper.Workspaces.Tests.csproj
```

如果 implement-agent 没有新增 `WindowSnapper.Workspaces.Tests` 项目，则用实际新增服务所在模块的测试项目替代第三条命令。

## 9. 可选测试

```bash
dotnet test tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj
dotnet build WindowSnapper.sln
dotnet test WindowSnapper.sln
```

## 10. 不建议自动运行的测试或命令

```bash
dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release
```

原因：

- 本任务不需要发布产物。
- 真实桌面窗口枚举/恢复不适合在普通单元测试中自动执行。
- implement 阶段如使用 `$implement-agent`，不得运行 `dotnet restore/build/test/publish`，只更新 `.agent_handoff/TEST_PLAN.md`。

## 11. 需要人工验证的部分

- [ ] 在 Windows 桌面环境中打开多个普通应用窗口。
- [ ] 从托盘点击“保存工作区快照”，确认 `%APPDATA%/WindowSnapper/workspaces/*.json` 生成。
- [ ] 检查 JSON 不包含完整窗口标题、URL、文件路径、命令行参数。
- [ ] 手动移动窗口后点击“恢复最近工作区快照”，确认窗口回到相对位置。
- [ ] 多显示器环境下保存/恢复，确认使用 `WorkArea`，不覆盖任务栏。
- [ ] 副屏在主屏左侧导致负坐标时保存/恢复。
- [ ] 管理员权限窗口、系统窗口、任务栏、桌面、全屏游戏不应被保存或恢复。
- [ ] 同进程同 class 多窗口场景下确认 MVP 的匹配限制可接受。

## 12. 风险与注意事项

- 当前 Core `IWindowManager` 没有枚举窗口能力，需新增单独 `IWindowEnumerator`，避免把枚举语义塞进 UI。
- 当前 Win32 `NativeMethods` 没有 `EnumWindows`，如实现真实保存当前所有窗口，需要在 Win32 层新增 P/Invoke；这是本任务允许的 Win32 修改，但必须集中在 Win32 项目。
- `WindowInfo` 当前包含 `Title`，但 workspace snapshot 绝不能持久化该字段。
- 仅用 `processName + className` 匹配无法完美区分多个同类窗口；MVP 应在文档/交接中说明限制，不要偷偷保存隐私字段来提高匹配率。
- 恢复 maximized 状态可能需要 Core/Win32 增加 maximize 能力；MVP 可先保存状态，恢复时以 normal rect 移动为主，最大化恢复作为后续增强，除非实现范围很小且不破坏架构。
- StoragePaths 增加字段会影响现有测试 helper 和所有构造调用，implement-agent 需要同步更新。
- 上一轮 Repeat Hotkey Cycle 改动尚未测试，implement-agent 不应回退其文件；如碰到冲突，只做最小兼容。
- 不要把 workspace snapshot 与自定义 layout JSON 混用；路径应独立为 `workspaces`。

## 13. 给 implement-agent 的执行要求

- 先读取本文件。
- 只做本计划允许范围内的最小修改。
- 不要扩大任务范围。
- 不要保存窗口标题、URL、文件路径、命令行参数。
- 不要新增网络、遥测、云同步。
- 不要自动申请管理员权限。
- 不要修改 WPF XAML 或 code-behind；App 入口只做服务调用转发。
- Win32 P/Invoke 只能写在 `WindowSnapper.Win32/NativeMethods.cs`。
- 优先测试 Core 纯转换、Storage 读写、Workspace 服务匹配规则。
- 如果使用 implement-agent 工作流，完成后不要运行 dotnet 验证命令，只更新 `.agent_handoff/TEST_PLAN.md`。
