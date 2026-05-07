# AGENTS.md — Codex 编程指令

本文件用于指导 Codex 在本仓库中开发一个 **Windows 自定义窗口分屏工具**。

项目目标：构建一个 Windows 桌面工具，允许用户通过快捷键、托盘菜单和自定义布局，将当前窗口快速移动到指定屏幕区域，并支持多显示器、DPI 缩放和 JSON 布局配置。

Codex 在本仓库中修改代码时，必须优先遵守本文件。

---

## 1. 项目定位

这是一个 Windows 桌面效率工具，不是完整桌面环境，也不是虚拟桌面管理器。

核心功能：

- 获取当前活动窗口。
- 获取当前窗口所在显示器。
- 根据布局规则计算目标区域。
- 将窗口移动到目标区域。
- 支持全局快捷键。
- 支持系统托盘常驻。
- 支持 JSON 自定义布局。
- 支持多显示器和不同 DPI 缩放。

优先级：

```text
稳定性 > 正确性 > 隐私安全 > 可测试性 > 用户体验 > 功能数量
```

---

## 2. 推荐技术栈

默认使用：

```text
C#
.NET 8
WPF
Win32 API P/Invoke
JSON 配置文件
xUnit 或 NUnit
```

除非用户明确要求，否则不要改成：

- Electron
- Tauri
- Avalonia
- WinUI 3
- C++
- Rust
- Python

原因：本项目需要深度调用 Windows 桌面窗口管理能力，C# + WPF + Win32 API 更适合快速实现 MVP。

---

## 3. 建议仓库结构

如果仓库尚未初始化，请按以下结构创建：

```text
WindowSnapper/
├─ src/
│  ├─ WindowSnapper.App/              # WPF 主程序
│  ├─ WindowSnapper.Core/             # 核心模型和业务接口
│  ├─ WindowSnapper.Win32/            # Win32 API 封装
│  ├─ WindowSnapper.Layouts/          # 布局模型与计算
│  ├─ WindowSnapper.Hotkeys/          # 全局快捷键
│  ├─ WindowSnapper.Storage/          # 配置读写
│  └─ WindowSnapper.Tray/             # 系统托盘
│
├─ tests/
│  ├─ WindowSnapper.Layouts.Tests/
│  ├─ WindowSnapper.Storage.Tests/
│  └─ WindowSnapper.Core.Tests/
│
├─ docs/
│  ├─ architecture.md
│  ├─ layout-schema.md
│  └─ window-management.md
│
├─ assets/
│  └─ icons/
│
├─ AGENTS.md
├─ README.md
├─ CHANGELOG.md
└─ WindowSnapper.sln
```

如果仓库已经存在，请优先遵循现有结构，不要为了匹配上面的结构而大规模移动文件。

---

## 4. Codex 工作原则

Codex 每次执行任务时必须遵守：

1. 先阅读相关文件，再修改。
2. 尽量做最小改动。
3. 不要无故重写整个模块。
4. 修改后保持项目可编译。
5. 涉及逻辑变更时补充或更新测试。
6. 不删除已有测试，除非测试明显错误。
7. 不引入大型依赖，除非任务明确要求。
8. 不默认添加网络请求、遥测、账号系统或云同步。
9. 不默认请求管理员权限。
10. 不把 Win32 P/Invoke 代码写进 UI 层。
11. 不把布局计算逻辑写死在按钮事件里。
12. 不把用户配置写死在代码中。
13. 不提交临时调试代码。
14. 不提交本地绝对路径。
15. 不修改与任务无关的大量文件。

---

## 5. 构建与测试命令

优先使用以下命令：

```bash
dotnet restore
```

```bash
dotnet build
```

```bash
dotnet test
```

如果解决方案文件存在：

```bash
dotnet build WindowSnapper.sln
```

```bash
dotnet test WindowSnapper.sln
```

如果只有某个测试项目需要运行：

```bash
dotnet test tests/WindowSnapper.Layouts.Tests/WindowSnapper.Layouts.Tests.csproj
```

发布前可使用：

```bash
dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release
```

如果当前环境不是 Windows，涉及 WPF 或 Win32 的构建可能无法完整运行。此时 Codex 应说明限制，并至少运行可运行的纯逻辑测试，例如 Layouts、Core、Storage 测试。

---

## 6. 分层架构要求

项目必须保持以下依赖方向：

```text
WindowSnapper.App
  -> WindowSnapper.Core
  -> WindowSnapper.Layouts
  -> WindowSnapper.Storage
  -> WindowSnapper.Hotkeys
  -> WindowSnapper.Tray
  -> WindowSnapper.Win32

WindowSnapper.Layouts
  -> WindowSnapper.Core

WindowSnapper.Storage
  -> WindowSnapper.Core
  -> WindowSnapper.Layouts

WindowSnapper.Win32
  -> WindowSnapper.Core

WindowSnapper.Core
  -> 不依赖 UI
  -> 不依赖 Win32
  -> 不依赖 WPF
```

禁止：

```text
Core -> App
Core -> WPF
Core -> Win32
Layouts -> WPF
Layouts -> Win32
Storage -> WPF
```

---

## 7. 模块职责

### 7.1 WindowSnapper.Core

负责纯模型、接口和通用类型。

可包含：

- `WindowHandle`
- `WindowInfo`
- `MonitorInfo`
- `RectInt`
- `PointInt`
- `Result<T>`
- `IWindowManager`
- `IMonitorManager`
- `IClock`
- `ILogger` 抽象，或使用 Microsoft.Extensions.Logging 抽象

不得包含：

- WPF 类型
- Win32 P/Invoke
- JSON 文件读写细节
- 托盘逻辑
- UI 事件处理

---

### 7.2 WindowSnapper.Win32

负责 Windows API 封装。

可包含：

- `NativeMethods`
- `Win32WindowManager`
- `Win32MonitorManager`
- `Win32HotkeyRegistrar`
- Win32 结构体映射
- P/Invoke 声明

要求：

- 所有 P/Invoke 必须集中放在本项目中。
- Win32 调用失败时必须返回明确错误。
- 不要静默吞掉错误。
- 封装层对外暴露业务友好的类型，不要让 UI 层直接处理原生结构体。

常用 API：

```text
GetForegroundWindow
GetWindowRect
SetWindowPos
MoveWindow
EnumWindows
IsWindowVisible
IsIconic
ShowWindow
GetWindowText
GetClassName
GetWindowThreadProcessId
MonitorFromWindow
MonitorFromPoint
GetMonitorInfo
RegisterHotKey
UnregisterHotKey
DwmGetWindowAttribute
GetDpiForWindow
```

注意：

- 普通权限程序可能无法稳定控制管理员权限窗口。
- UAC 安全桌面不能管理。
- 全屏游戏、特殊渲染窗口和部分系统窗口应默认忽略。

---

### 7.3 WindowSnapper.Layouts

负责布局模型、布局计算和内置布局。

可包含：

- `LayoutDefinition`
- `ZoneDefinition`
- `ZoneRect`
- `LayoutEngine`
- `BuiltinLayouts`
- `LayoutValidator`

布局计算必须是纯逻辑，方便单元测试。

不得依赖：

- WPF
- Win32
- 文件系统
- 当前真实显示器

---

### 7.4 WindowSnapper.Hotkeys

负责快捷键模型、注册、注销和命令分发。

可包含：

- `HotkeyDefinition`
- `HotkeyManager`
- `HotkeyCommand`
- `HotkeyPressedEventArgs`

要求：

- 快捷键注册失败不能导致程序崩溃。
- 退出程序时必须注销快捷键。
- 快捷键冲突要返回可理解的错误信息。

---

### 7.5 WindowSnapper.Storage

负责配置文件读写。

默认路径：

```text
%APPDATA%/WindowSnapper/config.json
%APPDATA%/WindowSnapper/layouts/*.json
%LOCALAPPDATA%/WindowSnapper/logs/app.log
```

要求：

- 配置不存在时创建默认配置。
- 配置损坏时备份损坏文件，再恢复默认配置。
- 写配置时使用原子写入。
- 配置 schema 变化时提升 version 并提供迁移逻辑。

---

### 7.6 WindowSnapper.Tray

负责系统托盘图标与托盘菜单。

可包含：

- 打开主窗口
- 打开设置窗口
- 切换布局
- 暂停快捷键
- 退出程序

不得包含：

- 布局计算
- Win32 P/Invoke
- 配置解析细节

---

### 7.7 WindowSnapper.App

负责 WPF 启动、依赖注入、主窗口、设置窗口和用户交互。

要求：

- UI 层只调用服务，不直接调用 Win32 API。
- UI 事件里不要写复杂业务逻辑。
- 不在 UI 线程执行耗时操作。
- 用户可见错误要友好。

---

## 8. 核心数据模型建议

### 8.1 RectInt

```csharp
public readonly record struct RectInt(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
}
```

### 8.2 MonitorInfo

```csharp
public sealed record MonitorInfo(
    string Id,
    string DeviceName,
    RectInt Bounds,
    RectInt WorkArea,
    bool IsPrimary,
    double DpiScale
);
```

### 8.3 WindowInfo

```csharp
public sealed record WindowInfo(
    WindowHandle Handle,
    string Title,
    string ProcessName,
    string ClassName,
    RectInt Bounds,
    bool IsVisible,
    bool IsMinimized,
    bool IsMaximized
);
```

### 8.4 LayoutDefinition

```csharp
public sealed record LayoutDefinition(
    string Id,
    string Name,
    int Version,
    int Gap,
    int Margin,
    IReadOnlyList<ZoneDefinition> Zones
);
```

### 8.5 ZoneDefinition

```csharp
public sealed record ZoneDefinition(
    string Id,
    string Name,
    double X,
    double Y,
    double Width,
    double Height
);
```

---

## 9. 布局 JSON 格式

布局文件示例：

```json
{
  "id": "dev-layout",
  "name": "开发布局",
  "version": 1,
  "gap": 8,
  "margin": 8,
  "zones": [
    {
      "id": "code",
      "name": "代码区",
      "x": 0,
      "y": 0,
      "width": 0.6,
      "height": 1
    },
    {
      "id": "browser",
      "name": "浏览器区",
      "x": 0.6,
      "y": 0,
      "width": 0.4,
      "height": 0.5
    },
    {
      "id": "terminal",
      "name": "终端区",
      "x": 0.6,
      "y": 0.5,
      "width": 0.4,
      "height": 0.5
    }
  ]
}
```

坐标规则：

```text
x: 0.0 到 1.0
y: 0.0 到 1.0
width: 0.0 到 1.0
height: 0.0 到 1.0
```

校验规则：

```text
id 不为空
name 不为空
width > 0
height > 0
x >= 0
y >= 0
x + width <= 1
y + height <= 1
gap >= 0
margin >= 0
zones 至少包含 1 个区域
```

---

## 10. 窗口移动流程

实现窗口移动时，必须遵循此流程：

```text
1. 获取当前活动窗口
2. 判断窗口是否可管理
3. 获取窗口当前状态
4. 获取窗口所在显示器
5. 获取显示器 WorkArea，不要覆盖任务栏
6. 根据 LayoutEngine 计算目标 Rect
7. 如果窗口最大化，先 Restore
8. 使用 SetWindowPos 移动窗口
9. 检查返回值
10. 失败时记录错误并返回 Result
```

不要直接假设：

- 显示器坐标从 0,0 开始。
- 所有窗口都能调整大小。
- 所有窗口都有标题。
- 所有窗口都属于普通应用。
- DPI 永远是 100%。

---

## 11. 可管理窗口规则

默认只管理普通桌面应用窗口。

必须忽略：

```text
Shell_TrayWnd
Progman
WorkerW
DV2ControlHost
MsgrIMEWindowClass
本程序主窗口
本程序设置窗口
本程序 Overlay 窗口
```

通常应忽略：

```text
全屏游戏窗口
UAC 安全桌面窗口
任务栏
桌面
开始菜单
系统弹窗
无尺寸窗口
不可见窗口
最小化窗口
```

管理员权限窗口：

- 普通权限下可能无法移动。
- 不要自动提升权限。
- 失败时返回提示，不要崩溃。

---

## 12. 快捷键要求

默认快捷键建议：

```text
Ctrl + Alt + Left      左半屏
Ctrl + Alt + Right     右半屏
Ctrl + Alt + Up        上半屏
Ctrl + Alt + Down      下半屏
Ctrl + Alt + 1         区域 1
Ctrl + Alt + 2         区域 2
Ctrl + Alt + 3         区域 3
Ctrl + Alt + 4         区域 4
Ctrl + Alt + Space     打开布局选择器
```

Codex 实现快捷键时必须保证：

- 支持注册失败。
- 支持注销。
- 支持快捷键冲突提示。
- 应用退出时释放快捷键。
- 不要把快捷键硬编码死，默认值可以硬编码，但最终要支持配置文件覆盖。

---

## 13. 内置布局要求

MVP 至少提供：

```text
left-half
right-half
top-half
bottom-half
quad-top-left
quad-top-right
quad-bottom-left
quad-bottom-right
left-two-thirds
right-one-third
left-one-third
right-two-thirds
```

内置布局 id 一旦发布，不要随意修改，否则会破坏用户配置。

---

## 14. 配置文件要求

全局配置示例：

```json
{
  "version": 1,
  "theme": "system",
  "language": "zh-CN",
  "startWithWindows": false,
  "minimizeToTray": true,
  "showOverlayPreview": true,
  "overlayOpacity": 0.35,
  "defaultGap": 8,
  "defaultMargin": 8,
  "ignoredProcesses": [
    "explorer.exe",
    "ApplicationFrameHost.exe"
  ],
  "ignoredWindowClasses": [
    "Shell_TrayWnd",
    "Progman",
    "WorkerW"
  ]
}
```

要求：

- 不存在时自动创建。
- 解析失败时备份为 `.bak`。
- 写入时先写临时文件，再替换目标文件。
- 新增字段必须有默认值。
- schema 变化必须升级 version。

---

## 15. 隐私与安全限制

默认禁止：

- 上传用户数据。
- 收集遥测。
- 记录完整窗口标题历史。
- 记录浏览器 URL。
- 记录用户文件路径。
- 自动启动网络请求。
- 自动申请管理员权限。
- 注入其他进程。
- 修改其他应用程序文件。

允许本地保存：

- 布局配置。
- 快捷键配置。
- 忽略窗口规则。
- 用户设置。
- 必要错误日志。

日志中不要默认记录完整窗口标题。调试模式除外。

---

## 16. 测试要求

### 16.1 必须优先测试的模块

优先为以下模块写单元测试：

- LayoutEngine
- LayoutValidator
- BuiltinLayouts
- SettingsStorage
- ConfigMigration
- HotkeyDefinition parser

### 16.2 LayoutEngine 测试场景

至少覆盖：

```text
1920x1080 工作区下左半屏计算
1920x1080 工作区下右半屏计算
带 margin 的计算
带 gap 的计算
副屏坐标为负数时的计算
竖屏显示器计算
区域 x + width 超过 1 时校验失败
区域 y + height 超过 1 时校验失败
```

### 16.3 Storage 测试场景

至少覆盖：

```text
配置不存在时创建默认配置
配置合法时正常读取
配置损坏时备份并恢复默认配置
新增字段时使用默认值
配置 version 迁移
```

### 16.4 Win32 相关测试

Win32 相关代码可以较少做单元测试，但应保持封装薄、职责清晰。

能测试的部分包括：

- 窗口过滤规则
- class name 忽略规则
- Result 错误映射
- monitor 选择逻辑

不要在单元测试中强依赖真实桌面窗口状态。

---

## 17. 编码规范

### 17.1 C# 规范

要求：

- 启用 nullable reference types。
- 优先使用 `record` 或 `record struct` 表示不可变模型。
- 公共 API 命名清晰。
- 不要返回 magic string 表示错误。
- 可以恢复的失败使用 `Result<T>` 或明确异常类型。
- 不要在业务层直接调用 `MessageBox.Show`。
- 不要吞异常。
- 不要使用空 catch。

### 17.2 异步规范

- 不要在 UI 线程执行耗时文件 IO。
- 异步方法以 `Async` 结尾。
- 避免 fire-and-forget。
- 如果必须 fire-and-forget，必须捕获并记录异常。

### 17.3 日志规范

记录：

- 程序启动和退出。
- 快捷键注册成功或失败。
- 配置加载失败。
- 布局文件校验失败。
- 窗口移动失败及错误码。

不要默认记录：

- 完整窗口标题。
- 浏览器网址。
- 用户文件路径。
- 用户输入内容。

---

## 18. UI 编程要求

WPF UI 要求：

- 使用 MVVM 或接近 MVVM 的结构。
- 不要把业务逻辑塞进 code-behind。
- code-behind 只处理 UI 事件转发。
- 设置项修改后要保存到配置服务。
- 错误提示要友好，不暴露原始 Win32 错误给普通用户。

用户提示示例：

```text
无法移动该窗口。它可能是系统窗口、管理员权限窗口，或当前不允许调整大小。
```

---

## 19. 开机自启动要求

默认不要开启开机自启动。

用户开启后可以通过以下方式实现：

- 注册表 Run 项
- Startup 文件夹快捷方式
- MSIX 启动任务

MVP 推荐使用注册表 Run 项或 Startup 快捷方式。

要求：

- 必须可关闭。
- 不需要管理员权限。
- 不要创建计划任务，除非用户明确要求。

---

## 20. 多显示器与 DPI 要求

必须处理：

- 主屏。
- 副屏。
- 副屏在主屏左侧导致负坐标。
- 横屏和竖屏混用。
- 不同 DPI 缩放。
- 任务栏不在底部。

计算目标窗口区域时，必须基于 `MonitorInfo.WorkArea`，不是 `Bounds`。

---

## 21. 常见任务实现方式

### 21.1 添加新内置布局

步骤：

```text
1. 修改 BuiltinLayouts
2. 添加稳定 layout id 和 zone id
3. 添加 LayoutEngine 测试
4. 更新 README 或 docs/layout-schema.md
```

不要修改已有布局 id。

---

### 21.2 添加快捷键命令

步骤：

```text
1. 添加命令 id
2. 更新 HotkeyDefinition
3. 更新 HotkeyManager 注册逻辑
4. 更新命令分发逻辑
5. 添加冲突处理
6. 添加测试
```

---

### 21.3 修改窗口移动逻辑

步骤：

```text
1. 阅读 IWindowManager 和 Win32WindowManager
2. 确认是否影响最大化窗口
3. 确认是否影响 DPI 坐标
4. 修改 SetWindowPos 相关逻辑
5. 处理失败返回值
6. 更新窗口过滤测试
7. 手动说明需要在 Windows 环境验证
```

---

### 21.4 新增配置字段

步骤：

```text
1. 修改 Settings 模型
2. 添加默认值
3. 添加迁移逻辑
4. 更新配置测试
5. 更新配置文档
```

---

## 22. 禁止行为

Codex 不得执行以下行为：

```text
删除 AGENTS.md
删除测试项目
绕过测试失败
隐藏构建错误
自动添加联网功能
自动添加数据上传
自动申请管理员权限
注入其他进程
强制管理 UAC 窗口
强制管理全屏游戏窗口
把用户窗口标题上传或长期保存
把 Win32 P/Invoke 写到 WPF 窗口类中
把布局计算写进按钮点击事件中
为了简单而破坏分层架构
```

---

## 23. 提交前检查清单

Codex 完成任务前应尽量检查：

```text
dotnet restore
dotnet build
dotnet test
```

同时确认：

- 没有无关文件大改。
- 没有临时代码。
- 没有本地绝对路径。
- 没有敏感信息。
- 没有新增不必要依赖。
- 布局计算有测试。
- 配置变更有迁移。
- Win32 失败路径有处理。
- UI 层没有直接 P/Invoke。

如果某些命令无法运行，Codex 必须在最终回复中说明原因。

---

## 24. MVP 开发顺序

推荐 Codex 按此顺序实现：

```text
1. 创建解决方案和项目结构
2. 添加 Core 基础模型
3. 添加 Layouts 模块和单元测试
4. 添加 Storage 配置读写
5. 添加 Win32 窗口和显示器封装
6. 添加 Hotkeys 快捷键模块
7. 添加 WPF App 和托盘入口
8. 打通快捷键移动当前窗口
9. 添加内置布局
10. 添加设置界面
11. 添加 README 和发布说明
```

不要一开始就实现复杂布局编辑器。先完成可用 MVP。

---

## 25. 最终目标

本仓库的目标不是做一个功能堆满的窗口工具，而是先实现一个稳定、清晰、可测试的 Windows 分屏工具 MVP。

Codex 在每次修改时都应保持这个方向：

```text
少做一点，但做稳。
先让左半屏、右半屏、四宫格和快捷键稳定工作。
再扩展布局编辑器、多窗口工作区恢复和高级吸附功能。
```
## Implementation/Test Separation Workflow

本仓库支持将「代码实现」和「测试验证」拆分为两个阶段。

### 1. implement-agent

当用户要求实现或修改代码，并希望把测试验证留到后续阶段时，使用 `implement-agent`。

implement-agent 的职责：

1. 阅读 `AGENTS.md` 和相关源码。
2. 完成最小必要代码修改。
3. 保持项目架构边界。
4. 不做无关重构。
5. 可运行轻量检查，但不进行长时间测试循环。
6. 在结束前更新 `.agent_handoff/TEST_PLAN.md`。

implement-agent 不应：

1. 反复运行全量测试并长时间调试。
2. 为了测试方便破坏架构。
3. 删除已有测试。
4. 隐藏构建错误。
5. 写虚假的测试状态。

### 2. test-agent

当用户要求测试、验证或调试 implement-agent 的改动时，使用 `test-agent`。

test-agent 的职责：

1. 必须先读取 `.agent_handoff/TEST_PLAN.md`。
2. 根据交接文档制定测试计划。
3. 优先运行最小相关测试。
4. 再逐步扩大到相关测试项目或全量测试。
5. 精准记录失败命令和错误原因。
6. 只在原因明确且修复范围很小时修改代码。
7. 在结束前把测试结果追加到 `.agent_handoff/TEST_PLAN.md`。

test-agent 不应：

1. 不读交接文档就直接测试。
2. 做大范围业务重构。
3. 删除失败测试。
4. 隐藏错误输出。
5. 在非 Windows 环境下声称已经验证真实 Win32/WPF/托盘行为。

### 3. 推荐调用方式

实现阶段：

```text
$implement-agent
请实现 XXX 功能。完成后更新 .agent_handoff/TEST_PLAN.md，不要进行长时间测试循环。
```

测试阶段：

```text
$test-agent
请先读取 .agent_handoff/TEST_PLAN.md，然后按其中的测试计划进行验证。
```

### 4. 固定交接文件

两个阶段通过以下文件交接：

```text
.agent_handoff/TEST_PLAN.md
```

该文件必须记录：

1. 本次实现摘要。
2. 修改文件列表。
3. 涉及模块。
4. 需要重点测试的功能。
5. 推荐测试命令。
6. 不建议自动运行的命令。
7. 已知风险。
8. 需要人工验证的部分。
9. test-agent 的测试结果。
