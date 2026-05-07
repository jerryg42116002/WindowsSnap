# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：打通 MVP 快捷键移动当前活动窗口闭环
- 实现日期：2026-05-07
- 相关需求：按下默认全局快捷键后，由 `HotkeyManager` 分发命令，App 组合根调用 `WindowSnapService`，使用 Win32 窗口/显示器封装和 `LayoutEngine` 将活动窗口移动到内置布局区域。
- 实现内容：
  - 新增纯业务项目 `WindowSnapper.Snap`，包含 `SnapCommand`、`WindowSnapService`、`IWindowSnapLogger`。
  - 公开 `BuiltinLayouts` 的稳定 layout/zone id 常量，避免业务映射散落 magic string。
  - 在 Win32 项目新增 `RegisterHotKey` / `UnregisterHotKey` 薄封装，P/Invoke 仍集中在 `NativeMethods`。
  - 在 WPF App 组合根装配 `HotkeyManager`、`WpfHotkeyRegistrar`、`WindowSnapService`、`Win32WindowManager`、`Win32MonitorManager`、`LayoutEngine` 和 Trace 日志。
  - 新增 `WindowSnapper.Snap.Tests`，覆盖命令映射、不可管理窗口、目标 Rect 传递、最大化 Restore、Move 失败友好错误。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Snap/WindowSnapper.Snap.csproj` | 新增 | 纯业务编排项目，依赖 Core、Layouts、Hotkeys。 |
| `src/WindowSnapper.Snap/SnapCommand.cs` | 新增 | 将 `HotkeyCommand` 映射到内置 layout id / zone id。 |
| `src/WindowSnapper.Snap/WindowSnapService.cs` | 新增 | 编排获取活动窗口、判断可管理、获取显示器、计算 WorkArea 目标 Rect、Restore、Move。 |
| `src/WindowSnapper.Snap/IWindowSnapLogger.cs` | 新增 | 不记录窗口标题的 snap 日志抽象。 |
| `src/WindowSnapper.Snap/NullWindowSnapLogger.cs` | 新增 | 默认空日志实现，便于单元测试。 |
| `src/WindowSnapper.Layouts/BuiltinLayouts.cs` | 修改 | 公开稳定内置 layout id 和 `MainZoneId`。 |
| `src/WindowSnapper.Win32/NativeMethods.cs` | 修改 | 新增 `RegisterHotKey`、`UnregisterHotKey`、`WM_HOTKEY` 声明。 |
| `src/WindowSnapper.Win32/Win32HotkeyRegistrar.cs` | 新增 | 对 Win32 hotkey API 的业务友好薄封装。 |
| `src/WindowSnapper.App/App.xaml` | 修改 | 移除 `StartupUri`，改由组合根创建主窗口和服务。 |
| `src/WindowSnapper.App/App.xaml.cs` | 修改 | WPF 启动/退出装配，接收 hotkey 事件后调用 `WindowSnapService`。 |
| `src/WindowSnapper.App/Composition/AppServices.cs` | 新增 | 手动 DI/组合根，集中注册 App 所需服务。 |
| `src/WindowSnapper.App/Hotkeys/WpfHotkeyRegistrar.cs` | 新增 | WPF HWND 消息适配器，实现 `IHotkeyRegistrar`，不包含 P/Invoke。 |
| `src/WindowSnapper.App/Logging/TraceWindowSnapLogger.cs` | 新增 | Trace 日志实现，不记录窗口标题。 |
| `src/WindowSnapper.App/WindowSnapper.App.csproj` | 修改 | 引用 `WindowSnapper.Snap`。 |
| `tests/WindowSnapper.Snap.Tests/WindowSnapper.Snap.Tests.csproj` | 新增 | Snap 业务测试项目。 |
| `tests/WindowSnapper.Snap.Tests/GlobalUsings.cs` | 新增 | 测试全局 using。 |
| `tests/WindowSnapper.Snap.Tests/SnapCommandTests.cs` | 新增 | Hotkey 命令到 layout/zone 的映射测试。 |
| `tests/WindowSnapper.Snap.Tests/WindowSnapServiceTests.cs` | 新增 | Snap 核心服务行为测试。 |
| `WindowSnapper.sln` | 修改 | 加入 `WindowSnapper.Snap` 和 `WindowSnapper.Snap.Tests`。 |

---

## 3. 涉及的类、函数、模块

- `SnapCommand.FromHotkeyCommand(HotkeyCommand)`
- `WindowSnapService.SnapActiveWindow(SnapCommand)`
- `IWindowSnapLogger`
- `BuiltinLayouts.LeftHalfId` / `RightHalfId` / `TopHalfId` / `BottomHalfId` / `Quad*Id` / `MainZoneId`
- `Win32HotkeyRegistrar.RegisterHotkey(...)`
- `Win32HotkeyRegistrar.UnregisterHotkey(...)`
- `WpfHotkeyRegistrar.Register(...)`
- `WpfHotkeyRegistrar.UnregisterAll()`
- `AppServices.Create(MainWindow)`
- `App.OnStartup(...)`
- `App.OnHotkeyPressed(...)`

---

## 4. 需要重点测试的功能

- [ ] `SnapCommand.FromHotkeyCommand`：
  - `SnapLeftHalf` -> `left-half` / `main`
  - `SnapRightHalf` -> `right-half` / `main`
  - `SnapTopHalf` -> `top-half` / `main`
  - `SnapBottomHalf` -> `bottom-half` / `main`
  - `SnapZone1-4` -> 四宫格四个内置 layout / `main`
  - `OpenLayoutSelector` 不执行窗口移动，返回 `NotSupported`
- [ ] `WindowSnapService.SnapActiveWindow`：
  - 窗口不可管理时返回 `WindowNotManageable` 和用户友好错误。
  - 使用 `MonitorInfo.WorkArea` 计算目标 Rect，不使用 Bounds。
  - 副屏负坐标 WorkArea 下计算结果原样传给 `IWindowManager.MoveWindow`。
  - 最大化窗口在 Move 前调用 `RestoreWindow`。
  - `MoveWindow` 失败时保留错误码，但返回用户友好错误消息。
- [ ] App 启动：
  - 主窗口只创建一次。
  - 默认快捷键注册失败不崩溃并显示友好提示。
  - App 退出时 `HotkeyManager.Dispose()` 触发注销。
- [ ] Win32 层：
  - `DllImport` 仍只出现在 `src/WindowSnapper.Win32/NativeMethods.cs`。
  - `Win32HotkeyRegistrar` 失败时通过 `Win32ErrorMapper` 返回明确错误。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

优先运行纯业务测试：

```bash
dotnet test tests/WindowSnapper.Snap.Tests/WindowSnapper.Snap.Tests.csproj
```

再运行相关模块测试：

```bash
dotnet test tests/WindowSnapper.Hotkeys.Tests/WindowSnapper.Hotkeys.Tests.csproj
dotnet test tests/WindowSnapper.Layouts.Tests/WindowSnapper.Layouts.Tests.csproj
dotnet test tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj
```

最后做整体验证：

```bash
dotnet build WindowSnapper.sln
dotnet test WindowSnapper.sln
```

---

## 6. 不建议自动运行的测试或命令

| 命令/测试 | 原因 |
|---|---|
| 自动化真实全局快捷键注册/窗口移动测试 | 依赖 Windows 桌面会话、前台窗口状态和快捷键占用情况，CI/非交互环境不稳定。 |
| 自动化移动管理员权限窗口、UAC 安全桌面、全屏游戏 | AGENTS.md 明确不应强制管理这些窗口。 |
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本轮只打通 MVP 运行闭环，发布验证留到后续打包阶段。 |

---

## 7. 已知风险

- 本轮 implement 未运行 `dotnet restore`、`dotnet build`、`dotnet test`，需要 test-agent 验证编译和测试。
- 当前 `Ctrl+Alt+1` 到 `Ctrl+Alt+4` 暂时映射为四宫格内置布局；尚未接入“当前用户自定义布局”的 zone 选择。
- `Ctrl+Alt+Space` 仅被识别为非移动命令，本轮未实现布局选择器 UI。
- `HotkeyManager.RegisterMany` 在部分快捷键已注册后遇到后续注册失败时，当前实现不会自动回滚已注册项；App 会提示部分注册失败。
- 真实 `RegisterHotKey` 可能因系统或其他程序占用快捷键而失败；应返回错误，不应崩溃。
- WPF/Win32 真实行为需要 Windows 桌面环境人工验证；非 Windows 环境不能证明真实窗口移动和全局快捷键可用。

---

## 8. 需要人工验证的部分

- [ ] 在 Windows 桌面环境启动 App，确认没有创建重复主窗口。
- [ ] 按 `Ctrl+Alt+Left`，当前普通应用窗口移动到当前显示器 WorkArea 左半屏。
- [ ] 按 `Ctrl+Alt+Right`、`Up`、`Down`，分别移动到右/上/下半屏。
- [ ] 按 `Ctrl+Alt+1` 到 `Ctrl+Alt+4`，分别移动到四宫格对应区域。
- [ ] 在副屏位于主屏左侧、坐标为负的环境下验证窗口仍移动到副屏 WorkArea 正确位置。
- [ ] 在任务栏不在底部或多显示器不同 DPI 场景下，确认不覆盖任务栏。
- [ ] 尝试管理员权限窗口、系统窗口、最小化窗口，确认失败时不崩溃并显示友好提示。
- [ ] 退出 App 后确认全局快捷键被释放。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 已执行静态检查：
  - `rg "DllImport|LibraryImport" -n src tests`，确认 P/Invoke 仍集中在 Win32 `NativeMethods.cs`。
  - `rg "System\\.Windows|WindowInterop|MessageBox" ...`，确认 Core、Layouts、Storage、Hotkeys、Win32、Snap 和 Snap.Tests 未引入 WPF UI 引用。
  - `git diff --check`，未发现空白错误。
- 未实现复杂布局编辑器、托盘菜单、配置覆盖快捷键、布局选择器 UI、管理员权限提升、遥测、网络或云同步。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
