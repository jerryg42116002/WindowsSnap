# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现窗口移动前 Overlay Preview 预览
- 实现日期：2026-05-07
- 相关需求：触发快捷键或托盘布局区域后，在目标显示器 WorkArea 上短暂显示半透明矩形；Overlay 不参与布局计算；支持负坐标、多显示器、DPI；可由 `showOverlayPreview` / `overlayOpacity` 配置控制。
- 实现内容：
  - Snap 业务层新增 `IOverlayPreviewService`、`OverlayPreviewOptions`、`NullOverlayPreviewService`。
  - `WindowSnapService` 在 `LayoutEngine` 计算出目标 Rect 后、Restore/Move 前调用 overlay preview；关闭配置时不调用。
  - App 层新增 WPF `OverlayWindow` 和 `OverlayPreviewService`。
  - Win32 层新增 overlay 窗口样式服务，用于设置 `NOACTIVATE` / `TRANSPARENT` / `TOOLWINDOW` 扩展样式；P/Invoke 仍集中在 `NativeMethods`。
  - `AppServices` 从 `AppSettings.ShowOverlayPreview` / `OverlayOpacity` 创建 preview options 并注入 `WindowSnapService`。
  - Overlay 内部标记 `WindowSnapperOverlayWindow` 加入默认忽略窗口类和 WindowFilter 默认忽略类。
  - 测试覆盖 preview 关闭不调用、目标 Rect 传递、默认 opacity、默认忽略类。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Snap/IOverlayPreviewService.cs` | 新增 | Overlay preview 抽象，避免 Snap 层依赖 WPF。 |
| `src/WindowSnapper.Snap/OverlayPreviewOptions.cs` | 新增 | Preview 开关和 opacity 选项，默认 opacity 为 `0.35`。 |
| `src/WindowSnapper.Snap/NullOverlayPreviewService.cs` | 新增 | Preview 禁用或未接入时的 no-op 实现。 |
| `src/WindowSnapper.Snap/WindowSnapService.cs` | 修改 | 在目标 Rect 计算后、Move 前调用 preview service。 |
| `src/WindowSnapper.App/Overlay/OverlayWindow.xaml` | 新增 | 半透明 preview 窗口。 |
| `src/WindowSnapper.App/Overlay/OverlayWindow.xaml.cs` | 新增 | 根据目标 Rect 和 DPI scale 定位 overlay。 |
| `src/WindowSnapper.App/Overlay/OverlayPreviewService.cs` | 新增 | WPF overlay preview 服务，短暂显示后关闭。 |
| `src/WindowSnapper.App/Composition/AppServices.cs` | 修改 | 从配置创建 overlay options，注入 preview service。 |
| `src/WindowSnapper.Win32/NativeMethods.cs` | 修改 | 新增 `GetWindowLong` / `SetWindowLong` 和 overlay 扩展样式常量。 |
| `src/WindowSnapper.Win32/Win32OverlayWindowStyleService.cs` | 新增 | Win32 overlay 样式封装，不暴露 P/Invoke 给 App。 |
| `src/WindowSnapper.Win32/WindowFilter.cs` | 修改 | 默认忽略 `WindowSnapperOverlayWindow`。 |
| `src/WindowSnapper.Storage/AppSettings.cs` | 修改 | 默认忽略窗口类加入 overlay 内部标记。 |
| `src/WindowSnapper.Storage/ConfigMigration.cs` | 修改 | 合并必要默认忽略类；无效 overlay opacity 恢复默认。 |
| `tests/WindowSnapper.Snap.Tests/WindowSnapServiceTests.cs` | 修改 | 覆盖 preview 开关、target Rect 传递、默认 opacity。 |
| `tests/WindowSnapper.Storage.Tests/SettingsStorageTests.cs` | 修改 | 覆盖默认 overlay opacity 和 overlay 忽略类。 |
| `tests/WindowSnapper.Win32.Tests/WindowFilterTests.cs` | 修改 | 覆盖 overlay class name 被忽略。 |

---

## 3. 涉及的类、函数、模块

- `WindowSnapService.SnapActiveWindow(SnapCommand)`
- `WindowSnapService.ShowOverlayPreviewIfEnabled(...)`
- `IOverlayPreviewService.ShowPreview(...)`
- `OverlayPreviewOptions`
- `OverlayPreviewService.ShowPreview(...)`
- `OverlayWindow.Configure(...)`
- `Win32OverlayWindowStyleService.ApplyOverlayStyles(...)`
- `AppServices.CreateWindowSnapService(...)`
- `AppSettings.ShowOverlayPreview`
- `AppSettings.OverlayOpacity`
- `ConfigMigration.Migrate(...)`
- `WindowFilter`

---

## 4. 需要重点测试的功能

- [ ] `WindowSnapService`：
  - `showOverlayPreview=false` / disabled options 时不调用 overlay service。
  - enabled options 时把 `LayoutEngine` 算出的目标 `RectInt` 原样传给 overlay service。
  - preview 调用发生在 Move 前，且 preview 失败不阻止窗口移动。
  - 默认 opacity 为 `0.35`。
- [ ] `OverlayWindow` / `OverlayPreviewService`：
  - 目标 Rect 为负坐标时 `Left` / `Top` 可为负。
  - `MonitorInfo.DpiScale` 不为 1 时，WPF 坐标按 DPI scale 转换。
  - 窗口 `ShowActivated=false`、`ShowInTaskbar=false`、`Topmost=true`。
  - preview 窗口短暂显示后关闭。
- [ ] Win32：
  - P/Invoke 只在 `WindowSnapper.Win32/NativeMethods.cs`。
  - `Win32OverlayWindowStyleService` 应用 `WS_EX_NOACTIVATE`、`WS_EX_TRANSPARENT`、`WS_EX_TOOLWINDOW`。
- [ ] Storage / WindowFilter：
  - `overlayOpacity` 缺失时默认 `0.35`。
  - `WindowSnapperOverlayWindow` 在默认 ignored window classes 中。
  - `WindowFilter` 忽略 `WindowSnapperOverlayWindow`。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

优先运行纯逻辑测试：

```bash
dotnet test tests/WindowSnapper.Snap.Tests/WindowSnapper.Snap.Tests.csproj
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
dotnet test tests/WindowSnapper.Win32.Tests/WindowSnapper.Win32.Tests.csproj
```

再做相关回归：

```bash
dotnet test tests/WindowSnapper.Layouts.Tests/WindowSnapper.Layouts.Tests.csproj
dotnet test tests/WindowSnapper.Hotkeys.Tests/WindowSnapper.Hotkeys.Tests.csproj
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
| 自动化真实 overlay WPF 截图测试 | 依赖 Windows 桌面会话、DPI 和窗口管理器状态。 |
| 自动化真实快捷键移动窗口测试 | 依赖前台窗口、多显示器、DPI 和用户桌面状态。 |
| 自动化管理管理员权限窗口/UAC/全屏游戏 | AGENTS.md 明确不应强制管理这些窗口。 |
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本轮只实现 preview 功能，不涉及发布。 |

---

## 7. 已知风险

- 本轮 implement 未运行 `dotnet restore`、`dotnet build`、`dotnet test`，需要 test-agent 验证编译和测试。
- WPF 坐标使用 `MonitorInfo.DpiScale` 做基础转换；真实多 DPI 场景仍需 Windows 人工验证。
- Overlay 显示为非激活、透明点击穿透、tool window；实际焦点行为需要 Windows 环境确认。
- Overlay class name/internal marker 已加入忽略列表；当前进程窗口也会被 `WindowFilter` 忽略，因此 overlay 不应被 WindowSnapper 管理。

---

## 8. 需要人工验证的部分

- [ ] Windows 桌面环境中按 `Ctrl+Alt+Left`，移动前目标区域出现半透明矩形。
- [ ] 设置 `showOverlayPreview=false` 后不显示 preview，但窗口仍移动。
- [ ] 修改 `overlayOpacity` 配置后 preview 透明度变化。
- [ ] 副屏位于主屏左侧、坐标为负时，preview 显示在正确显示器位置。
- [ ] 不同 DPI 显示器上，preview 与最终移动目标区域尽量一致。
- [ ] Preview 出现时不抢当前活动窗口焦点，不显示在任务栏。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 已执行静态检查：
  - `rg "WindowSnapService\\(|OverlayPreview|IOverlayPreviewService|Win32OverlayWindowStyleService|WindowSnapperOverlayWindow" -n src tests`，确认接入点。
  - `rg "DllImport|LibraryImport" -n src tests`，确认 P/Invoke 仍集中在 Win32 `NativeMethods.cs`。
  - `rg "NativeMethods|GetWindowLong|SetWindowLong" -n src/WindowSnapper.App ...`，确认 App 不直接调用 `NativeMethods`。
  - `rg "File\\.|Directory\\.|Path\\.|System\\.IO" -n src/WindowSnapper.Layouts src/WindowSnapper.Snap`，确认 Layouts/Snap 不读文件系统。
  - `git diff --check`，未发现空白错误。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
