# WindowSnapper

WindowSnapper 是一个 Windows 桌面分屏工具。当前 MVP 支持通过全局快捷键、系统托盘菜单和窗口选择器，将当前活动窗口或手动选择的窗口移动到内置布局或用户自定义布局的指定区域。

项目重点是稳定的窗口移动闭环：获取活动窗口、判断是否可管理、读取窗口所在显示器的 `WorkArea`、计算目标区域、显示可选 Overlay Preview，然后通过 Win32 API 移动窗口。

## 功能列表

已实现：

- 全局快捷键注册、注销、冲突检测和命令分发。
- 当前活动窗口移动到左半屏、右半屏、上半屏、下半屏和四宫格区域。
- 重复快捷键循环，例如短时间内连续按 `Ctrl+Alt+Right` 会在右半屏、右三分之一、右三分之二和右侧四宫格区域之间循环。
- 系统托盘常驻，支持打开主窗口、打开设置、暂停/恢复快捷键、退出程序。
- 托盘布局菜单，显示内置布局和已加载的用户自定义布局。
- 窗口选择器，支持枚举可管理窗口、选择目标窗口、批量移动、按布局区域名称排序、手动指定窗口到某个区域。
- 移动前快照和还原，支持把窗口还原到移动前位置；移动前为最小化或隐藏状态的窗口，还原后会恢复对应状态。
- MVP 可视化布局编辑器，支持创建和保存自定义比例布局。
- JSON 配置读写，配置损坏时备份为 `.bak` 并恢复默认配置。
- 从 `%APPDATA%/WindowSnapper/layouts/*.json` 加载用户自定义布局。
- 自定义布局校验；非法布局跳过并记录文件名级别的错误。
- 工作区快照，支持保存当前可管理窗口位置并恢复最近快照。
- 窗口移动前 Overlay Preview，支持通过配置关闭，默认透明度为 `0.35`。
- 基于显示器 `WorkArea` 计算目标区域，避免覆盖任务栏。
- 支持负坐标显示器和竖屏显示器的纯逻辑计算。
- Win32 层对 DWM 可见边框做补偿，减少 gap/margin 为 `0` 时由不可见 resize border 造成的视觉空隙。

Roadmap：

- 快捷键配置文件覆盖。
- `Ctrl+Alt+Space` 直接打开窗口选择器或布局选择器的完整交互。
- 开机自启动的实际启用/关闭逻辑。
- 更完整的布局编辑器，例如吸附、撤销/重做、打开已有布局继续编辑。
- 更完整的日志落盘。
- Windows 多 DPI 场景下的人工验证和细节修正。

## 技术栈

- C# / .NET 8
- WPF
- Windows Forms `NotifyIcon`
- Win32 API P/Invoke
- `System.Text.Json`
- xUnit

## MVP 使用方式

1. 在 Windows 上构建并运行 `WindowSnapper.App`。
2. 程序启动后默认常驻系统托盘。
3. 使用默认快捷键移动当前活动窗口。
4. 打开主窗口中的“窗口选择器”，可以从当前可管理窗口列表中选择一个或多个窗口并移动到指定区域。
5. 右键托盘图标可以打开主窗口、设置窗口、暂停/恢复快捷键、选择布局区域、保存/恢复工作区快照或退出。
6. 自定义布局可通过布局编辑器保存，也可手写 JSON 放到 `%APPDATA%/WindowSnapper/layouts` 目录。

更完整的使用说明见 [docs/user-guide.md](docs/user-guide.md)。

设置窗口当前支持：

- `minimizeToTray`
- `showOverlayPreview`
- `defaultGap`
- `defaultMargin`
- `ignoredProcesses`
- `ignoredWindowClasses`
- 暂停/恢复快捷键

`overlayOpacity` 已在配置模型中支持，当前设置窗口尚未提供编辑控件。

## 默认快捷键

| 快捷键 | 当前行为 |
| --- | --- |
| `Ctrl+Alt+Left` | 移动到左半屏 |
| `Ctrl+Alt+Right` | 移动到右半屏 |
| `Ctrl+Alt+Up` | 移动到上半屏 |
| `Ctrl+Alt+Down` | 移动到下半屏 |
| `Ctrl+Alt+1` | 移动到左上四分区 |
| `Ctrl+Alt+2` | 移动到右上四分区 |
| `Ctrl+Alt+3` | 移动到左下四分区 |
| `Ctrl+Alt+4` | 移动到右下四分区 |
| `Ctrl+Alt+Space` | 已注册，完整选择器热键入口仍在 Roadmap |

## 配置文件位置

默认本地路径：

```text
%APPDATA%/WindowSnapper/config.json
%APPDATA%/WindowSnapper/layouts/*.json
%APPDATA%/WindowSnapper/workspaces/*.json
%LOCALAPPDATA%/WindowSnapper/logs/app.log
```

当前配置 schema 版本为 `3`。读取配置时会补齐新增字段默认值，并保留必要的默认忽略窗口类名。版本 3 将默认 `defaultGap` 和 `defaultMargin` 调整为 `0`，默认行为更接近密铺。

配置示例：

```json
{
  "version": 3,
  "theme": "system",
  "language": "zh-CN",
  "startWithWindows": false,
  "minimizeToTray": true,
  "showOverlayPreview": true,
  "hotkeysPaused": false,
  "overlayOpacity": 0.35,
  "defaultGap": 0,
  "defaultMargin": 0,
  "ignoredProcesses": [
    "explorer.exe",
    "ApplicationFrameHost.exe"
  ],
  "ignoredWindowClasses": [
    "Shell_TrayWnd",
    "Progman",
    "WorkerW",
    "WindowSnapperOverlayWindow"
  ]
}
```

## 安全和隐私

- 不包含网络请求、遥测、账号系统或云同步。
- 不自动申请管理员权限。
- 不注入其他进程。
- 不尝试控制 UAC 安全桌面、全屏游戏或系统窗口。
- 日志和布局加载错误不应记录完整窗口标题、浏览器 URL 或用户输入内容。
- 用户自定义布局和设置只保存在本机配置目录。
- 工作区快照默认只保存进程名、窗口类名、显示器名、相对位置和窗口状态，不保存完整窗口标题、浏览器 URL、用户文件路径或命令行参数。

## 构建命令

安装 .NET 8 SDK 后运行：

```bash
dotnet restore
dotnet build WindowSnapper.sln
```

发布 WPF 主程序：

```bash
dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release
```

## 测试命令

运行全部测试：

```bash
dotnet test WindowSnapper.sln
```

运行单个测试项目：

```bash
dotnet test tests/WindowSnapper.Layouts.Tests/WindowSnapper.Layouts.Tests.csproj
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
dotnet test tests/WindowSnapper.Snap.Tests/WindowSnapper.Snap.Tests.csproj
dotnet test tests/WindowSnapper.App.Tests/WindowSnapper.App.Tests.csproj
dotnet test tests/WindowSnapper.Workspaces.Tests/WindowSnapper.Workspaces.Tests.csproj
```

在非 Windows 环境中，WPF、托盘和真实 Win32 行为可能无法完整验证；优先运行 Core、Layouts、Storage、Hotkeys、Snap 中的纯逻辑测试。

## 当前限制

- 真实窗口移动、全局快捷键、托盘和 Overlay Preview 需要 Windows 桌面环境验证。
- 普通权限进程通常不能稳定移动管理员权限窗口。
- UAC 安全桌面、全屏游戏、任务栏、桌面和部分系统窗口默认不管理。
- 默认快捷键目前硬编码，尚未从配置文件覆盖。
- `startWithWindows` 配置字段存在，但实际开机自启动逻辑尚未实现。
- `Ctrl+Alt+Space` 已注册，但还没有完整绑定到窗口选择器/布局选择器体验。
- 布局编辑器是 MVP，不支持吸附、撤销/重做、打开已有布局继续编辑。
- 工作区快照的窗口匹配规则有限，同进程同类名的多个窗口可能无法精确区分。
- 隐藏到托盘且不暴露普通顶层窗口句柄的程序，可能无法出现在窗口选择器中。
