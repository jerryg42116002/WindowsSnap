# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现用户自定义布局 JSON 的加载和使用
- 实现日期：2026-05-07
- 相关需求：从 `%APPDATA%/WindowSnapper/layouts/*.json` 加载用户布局；非法布局不崩溃；内置布局和用户布局合并；用户布局不能覆盖内置布局；托盘菜单显示并可使用可用布局。
- 实现内容：
  - `LayoutStorage` 改为批量加载并返回成功布局和非致命问题列表，非法布局被跳过。
  - 新增 `LayoutRegistry`，纯逻辑合并内置布局和用户布局，处理内置 id 冲突和用户布局重复 id。
  - `WindowSnapService` 使用 `LayoutRegistry` 查找布局，不再只查内置布局。
  - App 启动时加载自定义布局，记录非法文件/冲突问题，并用安全文件名提示用户。
  - 托盘菜单新增“布局”子菜单，显示内置布局和用户布局；选择布局/区域后移动当前活动窗口。
  - 新增/更新单元测试覆盖 Storage、Layouts registry、Snap 自定义布局使用。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Storage/LayoutStorage.cs` | 修改 | 批量读取 `*.json`，非法/不可读文件跳过并记录 issue。 |
| `src/WindowSnapper.Storage/LoadedLayoutDefinition.cs` | 新增 | 记录加载成功的布局和安全文件名。 |
| `src/WindowSnapper.Storage/LayoutLoadIssue.cs` | 新增 | 记录布局文件加载失败的安全文件名、错误码、消息。 |
| `src/WindowSnapper.Storage/LayoutLoadResult.cs` | 新增 | 返回成功布局和非致命加载问题。 |
| `src/WindowSnapper.Layouts/LayoutRegistry.cs` | 新增 | 合并内置布局和用户布局，不访问文件系统。 |
| `src/WindowSnapper.Layouts/LayoutRegistrationCandidate.cs` | 新增 | 用户布局注册候选模型，携带安全来源名。 |
| `src/WindowSnapper.Layouts/LayoutRegistryIssue.cs` | 新增 | Registry 非致命问题。 |
| `src/WindowSnapper.Layouts/LayoutRegistryIssueCode.cs` | 新增 | Registry 问题类别。 |
| `src/WindowSnapper.Snap/WindowSnapService.cs` | 修改 | 通过 `LayoutRegistry` 查找布局，支持用户布局。 |
| `src/WindowSnapper.App/Controllers/AppController.cs` | 修改 | 启动加载用户布局；提示非法文件；托盘布局命令触发 snap。 |
| `src/WindowSnapper.App/Composition/AppServices.cs` | 修改 | 将 `LayoutRegistry` 注入 `WindowSnapService`。 |
| `src/WindowSnapper.Tray/NotifyIconTrayIcon.cs` | 修改 | 新增“布局”子菜单并分发 layout/zone 命令。 |
| `src/WindowSnapper.Tray/TrayMenuCommand.cs` | 修改 | 新增 `SnapLayoutZone` 命令。 |
| `src/WindowSnapper.Tray/TrayMenuCommandEventArgs.cs` | 修改 | 托盘命令事件携带 layout id / zone id。 |
| `src/WindowSnapper.Tray/TrayMenuState.cs` | 修改 | 托盘状态携带可用布局列表。 |
| `src/WindowSnapper.Tray/TrayLayoutMenuItem.cs` | 新增 | 托盘布局菜单项模型。 |
| `src/WindowSnapper.Tray/TrayZoneMenuItem.cs` | 新增 | 托盘区域菜单项模型。 |
| `tests/WindowSnapper.Storage.Tests/LayoutStorageTests.cs` | 修改 | 覆盖单个/多个合法布局、非法跳过、空目录。 |
| `tests/WindowSnapper.Layouts.Tests/LayoutRegistryTests.cs` | 新增 | 覆盖内置保留、用户布局追加、内置冲突、重复用户 id。 |
| `tests/WindowSnapper.Snap.Tests/WindowSnapServiceTests.cs` | 修改 | 覆盖通过 registry 使用自定义布局。 |

---

## 3. 涉及的类、函数、模块

- `LayoutStorage.LoadLayoutsAsync(...)`
- `LayoutStorage.LoadLayoutAsync(...)`
- `LayoutLoadResult`
- `LayoutLoadIssue`
- `LoadedLayoutDefinition`
- `LayoutRegistry.Create(...)`
- `LayoutRegistry.FindById(...)`
- `LayoutRegistryIssue`
- `WindowSnapService.SnapActiveWindow(...)`
- `AppController.LoadLayoutRegistryAsync()`
- `AppController.SnapFromTray(...)`
- `NotifyIconTrayIcon.UpdateState(...)`
- `TrayMenuState`
- `TrayLayoutMenuItem`
- `TrayZoneMenuItem`

---

## 4. 需要重点测试的功能

- [ ] `LayoutStorage`：
  - 单个合法布局可读取。
  - 多个合法布局都可读取且顺序稳定。
  - 非法布局文件被跳过，合法文件仍返回。
  - 非法布局 issue 包含安全文件名，例如 `invalid-layout.json`，不包含完整本地路径。
  - 空 `layouts` 目录返回空用户布局和空 issue。
- [ ] `LayoutRegistry`：
  - 空用户布局时仍包含全部内置布局。
  - 用户布局追加到内置布局之后。
  - 用户 layout id 与内置 id 冲突时跳过用户布局，不覆盖内置布局。
  - 重复用户 layout id 时保留第一个，跳过后续重复。
- [ ] `WindowSnapService`：
  - 通过自定义 `LayoutRegistry` 能找到并使用用户布局。
  - 仍支持内置布局。
- [ ] App/Tray：
  - 启动时加载 `%APPDATA%/WindowSnapper/layouts/*.json`。
  - 非法布局文件不导致崩溃，Trace 记录安全文件名。
  - 托盘菜单“布局”显示内置布局和用户布局。
  - 单 zone 布局点击布局项即可移动当前窗口。
  - 多 zone 布局显示 zone 子菜单，点击区域移动当前窗口。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

优先运行纯逻辑/Storage 相关测试：

```bash
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
dotnet test tests/WindowSnapper.Layouts.Tests/WindowSnapper.Layouts.Tests.csproj
dotnet test tests/WindowSnapper.Snap.Tests/WindowSnapper.Snap.Tests.csproj
```

再做相关模块回归：

```bash
dotnet test tests/WindowSnapper.Hotkeys.Tests/WindowSnapper.Hotkeys.Tests.csproj
dotnet test tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj
```

最后做整体构建/测试：

```bash
dotnet build WindowSnapper.sln
dotnet test WindowSnapper.sln
```

---

## 6. 不建议自动运行的测试或命令

| 命令/测试 | 原因 |
|---|---|
| 自动化真实托盘点击测试 | 依赖 Windows 桌面/Explorer 状态，普通单元测试不稳定。 |
| 自动化真实窗口移动测试 | 依赖前台窗口、多显示器、DPI 和桌面会话。 |
| 自动化写入真实 `%APPDATA%/WindowSnapper/layouts` | 会污染用户真实配置；测试应使用临时目录和 `StoragePaths`。 |
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本轮只实现加载/使用自定义布局，不涉及发布。 |

---

## 7. 已知风险

- 本轮 implement 未运行 `dotnet restore`、`dotnet build`、`dotnet test`，需要 test-agent 验证编译和测试。
- 当前自定义布局在 App 启动时加载；运行中直接修改 layouts 目录不会自动刷新。
- 托盘菜单会显示全部内置布局和用户布局；大量用户布局时菜单可能较长，后续可加分组/搜索。
- 用户布局 id 与内置布局冲突时明确跳过用户布局；没有设计“覆盖内置布局”的规则。
- 非法布局提示只显示安全文件名，不显示完整路径；Trace 同样避免完整路径。

---

## 8. 需要人工验证的部分

- [ ] 在 Windows 环境创建 `%APPDATA%/WindowSnapper/layouts/dev-layout.json`，启动 App 后托盘“布局”菜单显示该布局。
- [ ] 选择自定义布局单 zone 项，当前普通窗口移动到对应区域。
- [ ] 创建多 zone 自定义布局，确认托盘显示区域子菜单并能移动。
- [ ] 创建非法 JSON 或校验失败布局，确认 App 不崩溃，并提示对应文件名。
- [ ] 创建与内置 id 冲突的 `left-half` 用户布局，确认内置布局仍可用，用户布局被跳过。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 已执行静态检查：
  - `rg "LoadLayoutsAsync|LayoutLoadResult|LoadedLayoutDefinition|LayoutLoadIssue" -n src tests`，确认调用点已更新。
  - `rg "BuiltinLayouts\\.FindById|LayoutRegistry\\.Create\\(\\[\\]|AppServices.Create\\(" -n src tests`，确认旧内置直查路径和 create 调用已处理。
  - `rg "DllImport|LibraryImport" -n src tests`，确认 P/Invoke 仍集中在 Win32 `NativeMethods.cs`。
  - `rg "File\\.|Directory\\.|Path\\.|System\\.IO" -n src/WindowSnapper.Layouts src/WindowSnapper.Snap`，确认 Layouts/Snap 不读文件系统。
  - `rg "LayoutStorage|SettingsStorage|StoragePaths|AppSettings" -n src/WindowSnapper.Tray src/WindowSnapper.Layouts src/WindowSnapper.Snap`，确认 Tray/Layouts/Snap 不直接依赖 Storage。
  - `git diff --check`，未发现空白错误。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
