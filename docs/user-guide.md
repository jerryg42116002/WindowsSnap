# WindowSnapper 使用文档

本文档面向普通使用者，说明如何安装环境、启动 WindowSnapper、使用快捷键和托盘菜单、创建自定义布局，以及排查常见问题。

## 1. 软件简介

WindowSnapper 是一个 Windows 桌面分屏工具。它可以把当前活动窗口移动到指定屏幕区域，例如左半屏、右半屏、四宫格区域，或用户自定义的任意比例布局区域。

当前项目重点是本地、轻量、可控：

- 不需要账号。
- 不需要联网。
- 不上传数据。
- 不自动申请管理员权限。
- 不注入其他进程。
- 配置和布局保存在本机。

## 2. 环境要求

### 2.1 操作系统

推荐：

```text
Windows 10 / Windows 11
```

说明：

- WPF、系统托盘、全局快捷键和 Win32 窗口移动能力需要 Windows 桌面环境。
- WSL、Linux、macOS 不能完整运行本程序的桌面功能。

### 2.2 .NET SDK

如果你从源码运行，需要安装：

```text
.NET 8 SDK - Windows x64
```

下载地址：

```text
https://dotnet.microsoft.com/download/dotnet/8.0
```

安装后打开新的 PowerShell，验证：

```powershell
dotnet --version
```

能显示 `8.x.x` 即可。

## 3. 获取和启动

进入项目根目录，例如：

```powershell
cd D:\2026-5-6
```

还原依赖：

```powershell
dotnet restore
```

构建解决方案：

```powershell
dotnet build WindowSnapper.sln
```

运行主程序：

```powershell
dotnet run --project src\WindowSnapper.App\WindowSnapper.App.csproj
```

启动后，程序默认常驻系统托盘。托盘图标使用系统默认应用图标。

## 4. 首次使用流程

1. 启动程序。
2. 查看右下角系统托盘是否出现 WindowSnapper 图标。
3. 打开一个普通桌面窗口，例如记事本、资源管理器或浏览器。
4. 让该窗口成为当前活动窗口。
5. 按默认快捷键，例如：

```text
Ctrl + Alt + Left
```

如果快捷键注册成功，当前窗口会移动到左半屏。

如果提示“全局快捷键注册失败”，参考本文档的“快捷键冲突排查”章节。

## 5. 默认快捷键

| 快捷键 | 行为 |
|---|---|
| `Ctrl + Alt + Left` | 移动到左半屏 |
| `Ctrl + Alt + Right` | 移动到右半屏 |
| `Ctrl + Alt + Up` | 移动到上半屏 |
| `Ctrl + Alt + Down` | 移动到下半屏 |
| `Ctrl + Alt + 1` | 移动到左上四分区 |
| `Ctrl + Alt + 2` | 移动到右上四分区 |
| `Ctrl + Alt + 3` | 移动到左下四分区 |
| `Ctrl + Alt + 4` | 移动到右下四分区 |
| `Ctrl + Alt + Space` | 已注册；完整选择器热键入口仍在 Roadmap |

当前版本默认快捷键仍是硬编码，暂不支持在设置界面中修改快捷键。

## 6. Repeat Hotkey Cycle

当前项目已实现重复快捷键循环功能。以 `Ctrl + Alt + Right` 为例：

```text
第一次：right-half
1.5 秒内第二次：right-one-third
再次：right-two-thirds
再次：quad-top-right
再次：quad-bottom-right
超过 1.5 秒后：重新从 right-half 开始
```

注意：

- 该功能是内存状态，不持久化。
- 换窗口、换命令或超过重置时间后，会重新从第一项开始。
- 如果测试验证尚未完成，请以实际运行为准。

## 7. 托盘菜单

右键系统托盘图标，可以看到常用操作。

当前菜单包含：

- 打开主窗口
- 设置
- 布局
- 保存工作区快照
- 恢复最近工作区快照
- 暂停快捷键 / 恢复快捷键
- 退出

### 7.1 打开主窗口

显示 WindowSnapper 主窗口。主窗口当前提供：

- 快捷键状态显示
- 暂停 / 恢复快捷键
- 打开布局编辑器
- 打开设置
- 退出

### 7.2 布局菜单

托盘中的“布局”菜单会列出：

- 内置布局
- 已加载的用户自定义布局

点击某个布局区域后，WindowSnapper 会把当前活动窗口移动到该区域。

### 7.3 暂停 / 恢复快捷键

暂停快捷键后，全局快捷键不会触发窗口移动。

恢复快捷键时，如果快捷键被其他程序占用，会显示注册失败提示。

### 7.4 退出

退出时会：

- 注销已注册快捷键。
- 释放托盘图标。
- 关闭应用。

## 8. 窗口选择器

窗口选择器用于手动选择要移动的窗口。它适合以下场景：

- 不想切换活动窗口，只想从列表中选择窗口。
- 一次性移动多个窗口。
- 把某个窗口固定到指定布局区域。
- 移动后需要还原到移动前的位置和状态。

打开方式：

1. 打开主窗口。
2. 点击“窗口选择器”。

### 8.1 选择单个窗口

1. 在窗口列表中选中一个窗口。
2. 选择布局。
3. 选择区域。
4. 点击“移动窗口”。

如果该窗口原本是最小化状态，WindowSnapper 会先恢复窗口，再移动到目标区域。

### 8.2 一次选择多个窗口

窗口列表支持多选：

- 按住 `Ctrl` 点击多个窗口，可以逐个加入选择。
- 按住 `Shift` 点击，可以选择连续的一段窗口。

多选后选择一个“起始区域”，再点击“移动窗口”。规则是：

```text
从你选的第 N 个区域开始，
第 1 个选中的窗口放到第 N 个区域，
第 2 个选中的窗口放到第 N+1 个区域，
第 3 个选中的窗口放到第 N+2 个区域，
依次类推。
```

区域顺序按区域名称排序，再按区域 id 排序。如果从起始区域开始剩余区域不够放下所有选中的窗口，移动会失败并显示提示。

### 8.3 手动“选定”窗口目标

“选定”用于明确指定某一个窗口应该移动到哪个区域。

使用方式：

1. 只选中一个窗口。
2. 选择布局。
3. 选择区域。
4. 点击“选定”。

窗口列表会显示该窗口的目标区域。可以对多个窗口重复这个过程，让每个窗口有自己的固定目标。

点击“移动窗口”时，如果列表中存在已选定固定目标的窗口，会优先移动这些窗口。

### 8.4 还原

窗口选择器会在移动前保存最近一次移动快照。点击“还原”后：

- 普通窗口会回到移动前位置。
- 移动前是最小化的窗口，还原后会再次最小化。
- 移动前是隐藏状态的窗口，如果能被枚举和移动，还原后会再次隐藏。
- 多个窗口一起移动时，会分别恢复各自状态。

限制：

- 还原快照只保存在内存中。
- 刷新窗口列表、关闭选择器或重启应用后，最近一次还原快照会丢失。
- 完全隐藏到托盘且不暴露普通顶层窗口的程序，可能不会出现在窗口列表中。

## 9. 设置窗口

设置窗口当前支持：

| 设置项 | 说明 |
|---|---|
| `minimizeToTray` | 关闭主窗口时是否最小化到托盘 |
| `showOverlayPreview` | 移动窗口前是否显示半透明预览框 |
| `defaultGap` | 默认布局区域间距 |
| `defaultMargin` | 默认布局外边距 |
| `ignoredProcesses` | 忽略的进程名列表 |
| `ignoredWindowClasses` | 忽略的窗口类名列表 |
| 暂停快捷键 | 启用或暂停全局快捷键 |

说明：

- `overlayOpacity` 已在配置文件中支持，默认值 `0.35`。
- 当前设置窗口尚未提供 `overlayOpacity` 的编辑控件。
- `startWithWindows` 配置字段存在，但实际开机自启动逻辑尚未完成。

## 10. Overlay Preview

如果 `showOverlayPreview=true`，触发窗口移动前会短暂显示一个半透明矩形，表示目标区域。

特点：

- 只用于预览，不参与布局计算。
- 不应抢占当前活动窗口焦点。
- 支持负坐标副屏和多显示器场景。
- 透明度来自配置中的 `overlayOpacity`，默认 `0.35`。

如果你不想看到预览，可以在设置窗口关闭 `showOverlayPreview`。

## 11. 可视化布局编辑器

当前项目提供一个 MVP 级可视化布局编辑器，用于创建任意比例分屏布局。

打开方式：

1. 打开主窗口。
2. 点击“布局编辑器”。

### 11.1 编辑器界面

编辑器包含：

- 布局 id
- 布局名称
- 间隙 `gap`
- 边距 `margin`
- 区域列表
- 可视化画布
- 保存 / 取消按钮

### 11.2 新增区域

点击“新增区域”可以添加一个区域。

区域会显示在右侧画布中。你可以连续新增多个区域，例如 6 个、8 个、9 个区域。

### 11.3 移动区域

在画布中拖拽区域矩形，可以改变该区域的：

```text
x
y
```

坐标会保持在 `0.0` 到 `1.0` 范围内。

### 11.4 缩放区域

拖拽区域右下角的小块，可以改变该区域的：

```text
width
height
```

区域不会缩小到 0，也不会超出画布边界。

### 11.5 编辑区域信息

选中一个区域后，可以编辑：

- 区域 id
- 区域名称

区域 id 会写入布局 JSON，区域名称会显示在托盘菜单中。

### 11.6 保存布局

点击“保存”后，程序会：

1. 生成 `LayoutDefinition`。
2. 使用 `LayoutValidator` 校验布局。
3. 调用 `LayoutStorage.SaveLayoutAsync` 保存 JSON。
4. 重新加载布局。
5. 刷新托盘布局菜单。

保存路径：

```text
%APPDATA%\WindowSnapper\layouts\{layoutId}.json
```

保存成功后，不需要重启应用，托盘“布局”菜单应能看到新布局。

### 11.7 编辑器当前限制

当前编辑器是 MVP，不是完整专业布局编辑器。

暂不支持：

- 自动吸附分割线。
- 多选。
- 撤销 / 重做。
- 键盘微调。
- 图形化导入 / 导出。
- 禁止区域重叠。
- 从现有布局文件反向打开编辑。

如果需要更复杂的布局，可以先使用编辑器生成基础 JSON，再手动微调。

## 12. 手写自定义布局 JSON

除了可视化编辑器，你仍然可以手写布局 JSON。

路径：

```text
%APPDATA%\WindowSnapper\layouts\*.json
```

示例：六分屏。

```json
{
  "id": "six-grid",
  "name": "六分屏",
  "version": 1,
  "gap": 0,
  "margin": 0,
  "zones": [
    {
      "id": "top-left",
      "name": "左上",
      "x": 0,
      "y": 0,
      "width": 0.333333,
      "height": 0.5
    },
    {
      "id": "top-middle",
      "name": "中上",
      "x": 0.333333,
      "y": 0,
      "width": 0.333334,
      "height": 0.5
    },
    {
      "id": "top-right",
      "name": "右上",
      "x": 0.666667,
      "y": 0,
      "width": 0.333333,
      "height": 0.5
    },
    {
      "id": "bottom-left",
      "name": "左下",
      "x": 0,
      "y": 0.5,
      "width": 0.333333,
      "height": 0.5
    },
    {
      "id": "bottom-middle",
      "name": "中下",
      "x": 0.333333,
      "y": 0.5,
      "width": 0.333334,
      "height": 0.5
    },
    {
      "id": "bottom-right",
      "name": "右下",
      "x": 0.666667,
      "y": 0.5,
      "width": 0.333333,
      "height": 0.5
    }
  ]
}
```

### 12.1 坐标规则

所有区域坐标都是相对比例：

```text
x: 0.0 到 1.0
y: 0.0 到 1.0
width: 大于 0，到 1.0
height: 大于 0，到 1.0
```

计算目标窗口位置时，WindowSnapper 使用当前显示器的 `WorkArea`，不是完整 `Bounds`，因此不会主动覆盖任务栏。

### 12.2 校验规则

布局必须满足：

- `id` 不为空。
- `name` 不为空。
- `gap >= 0`。
- `margin >= 0`。
- `zones` 至少有一个区域。

区域必须满足：

- `id` 不为空。
- `name` 不为空。
- `width > 0`。
- `height > 0`。
- `x >= 0`。
- `y >= 0`。
- `x + width <= 1`。
- `y + height <= 1`。

用户布局不能覆盖内置布局 id。

## 13. Workspace Snapshot

Workspace Snapshot 用于保存当前可管理窗口的位置，并在之后恢复。

托盘菜单包含：

- 保存工作区快照
- 恢复最近工作区快照

保存路径：

```text
%APPDATA%\WindowSnapper\workspaces\*.json
```

默认保存字段包括：

- version
- id
- name
- createdAt
- windows
- processName
- className
- monitorDeviceName
- relativeRect
- windowState

默认不保存：

- 完整窗口标题。
- 浏览器 URL。
- 用户文件路径。
- 命令行参数。

恢复行为：

- 快照按进程名和窗口类名匹配仍在运行的窗口。
- 目标位置会按当前显示器 `WorkArea` 重新计算。
- 如果快照记录窗口是最小化状态，恢复后会再次最小化。

限制：

- 多个同进程同窗口类的窗口只能按有限规则匹配。
- 管理员权限窗口、系统窗口、全屏游戏窗口不保证可恢复。
- 当前功能仍需要 Windows 桌面环境人工验证。

## 14. 配置文件位置

WindowSnapper 使用以下本地路径：

```text
%APPDATA%\WindowSnapper\config.json
%APPDATA%\WindowSnapper\layouts\*.json
%APPDATA%\WindowSnapper\workspaces\*.json
%LOCALAPPDATA%\WindowSnapper\logs\app.log
```

说明：

- 配置不存在时会创建默认配置。
- 配置损坏时会备份为 `.bak`，然后恢复默认配置。
- 用户布局和工作区快照只保存在本机。

## 15. 配置文件示例

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

通常不需要手动修改配置文件。设置窗口能覆盖一部分常用设置。

## 16. 快捷键冲突排查

如果启动时提示：

```text
全局快捷键注册失败
```

常见原因是默认快捷键被其他程序占用。

最容易冲突的是：

```text
Ctrl + Alt + Left
Ctrl + Alt + Right
Ctrl + Alt + Up
Ctrl + Alt + Down
```

这些快捷键经常被显卡驱动用于屏幕旋转。

常见占用来源：

- Intel Graphics Command Center
- AMD Radeon Software
- NVIDIA 控制面板或驱动工具
- PowerToys Keyboard Manager
- AutoHotkey 脚本
- 其他窗口管理工具
- 笔记本厂商热键工具

### 16.1 查看诊断信息

当前版本注册失败时会尽量显示具体失败快捷键，例如：

```text
Hotkey 'Ctrl+Alt+Right' could not be registered.
RegisterHotKey failed with Win32 error 1409: 热键已注册。
```

如果没有看到诊断信息，可能是因为此前注册失败后已经把配置保存为：

```json
"hotkeysPaused": true
```

此时程序启动时不会再次尝试注册快捷键。

解决方式：

1. 右键托盘图标。
2. 点击“恢复快捷键”。
3. 查看新的失败提示。

或者手动编辑：

```text
%APPDATA%\WindowSnapper\config.json
```

把：

```json
"hotkeysPaused": true
```

改成：

```json
"hotkeysPaused": false
```

然后退出托盘中的旧进程，重新启动程序。

### 16.2 解决冲突

建议先关闭显卡控制面板里的屏幕旋转快捷键。

如果数字快捷键可用，而方向键快捷键不可用，通常就是显卡旋转快捷键占用了方向键组合。

## 17. 为什么有些窗口不能移动

WindowSnapper 默认只管理普通桌面应用窗口。

以下窗口通常不会被管理：

- 任务栏。
- 桌面。
- 开始菜单。
- 系统弹窗。
- 不可见窗口。
- 最小化窗口。窗口选择器会尽量列出并先恢复后移动，但普通活动窗口快捷键流程仍不直接管理最小化窗口。
- 无尺寸窗口。
- 全屏独占游戏。
- UAC 安全桌面。
- 管理员权限窗口。
- WindowSnapper 自己的主窗口、设置窗口和 Overlay 窗口。

如果移动失败，常见原因：

- 目标窗口是管理员权限，WindowSnapper 是普通权限。
- 目标窗口是系统窗口。
- 目标窗口不允许调整大小。
- 目标窗口是全屏游戏或特殊渲染窗口。

项目默认不自动请求管理员权限，也不绕过系统权限边界。

## 18. 多显示器、DPI 和视觉贴边

WindowSnapper 支持多显示器的关键规则：

- 不假设屏幕从 `(0, 0)` 开始。
- 副屏可以在主屏左侧或上方，因此坐标可能为负数。
- 目标区域基于显示器 `WorkArea`，不会主动覆盖任务栏。
- 支持横屏和竖屏混用。

注意：

- 真实多显示器、多 DPI 环境仍需要人工验证。
- 如果任务栏位置特殊，布局仍应基于 `WorkArea` 计算。

当 `defaultGap=0` 且 `defaultMargin=0` 时，布局计算会按密铺方式生成目标区域。Windows 现代窗口可能带有不可见 resize border，WindowSnapper 在 Win32 层会做 DWM 可见边框补偿，减少视觉空隙。

如果仍看到空隙，常见原因包括：

- 目标应用自绘边框或阴影。
- 应用本身限制最小尺寸。
- 不同 DPI 显示器之间移动时存在系统舍入。
- 窗口不是标准可调整大小窗口。

## 19. 构建、测试和发布

构建：

```powershell
dotnet restore
dotnet build WindowSnapper.sln
```

运行：

```powershell
dotnet run --project src\WindowSnapper.App\WindowSnapper.App.csproj
```

测试：

```powershell
dotnet test WindowSnapper.sln
```

发布：

```powershell
dotnet publish src\WindowSnapper.App\WindowSnapper.App.csproj -c Release
```

发布产物通常位于：

```text
src\WindowSnapper.App\bin\Release\net8.0-windows\publish\
```

运行其中的：

```text
WindowSnapper.App.exe
```

## 20. 当前限制

当前版本仍有限制：

- 默认快捷键暂不支持在 UI 中改绑。
- `Ctrl + Alt + Space` 已注册，但还没有完整绑定到窗口选择器/布局选择器体验。
- 开机自启动字段存在，但实际启用/关闭逻辑尚未完成。
- 布局编辑器是 MVP，不是完整图形化编辑器。
- 工作区快照的窗口匹配规则有限。
- 窗口选择器的还原快照是内存状态，不会持久化。
- 隐藏到托盘且不暴露普通顶层窗口句柄的程序，可能无法被窗口选择器列出。
- 真实 Win32、托盘、全局快捷键、多显示器和 DPI 行为需要 Windows 桌面人工验证。

## 21. 推荐使用方式

日常推荐流程：

1. 启动 WindowSnapper。
2. 用默认快捷键完成常用分屏。
3. 需要复杂布局时，打开“布局编辑器”创建自定义布局。
4. 保存后从托盘“布局”菜单选择自定义区域。
5. 如果快捷键冲突，先关闭显卡或其他工具的同组合快捷键。
6. 对重要工作区，可以使用“保存工作区快照”记录当前窗口位置。
