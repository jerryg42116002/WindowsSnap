# Window Management

本文档说明 WindowSnapper 当前 MVP 的窗口移动流程、窗口过滤规则、多显示器和 DPI 注意事项。

Win32 P/Invoke 声明必须只放在 `WindowSnapper.Win32`。WPF App 通过 `WindowSnapService` 调用业务流程，不直接调用 `NativeMethods`。

## 窗口移动流程

当前快捷键或托盘布局命令触发后，流程如下：

1. `HotkeyManager` 或托盘菜单发出命令。
2. App 将命令转换为 `SnapCommand`。
3. `WindowSnapService` 调用 `IWindowManager.GetActiveWindow()` 获取当前活动窗口。
4. 调用 `IWindowManager.GetWindowInfo()` 获取窗口信息。
5. 调用 `IWindowManager.IsWindowManageable()` 判断窗口是否可管理。
6. 调用 `IMonitorManager.GetMonitorForWindow()` 获取窗口所在显示器。
7. 从 `LayoutRegistry` 查找目标布局。
8. `LayoutEngine` 基于 `MonitorInfo.WorkArea` 计算目标 `RectInt`。
9. 如果 `showOverlayPreview=true`，显示短暂半透明 Overlay Preview。
10. 如果窗口最大化，先调用 `RestoreWindow()`。
11. 调用 `IWindowManager.MoveWindow()` 移动窗口。
12. Win32 失败时映射为 `Result`，App 显示用户友好错误。

窗口选择器移动指定窗口时，入口不同，但核心流程仍由 `WindowSnapService` 执行：

1. `WindowSelectorViewModel` 保存移动前快照。
2. 快照记录窗口句柄、可见区域 bounds、是否可见、是否最小化。
3. 调用 `WindowSnapService.SnapWindow(window, command)` 移动指定窗口。
4. 如果窗口当前最小化或隐藏，先调用 `RestoreWindow()` 让窗口可移动。
5. 移动完成后，用户可点击“还原”。
6. 还原时先移动回原始 bounds，再按快照恢复为最小化或隐藏状态。

用户可见错误统一避免暴露原始 Win32 细节，例如：

```text
无法移动该窗口。它可能是系统窗口、管理员权限窗口，或当前不允许调整大小。
```

## 可管理窗口规则

默认只管理普通桌面应用窗口。

当前过滤条件包括：

- 空窗口句柄不管理。
- 不可见窗口不管理。
- 最小化窗口不管理。
- 宽度或高度小于等于 `0` 的窗口不管理。
- 当前 WindowSnapper 进程窗口不管理。
- 配置中的忽略进程不管理。
- 配置中的忽略窗口类名不管理。
- 覆盖完整显示器边界的疑似全屏窗口不管理。

默认忽略的窗口类名包括：

```text
Shell_TrayWnd
Progman
WorkerW
DV2ControlHost
MsgrIMEWindowClass
WindowSnapperOverlayWindow
```

`WindowSnapperOverlayWindow` 用于 Overlay Preview，必须保持在忽略列表中，避免预览窗口被 WindowSnapper 自己管理。

默认忽略的进程包括：

```text
explorer.exe
ApplicationFrameHost.exe
```

用户可在设置窗口中编辑忽略进程和忽略窗口类名。配置迁移会保留必要的默认忽略窗口类名。

窗口选择器为了支持移动最小化窗口，会在枚举时允许最小化窗口出现在列表中，但不会改变 `WindowFilter` 的默认规则。真正移动前仍由 `WindowSnapService` 先恢复窗口，再执行可管理性判断和移动。

隐藏窗口只有在仍能通过 `EnumWindows` 枚举到顶层窗口句柄时才可能出现在窗口选择器中。完全隐藏到托盘且不暴露普通顶层窗口的程序，不保证可选择或可还原。

## Win32 封装

`WindowSnapper.Win32` 当前封装的职责包括：

- 获取前台窗口。
- 获取窗口矩形、标题、进程名、窗口类名和状态。
- 判断窗口是否可见、是否最小化、是否最大化。
- 获取窗口所在显示器。
- 获取显示器 `Bounds` 和 `WorkArea`。
- 读取窗口 DPI 比例。
- 还原最大化、最小化或隐藏窗口。
- 最小化窗口。
- 隐藏窗口。
- 使用 `SetWindowPos` 移动窗口。
- 注册和注销全局快捷键。
- 为 Overlay Preview 设置非激活、透明点击穿透和工具窗口样式。

Win32 调用失败时通过 `Win32ErrorMapper` 转换为明确的 `ResultErrorCode` 和诊断消息。

## 可见边框补偿

Windows 现代窗口经常存在不可见 resize border。`GetWindowRect` 返回的是外框，DWM 扩展边框更接近用户实际看到的可见区域。如果直接把 LayoutEngine 的目标矩形传给 `SetWindowPos`，即使 `gap=0` 和 `margin=0`，相邻窗口之间也可能出现视觉空隙。

当前实现规则：

- LayoutEngine 只计算目标可见区域。
- `Win32WindowManager.MoveWindow` 在调用 `SetWindowPos` 前读取当前外框和 DWM 可见边框。
- Win32 层按当前窗口的不可见边框厚度反推出目标外框。
- 如果 DWM 或外框读取失败，回退到原始目标矩形，避免移动流程崩溃。

该补偿只属于 Win32 封装层。UI、Snap 和 Layouts 不直接处理 DWM 结构体。

## 窗口选择器与还原

窗口选择器用于手动选择目标窗口，而不是只操作当前活动窗口。当前支持：

- 枚举当前可管理窗口。
- 显示最小化窗口。
- 单选窗口移动到指定区域。
- 多选窗口从选中的起始区域开始依次移动。
- 为某个窗口点击“选定”，固定它要移动到的布局区域。
- 点击“还原”，恢复最近一次移动前的位置和状态。

还原状态规则：

- 移动前是普通显示窗口：还原后保持普通显示。
- 移动前是最小化窗口：移动时会先恢复，点击还原后会再次最小化。
- 移动前是隐藏窗口：移动时会先恢复，点击还原后会再次隐藏。
- 还原快照是内存状态；刷新窗口列表、关闭选择器或重启应用后不会保留。

多选移动规则：

- 窗口选择器中的区域按区域名称排序，再按 id 排序。
- 多选窗口时，从当前选中的起始区域开始，依次把窗口放入排序后的后续区域。
- 如果剩余区域数量不足，移动会失败并显示友好错误。

固定目标规则：

- 点击“选定”时必须只选中一个窗口。
- 被选定的窗口会在列表中显示目标区域。
- 点击“移动窗口”时，如果存在固定目标，优先移动所有已选定固定目标的窗口。

## 多显示器注意事项

布局计算不得假设屏幕从 `(0, 0)` 开始。Windows 多显示器虚拟桌面可能出现：

- 副屏在主屏左侧，坐标为负数。
- 副屏在主屏上方，Y 坐标为负数。
- 横屏和竖屏混用。
- 任务栏不在底部。
- 每个显示器的 `Bounds` 和 `WorkArea` 不同。

WindowSnapper 使用 `MonitorInfo.WorkArea` 计算目标区域，因此不会主动覆盖任务栏占用区域。

## DPI 注意事项

`Win32MonitorManager.GetMonitorForWindow()` 会通过 `GetDpiForWindow()` 计算窗口所在显示器的 `DpiScale`。Overlay Preview 在 WPF 层显示时会按该比例把像素坐标换算为 WPF DIP。

需要注意：

- 真实多 DPI 桌面环境仍需 Windows 人工验证。
- 不同 DPI 显示器之间移动窗口时，Win32 和 WPF 的坐标单位容易混淆。
- Layouts 层只处理整数像素矩形，不直接处理 WPF DIP。

## 管理员权限窗口限制

普通权限运行的 WindowSnapper 通常不能稳定控制管理员权限窗口。当前设计不自动请求管理员权限，也不尝试绕过 Windows 权限边界。

如果移动失败，服务返回失败结果，App 显示用户友好提示。

## UAC、全屏游戏和系统窗口限制

WindowSnapper 当前不支持也不应强制管理：

- UAC 安全桌面。
- 全屏独占游戏。
- 任务栏。
- 桌面窗口。
- 开始菜单。
- 系统弹窗。
- 特殊渲染窗口。
- 被 Windows 权限或窗口样式限制调整大小的窗口。

这些限制是稳定性和安全边界的一部分，不应通过注入其他进程或自动提升权限绕过。

## Overlay Preview

Overlay Preview 只用于显示即将移动到的目标矩形：

- 不参与布局计算。
- 目标矩形仍由 `LayoutEngine` 计算。
- `showOverlayPreview=false` 时不显示。
- 默认透明度来自 `overlayOpacity`，当前默认值为 `0.35`。
- Overlay 窗口尽量不抢焦点，使用非激活窗口样式。
- Overlay 窗口被加入忽略规则，避免被自身管理。

Overlay Preview 的真实焦点行为、多显示器负坐标和不同 DPI 表现需要在 Windows 桌面环境中验证。

## 工作区快照恢复

Workspace Snapshot 保存当前可管理窗口的位置，并在之后恢复。

快照保存：

- 保存进程名。
- 保存窗口类名。
- 保存显示器设备名。
- 保存相对于显示器 `WorkArea` 的位置。
- 保存窗口状态：普通、最小化或最大化。

快照不默认保存：

- 完整窗口标题。
- 浏览器 URL。
- 用户文件路径。
- 命令行参数。

恢复时：

- 根据进程名和窗口类名匹配仍在运行的窗口。
- 根据快照中的显示器设备名选择显示器；找不到时回退到主显示器或第一个显示器。
- 将相对位置转换为当前 `WorkArea` 下的绝对矩形。
- 移动窗口。
- 如果快照状态为最小化，移动后重新最小化。

当前工作区快照匹配规则有限。同一进程、同一窗口类名的多个窗口可能无法精确区分。
