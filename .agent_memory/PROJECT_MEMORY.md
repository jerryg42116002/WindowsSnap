# PROJECT_MEMORY.md

> 本文件记录长期有效的项目记忆。
> 不记录完整对话历史，只记录稳定决策、当前状态、关键约束和长期经验。
> 由 memory-agent 维护，其他 agent 可在任务开始前读取。

## 1. 项目定位

- 项目名称：WindowSnapper
- 项目目标：构建一个 Windows 桌面分屏工具，允许用户通过快捷键、托盘菜单和自定义布局，将当前窗口快速移动到指定屏幕区域，并支持多显示器、DPI 缩放和 JSON 布局配置。
- 不做什么：不做完整桌面环境、虚拟桌面管理器、云同步、遥测、账号系统、自动管理员提权、进程注入、复杂布局编辑器优先版本。
- 当前优先级：稳定性 > 正确性 > 隐私安全 > 可测试性 > 用户体验 > 功能数量。

## 2. 技术栈

- 语言：C# / .NET 8
- UI：WPF，系统托盘使用 Windows Forms `NotifyIcon`
- 系统 API：Win32 API P/Invoke
- 测试框架：xUnit
- 配置格式：本地 JSON 文件，使用 `System.Text.Json`

## 3. 核心架构规则

- Core 只包含纯模型、接口和通用类型，不依赖 WPF、Win32、Storage 或文件系统。
- Layouts 只依赖 Core，布局计算必须保持纯逻辑，不读取真实显示器或文件系统。
- Storage 负责本地 JSON 和配置文件读写，不依赖 WPF。
- Win32 项目集中封装所有 P/Invoke，对外暴露 Core 友好的类型。
- App 负责 WPF 启动、依赖组合和用户交互，不直接调用 `NativeMethods`。
- Tray 只负责托盘图标和菜单命令，不承载布局计算、Win32 调用或配置解析。
- UI code-behind 只做事件转发，不写复杂业务逻辑。

## 4. 当前已实现能力

- [x] .NET 8 C# 解决方案和分层项目结构。
- [x] Core 基础模型、结果类型和窗口/显示器接口。
- [x] Layouts 模块，包含内置布局、布局校验和基于 WorkArea 的区域计算。
- [x] Storage 模块，包含配置读写、默认配置、损坏配置备份恢复、布局 JSON 加载。
- [x] Win32 薄封装，包含窗口信息、显示器信息、窗口移动、窗口过滤和全局快捷键注册能力。
- [x] Hotkeys 模块，包含默认快捷键、解析、冲突检测、注册/注销和命令分发。
- [x] Snap 服务闭环：快捷键命令到布局区域，再移动当前活动窗口。
- [x] WPF App 和系统托盘 MVP，支持打开窗口、设置、暂停/恢复快捷键、退出。
- [x] 用户自定义布局 JSON 加载并与内置布局合并。
- [x] Overlay Preview MVP，可按配置显示/关闭。
- [x] Repeat Hotkey Cycle 已实现，测试验证状态待确认。
- [x] Workspace Snapshot MVP 已实现，测试验证状态待确认。

## 5. 当前正在开发

- memory-agent 工作流：用于阶段性开发完成后维护 `.agent_memory/` 长期记忆文档。
- Workspace Snapshot MVP 的自动测试与 Windows 人工验证：待确认。
- Repeat Hotkey Cycle 的自动测试与 Windows 人工验证：待确认。

## 6. 下一步路线

1. 使用 test-agent 验证当前未验证改动，优先跑 Core、Storage、Workspaces、Snap 等相关测试。
2. 使用 review-agent 审查 Workspace Snapshot、Repeat Hotkey Cycle 和 memory-agent 工作流是否符合架构边界。
3. 将 `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md` 的稳定内容追加到 `AGENTS.md`。
4. 在 Windows 桌面环境人工验证托盘、全局快捷键、Overlay、多显示器、DPI 和 Workspace Snapshot 恢复行为。

## 7. 长期注意事项

- 坐标计算必须基于 `MonitorInfo.WorkArea`，不要用 `Bounds` 覆盖任务栏。
- 不要假设显示器从 `(0, 0)` 开始；副屏可能有负坐标。
- Win32、WPF、托盘、全局快捷键和真实窗口移动需要 Windows 桌面环境人工验证。
- implement-agent 阶段不得运行 `dotnet restore/build/test/publish`，只更新 `.agent_handoff/TEST_PLAN.md`。
- test-agent 必须先读取 `.agent_handoff/TEST_PLAN.md`，再按从小到大的顺序验证。
- 当前存在阶段性交接文件和长期记忆文件，两者职责不同，不能混用。

## 8. 隐私与安全原则

- 不添加网络请求、遥测、账号系统或云同步。
- 不自动申请管理员权限。
- 不注入其他进程。
- 不强制管理 UAC 安全桌面、全屏游戏、系统窗口、任务栏或桌面。
- 默认不记录或长期保存完整窗口标题、浏览器 URL、用户文件路径、命令行参数或用户输入内容。
- Workspace Snapshot 只应保存恢复窗口位置所需的非敏感字段，例如进程名、窗口类名、显示器设备名、相对矩形和窗口状态。

## 9. 过时记录

> 已不再适用但仍有参考价值的信息放在这里。

- 待确认。
