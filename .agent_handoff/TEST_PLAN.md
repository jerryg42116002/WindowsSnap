# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现 WindowSnapper.App 和 WindowSnapper.Tray 的 MVP 用户入口
- 实现日期：2026-05-07
- 相关需求：启动后常驻托盘；托盘可打开主窗口/设置窗口、暂停/恢复快捷键、退出程序；关闭主窗口时默认最小化到托盘；退出时注销快捷键并释放托盘图标；设置变更保存到 Storage。
- 实现内容：
  - 新增 Tray 模块的 `NotifyIcon` 托盘入口和菜单命令模型。
  - App 启动改为 `AppController` 控制，加载 Storage 配置，创建主窗口、设置窗口、托盘图标和核心服务。
  - 新增 MVVM 基础设施、主窗口 ViewModel、设置窗口 ViewModel。
  - 新增设置窗口 MVP，支持 `minimizeToTray`、`showOverlayPreview`、`defaultGap`、`defaultMargin`、`ignoredProcesses`、`ignoredWindowClasses`、暂停快捷键。
  - 新增 `AppSettings.HotkeysPaused` 并将配置版本升级到 `2`，通过 Storage 迁移默认值。
  - App 退出时集中释放托盘图标、注销快捷键、释放 hotkey registrar。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Tray/TrayAssemblyMarker.cs` | 删除 | 移除占位 marker。 |
| `src/WindowSnapper.Tray/ITrayIcon.cs` | 新增 | 托盘图标抽象。 |
| `src/WindowSnapper.Tray/TrayMenuCommand.cs` | 新增 | 托盘菜单命令枚举。 |
| `src/WindowSnapper.Tray/TrayMenuCommandEventArgs.cs` | 新增 | 托盘命令事件参数。 |
| `src/WindowSnapper.Tray/TrayMenuState.cs` | 新增 | 托盘菜单状态模型。 |
| `src/WindowSnapper.Tray/NotifyIconTrayIcon.cs` | 新增 | WinForms `NotifyIcon` 托盘实现。 |
| `src/WindowSnapper.App/App.xaml.cs` | 修改 | 启动/退出转发给 `AppController`，异常显示友好错误。 |
| `src/WindowSnapper.App/MainWindow.xaml` | 修改 | MVP 主窗口，绑定 ViewModel 命令。 |
| `src/WindowSnapper.App/MainWindow.xaml.cs` | 修改 | 只初始化并设置 DataContext。 |
| `src/WindowSnapper.App/SettingsWindow.xaml` | 新增 | MVP 设置窗口。 |
| `src/WindowSnapper.App/SettingsWindow.xaml.cs` | 新增 | 只初始化并设置 DataContext。 |
| `src/WindowSnapper.App/Controllers/AppController.cs` | 新增 | App 生命周期、托盘命令、窗口显示、快捷键暂停/恢复、设置保存。 |
| `src/WindowSnapper.App/Commands/RelayCommand.cs` | 新增 | 同步 MVVM 命令。 |
| `src/WindowSnapper.App/Commands/AsyncRelayCommand.cs` | 新增 | 异步 MVVM 命令，捕获异常。 |
| `src/WindowSnapper.App/ViewModels/ViewModelBase.cs` | 新增 | `INotifyPropertyChanged` 基类。 |
| `src/WindowSnapper.App/ViewModels/MainWindowViewModel.cs` | 新增 | 主窗口状态和命令。 |
| `src/WindowSnapper.App/ViewModels/SettingsViewModel.cs` | 新增 | 设置窗口状态、立即保存、错误提示。 |
| `src/WindowSnapper.App/Composition/AppServices.cs` | 修改 | 根据配置创建 WindowFilter/WindowSnapService，设置变更后可应用忽略规则。 |
| `src/WindowSnapper.Storage/AppSettings.cs` | 修改 | 新增 `HotkeysPaused`。 |
| `src/WindowSnapper.Storage/ConfigMigration.cs` | 修改 | 当前配置版本升级到 `2`。 |
| `tests/WindowSnapper.Storage.Tests/SettingsStorageTests.cs` | 修改 | 覆盖 `HotkeysPaused` 读取和默认值。 |

---

## 3. 涉及的类、函数、模块

- `NotifyIconTrayIcon.Show(TrayMenuState)`
- `NotifyIconTrayIcon.UpdateState(TrayMenuState)`
- `TrayMenuCommand`
- `AppController.InitializeAsync()`
- `AppController.ExitApplication()`
- `AppController.SetHotkeysPausedAsync(...)`
- `AppController.SaveSettingsAsync(...)`
- `MainWindowViewModel`
- `SettingsViewModel`
- `AppServices.ApplySettings(AppSettings)`
- `AppSettings.HotkeysPaused`
- `ConfigMigration.CurrentVersion`

---

## 4. 需要重点测试的功能

- [ ] 启动 App 后托盘图标可见，托盘菜单包含：
  - 打开主窗口
  - 设置
  - 暂停快捷键 / 恢复快捷键
  - 退出
- [ ] 默认 `MinimizeToTray=true` 时，启动后可常驻托盘；关闭主窗口不退出进程。
- [ ] 托盘“打开主窗口”能显示并激活主窗口。
- [ ] 托盘“设置”能打开设置窗口，重复点击不会创建多个设置窗口。
- [ ] 托盘和主窗口都能暂停/恢复快捷键，菜单文字同步变化。
- [ ] 设置窗口修改以下字段后保存到 `%APPDATA%/WindowSnapper/config.json`：
  - `minimizeToTray`
  - `showOverlayPreview`
  - `hotkeysPaused`
  - `defaultGap`
  - `defaultMargin`
  - `ignoredProcesses`
  - `ignoredWindowClasses`
- [ ] 配置损坏时，`SettingsStorage.LoadOrCreateAsync` 备份 `.bak` 并恢复默认配置。
- [ ] 点击退出时调用 `HotkeyManager.Dispose()` / registrar 注销，并释放托盘图标。
- [ ] 用户可见错误不暴露原始 Win32 错误文本。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

优先验证配置 schema 变更：

```bash
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
```

再验证相关业务模块：

```bash
dotnet test tests/WindowSnapper.Hotkeys.Tests/WindowSnapper.Hotkeys.Tests.csproj
dotnet test tests/WindowSnapper.Snap.Tests/WindowSnapper.Snap.Tests.csproj
```

最后在 Windows 环境验证 App/Tray 编译：

```bash
dotnet build WindowSnapper.sln
```

必要时全量：

```bash
dotnet test WindowSnapper.sln
```

---

## 6. 不建议自动运行的测试或命令

| 命令/测试 | 原因 |
|---|---|
| 自动化真实托盘点击测试 | 依赖 Windows Explorer/桌面会话，普通单元测试环境不稳定。 |
| 自动化真实 `RegisterHotKey` 冲突测试 | 依赖当前桌面快捷键占用情况。 |
| 自动化真实窗口移动测试 | 依赖前台窗口、显示器和 DPI 状态。 |
| 开机自启动写注册表测试 | 本轮未实现开机自启动，不应修改用户系统启动项。 |
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本轮是 MVP 入口实现，发布留到打包阶段。 |

---

## 7. 已知风险

- 本轮 implement 未运行 `dotnet restore`、`dotnet build`、`dotnet test`，需要 test-agent 验证编译和测试。
- WPF/WinForms 托盘行为需要 Windows 桌面环境人工验证；非 Windows 环境不能完整证明。
- 当前 `StartWithWindows` 仍只存在于配置模型，本轮未实现开机自启动 UI 或注册表/Startup 快捷方式写入。
- 设置窗口保存使用异步 fire-and-forget 触发，但内部已捕获异常并显示友好错误；仍需验证快速连续修改时最终配置符合最后一次输入。
- `ignoredProcesses` / `ignoredWindowClasses` 已保存并应用到新建的 `WindowSnapService`，但当前进程名匹配仍沿用已有 `WindowFilter` 规则。

---

## 8. 需要人工验证的部分

- [ ] Windows 桌面启动 App，确认托盘图标出现。
- [ ] 默认配置下启动后主窗口是否符合预期隐藏/托盘常驻。
- [ ] 托盘菜单“打开主窗口”“设置”“暂停快捷键/恢复快捷键”“退出”逐项可用。
- [ ] 关闭主窗口后进程仍在，托盘菜单仍可打开主窗口。
- [ ] `minimizeToTray=false` 时关闭主窗口会退出。
- [ ] 设置窗口修改字段后重启 App，确认配置被持久化。
- [ ] 人为写坏 `config.json` 后启动，确认生成 `.bak` 并恢复默认配置。
- [ ] 退出后托盘图标消失，全局快捷键释放。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 已执行静态检查：
  - `rg "DllImport|LibraryImport" -n src tests`，确认 P/Invoke 仍集中在 Win32 `NativeMethods.cs`。
  - `rg "System\\.Windows|WindowInterop|MessageBox" ...`，确认 WPF 引用未进入 Core、Layouts、Storage、Hotkeys、Win32、Snap；Tray 仅使用 WinForms。
  - `rg "File\\.|Directory\\.|StoragePaths|SettingsStorage|AppSettings" -n src/WindowSnapper.Tray src/WindowSnapper.App`，确认配置读写只在 App，不在 Tray。
  - `git diff --check`，未发现空白错误。
- 未添加网络、遥测、账号系统、云同步、管理员权限提升、复杂布局编辑器或开机自启动写入逻辑。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
