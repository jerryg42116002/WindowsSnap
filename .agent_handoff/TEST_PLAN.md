# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现 WindowSnapper.Win32 模块
- 实现日期：2026-05-07
- 相关需求：封装 Windows API，对外暴露 Core 业务类型，避免 UI 直接处理 Win32 原生结构体。
- 实现内容：新增 `NativeMethods`、`Win32WindowManager`、`Win32MonitorManager`、`WindowFilter`、`Win32ErrorMapper`，并新增不依赖真实桌面状态的 Win32 纯逻辑测试项目。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Win32/NativeMethods.cs` | 新增 | 集中声明 User32/DWM P/Invoke 和原生结构体。 |
| `src/WindowSnapper.Win32/Win32WindowManager.cs` | 新增 | 实现 `IWindowManager`，封装活动窗口、窗口信息、Restore、SetWindowPos 移动。 |
| `src/WindowSnapper.Win32/Win32MonitorManager.cs` | 新增 | 实现 `IMonitorManager`，封装 monitor 查询、WorkArea、DPI scale。 |
| `src/WindowSnapper.Win32/WindowFilter.cs` | 新增 | 默认可管理窗口过滤规则。 |
| `src/WindowSnapper.Win32/WindowFilterInfo.cs` | 新增 | WindowFilter 纯逻辑测试输入模型。 |
| `src/WindowSnapper.Win32/Win32ErrorMapper.cs` | 新增 | Win32 错误码到 `ResultErrorCode` 映射。 |
| `src/WindowSnapper.Win32/Win32RectMapper.cs` | 新增 | 原生 RECT 到 `RectInt` 转换。 |
| `src/WindowSnapper.Win32/Win32AssemblyMarker.cs` | 删除 | 移除占位 marker。 |
| `tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj` | 新增 | Win32 纯逻辑测试项目。 |
| `tests/WindowSnapper.Win32.Tests/WindowFilterTests.cs` | 新增 | class name、不可见、最小化、无尺寸、本程序窗口、全屏覆盖判断测试。 |
| `tests/WindowSnapper.Win32.Tests/Win32ErrorMapperTests.cs` | 新增 | Win32 错误映射测试。 |
| `tests/WindowSnapper.Win32.Tests/Win32MonitorManagerTests.cs` | 新增 | monitor 选择纯函数测试，覆盖负坐标。 |
| `tests/WindowSnapper.Win32.Tests/GlobalUsings.cs` | 新增 | 测试项目全局 using。 |
| `WindowSnapper.sln` | 修改 | 加入 `WindowSnapper.Win32.Tests`。 |

---

## 3. 涉及的类、函数、模块

- `NativeMethods`
- `Win32WindowManager.GetActiveWindow()`
- `Win32WindowManager.GetWindowInfo(WindowHandle)`
- `Win32WindowManager.IsWindowManageable(WindowInfo)`
- `Win32WindowManager.RestoreWindow(WindowHandle)`
- `Win32WindowManager.MoveWindow(WindowHandle, RectInt)`
- `Win32MonitorManager.GetMonitors()`
- `Win32MonitorManager.GetMonitorForWindow(WindowHandle)`
- `Win32MonitorManager.GetMonitorForPoint(PointInt)`
- `Win32MonitorManager.SelectMonitorContainingPoint(...)`
- `WindowFilter.IsWindowManageable(...)`
- `WindowFilter.CoversMonitorBounds(...)`
- `Win32ErrorMapper.ToFailure(...)`

---

## 4. 需要重点测试的功能

- [ ] P/Invoke 只存在于 `src/WindowSnapper.Win32/NativeMethods.cs`。
- [ ] `WindowFilter` 忽略 `Shell_TrayWnd`、`Progman`、`WorkerW`、`DV2ControlHost`、`MsgrIMEWindowClass`。
- [ ] `WindowFilter` 忽略不可见窗口、最小化窗口、无尺寸窗口、本程序窗口。
- [ ] `WindowFilter.CoversMonitorBounds` 能识别覆盖完整 monitor bounds 的疑似全屏窗口。
- [ ] `Win32ErrorMapper` 将 access denied 映射到 `PermissionDenied`。
- [ ] `Win32ErrorMapper` 将 invalid window handle 映射到 `NotFound`。
- [ ] monitor 纯选择逻辑支持负坐标副屏。
- [ ] `Win32WindowManager.MoveWindow` 对不可管理窗口返回 `WindowNotManageable`，并使用 `SetWindowPos`。
- [ ] `Win32MonitorManager` 返回 `MonitorInfo.WorkArea`，不要用 `Bounds` 覆盖任务栏区域。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

```bash
dotnet test tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj
```

必要时扩大到：

```bash
dotnet build WindowSnapper.sln
dotnet test WindowSnapper.sln
```

---

## 6. 不建议自动运行的测试或命令

| 命令/测试 | 原因 |
|---|---|
| 真实移动桌面窗口的自动化测试 | 依赖真实 Windows 桌面状态，不稳定，应人工验证。 |
| UAC 安全桌面测试 | 普通权限程序不能管理，也不应强制管理。 |
| 全屏游戏自动化测试 | 不稳定且可能干扰用户环境，应人工验证或后续专项验证。 |
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本次只涉及 Win32 封装和纯逻辑测试，不需要发布验证。 |

---

## 7. 已知风险

- 本轮 implement 未运行 dotnet 验证命令，可能存在编译问题，需要 test-agent 验证。
- 当前环境不是 Windows 桌面环境，无法完整验证真实 Win32/WPF/托盘行为。
- `Win32MonitorManager.GetMonitorForPoint` 无窗口句柄可用时 DPI scale 暂设为 `1.0`；`GetMonitorForWindow` 使用 `GetDpiForWindow`。
- 全屏窗口检测基于窗口 bounds 覆盖 monitor bounds；特殊游戏/渲染窗口仍需要人工验证。
- `ShowWindow(SW_RESTORE)` 的返回值不代表失败，本实现不把 false 当作错误。

---

## 8. 需要人工验证的部分

- [ ] Windows 环境下验证 `GetActiveWindow` 能返回普通桌面应用窗口。
- [ ] Windows 环境下验证 `GetWindowInfo` 能返回 class name、process name、visible/minimized/maximized 和 bounds。
- [ ] Windows 多显示器环境下验证 `GetMonitorForWindow` 使用正确 monitor，并返回 WorkArea。
- [ ] Windows DPI 缩放环境下验证 `DpiScale` 来自 `GetDpiForWindow`。
- [ ] Windows 环境下验证最大化窗口先 Restore，再 `SetWindowPos` 移动。
- [ ] 验证管理员权限窗口/UAC/系统窗口失败时返回明确 Result，不崩溃、不提权。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 仅执行了文档/源码读取和 `rg` 静态搜索检查。
- 未实现 UI、托盘、快捷键注册、注入、提权、网络、遥测或云同步。
- 静态扫描确认 `DllImport` 只出现在 `src/WindowSnapper.Win32/NativeMethods.cs`。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
