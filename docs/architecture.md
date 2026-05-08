# Architecture

WindowSnapper 采用小项目分层，依赖方向保持单向。UI 层负责组合和用户交互，底层模块负责纯模型、布局计算、配置读写、Win32 封装和快捷键抽象。

## 项目分层

```text
WindowSnapper.App
  -> WindowSnapper.Core
  -> WindowSnapper.Layouts
  -> WindowSnapper.Storage
  -> WindowSnapper.Win32
  -> WindowSnapper.Hotkeys
  -> WindowSnapper.Snap
  -> WindowSnapper.Tray
  -> WindowSnapper.Workspaces

WindowSnapper.Snap
  -> WindowSnapper.Core
  -> WindowSnapper.Layouts
  -> WindowSnapper.Hotkeys

WindowSnapper.Workspaces
  -> WindowSnapper.Core

WindowSnapper.Layouts
  -> WindowSnapper.Core

WindowSnapper.Storage
  -> WindowSnapper.Core
  -> WindowSnapper.Layouts

WindowSnapper.Win32
  -> WindowSnapper.Core

WindowSnapper.Hotkeys
  -> WindowSnapper.Core

WindowSnapper.Tray
  -> WindowSnapper.Core
```

禁止方向：

```text
Core -> App / WPF / Win32 / Storage
Layouts -> WPF / Win32 / Storage
Storage -> WPF / Win32
Hotkeys -> WPF / Win32
Tray -> Win32 P/Invoke / 布局计算 / 配置解析
App code-behind -> NativeMethods
Workspaces -> WPF / Win32 / Storage 实现细节
```

## 模块职责

### WindowSnapper.Core

Core 只包含通用模型、接口和结果类型：

- `WindowHandle`
- `WindowInfo`
- `MonitorInfo`
- `RectInt`
- `PointInt`
- `Result`
- `Result<T>`
- `IWindowManager`
- `IMonitorManager`
- `IClock`
- 工作区快照模型，例如 `WorkspaceSnapshot`、`WorkspaceWindowSnapshot`、`RelativeRect`

Core 不引用 WPF、Win32、P/Invoke、文件系统或配置读写。

### WindowSnapper.Layouts

Layouts 负责布局模型、校验、计算和内置布局：

- `LayoutDefinition`
- `ZoneDefinition`
- `ZoneRect`
- `LayoutEngine`
- `LayoutValidator`
- `BuiltinLayouts`
- `LayoutRegistry`

布局计算只接收 `MonitorInfo.WorkArea`、布局定义和 zone id，不读取真实显示器、不访问文件系统、不调用 Win32。

### WindowSnapper.Storage

Storage 负责本地 JSON 配置和用户布局文件：

- `AppSettings`
- `SettingsStorage`
- `LayoutStorage`
- `WorkspaceSnapshotStorage`
- `ConfigMigration`
- `DefaultSettingsFactory`
- `StoragePaths`

Storage 会创建默认配置、迁移 schema、原子写入 JSON，并在配置损坏时备份为 `.bak`。用户布局读取后交给 `LayoutValidator` 校验。工作区快照保存在本地 JSON 文件中，不保存完整窗口标题、浏览器 URL、用户文件路径或命令行参数。

### WindowSnapper.Win32

Win32 是唯一包含 P/Invoke 的项目：

- `NativeMethods`
- `Win32WindowManager`
- `Win32MonitorManager`
- `Win32HotkeyRegistrar`
- `WindowFilter`
- `Win32ErrorMapper`
- `Win32OverlayWindowStyleService`

该层把 Win32 结构和错误转换为 Core 类型与 `Result`，避免 UI 层处理原生结构体或直接调用 Windows API。

当前窗口移动还包含 DWM 可见边框补偿：`WindowSnapper.Win32` 使用 `GetWindowRect` 和 `DwmGetWindowAttribute` 反推传给 `SetWindowPos` 的外框坐标，让上层仍以用户可见区域作为目标矩形。该逻辑不能放入 Layouts 或 App。

### WindowSnapper.Hotkeys

Hotkeys 负责快捷键业务抽象：

- `HotkeyDefinition`
- `HotkeyModifiers`
- `HotkeyKey`
- `HotkeyCommand`
- `HotkeyParser`
- `HotkeyManager`
- `IHotkeyRegistrar`

`RegisterHotKey` 和 `UnregisterHotKey` 的实际 Win32 调用不在 Hotkeys 层，而是在 Win32 层实现。

### WindowSnapper.Snap

Snap 是当前 MVP 的应用服务层，负责串联窗口移动闭环：

- `SnapCommand`
- `WindowSnapService`
- `IWindowSnapLogger`
- `IOverlayPreviewService`
- `OverlayPreviewOptions`
- `RepeatHotkeyCycleService`

Snap 不依赖 WPF 或 Win32 实现，只依赖 Core 接口和 Layouts。App 在启动时注入 Win32 实现和 WPF Overlay 实现。窗口选择器的“还原”也通过 Snap 服务恢复移动前位置和状态；如果窗口移动前是最小化或隐藏状态，还原后会调用 Core 接口恢复对应状态。

### WindowSnapper.Workspaces

Workspaces 负责工作区快照的业务流程：

- 枚举当前可管理窗口。
- 记录进程名、窗口类名、显示器名、相对位置和窗口状态。
- 根据快照匹配仍在运行的窗口。
- 将相对位置转换回当前显示器 `WorkArea` 中的绝对位置。
- 恢复最近或指定快照。

Workspaces 只依赖 Core 抽象，不直接读取文件系统，不直接调用 Win32。持久化由 Storage 中的 `WorkspaceSnapshotStorage` 提供，真实窗口枚举和移动由 App 注入 Win32 实现。

### WindowSnapper.Tray

Tray 负责系统托盘入口和菜单：

- 打开主窗口
- 打开设置窗口
- 暂停/恢复快捷键
- 保存工作区快照
- 恢复最近工作区快照
- 退出程序
- 显示可用布局和 zone 菜单

Tray 不做布局计算，不读写配置，不包含 P/Invoke。

### WindowSnapper.App

App 是 WPF 主程序，负责：

- 应用启动和退出。
- 依赖组合。
- 加载设置和自定义布局。
- 注册默认快捷键。
- 创建托盘图标。
- 主窗口、设置窗口、窗口选择器、布局编辑器和 Overlay Preview。
- 把 UI 事件转发给应用服务。

WPF code-behind 保持轻量，不直接调用 `NativeMethods`。

## 为什么 Win32 不写进 UI 层

WindowSnapper 的窗口管理涉及权限、DPI、多显示器、焦点、系统窗口过滤和 Win32 错误处理。如果把 P/Invoke 写进 WPF 窗口或按钮事件，会带来几个问题：

- UI 事件会混入窗口移动流程，难以测试。
- 原生结构体和 Win32 错误码会泄露到用户界面层。
- 快捷键、托盘菜单和未来其他入口无法复用同一套窗口移动逻辑。
- 容易绕过可管理窗口规则，误操作任务栏、桌面、Overlay 或系统窗口。
- DPI、负坐标、多显示器处理会分散到多个 UI 文件中。

当前架构让 UI 只调用 `WindowSnapService`，由 Win32 层负责 API 细节，由 Layouts 层负责目标区域计算。
