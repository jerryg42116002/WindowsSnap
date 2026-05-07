# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现 WindowSnapper.Hotkeys 模块
- 实现日期：2026-05-07
- 相关需求：实现全局快捷键业务模型、注册/注销抽象、命令分发、默认快捷键、快捷键解析和冲突检测。
- 实现内容：新增 `HotkeyDefinition`、`HotkeyModifiers`、`HotkeyKey`、`HotkeyCommand`、`HotkeyPressedEventArgs`、`IHotkeyRegistrar`、`HotkeyManager`、`HotkeyParser`、`DefaultHotkeys`；新增 Hotkeys 测试项目覆盖 parser、默认快捷键、冲突检测和注册失败路径。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Hotkeys/HotkeyDefinition.cs` | 新增 | 快捷键命令和 key chord 模型。 |
| `src/WindowSnapper.Hotkeys/HotkeyModifiers.cs` | 新增 | 快捷键修饰键 flags 枚举。 |
| `src/WindowSnapper.Hotkeys/HotkeyKey.cs` | 新增 | 支持的非修饰键枚举。 |
| `src/WindowSnapper.Hotkeys/HotkeyCommand.cs` | 新增 | 快捷键命令枚举，避免 magic string。 |
| `src/WindowSnapper.Hotkeys/HotkeyPressedEventArgs.cs` | 新增 | 快捷键触发事件参数。 |
| `src/WindowSnapper.Hotkeys/IHotkeyRegistrar.cs` | 新增 | 平台注册器抽象。 |
| `src/WindowSnapper.Hotkeys/HotkeyManager.cs` | 新增 | 注册、注销、冲突检测和事件分发。 |
| `src/WindowSnapper.Hotkeys/HotkeyParser.cs` | 新增 | 用户配置字符串解析和 chord 格式化。 |
| `src/WindowSnapper.Hotkeys/HotkeyChord.cs` | 新增 | 内部冲突检测 key。 |
| `src/WindowSnapper.Hotkeys/DefaultHotkeys.cs` | 新增 | AGENTS.md 要求的默认快捷键。 |
| `src/WindowSnapper.Hotkeys/HotkeysAssemblyMarker.cs` | 删除 | 移除占位 marker。 |
| `tests/WindowSnapper.Hotkeys.Tests/WindowSnapper.Hotkeys.Tests.csproj` | 新增 | Hotkeys 测试项目。 |
| `tests/WindowSnapper.Hotkeys.Tests/HotkeyParserTests.cs` | 新增 | parser 测试。 |
| `tests/WindowSnapper.Hotkeys.Tests/HotkeyManagerTests.cs` | 新增 | 冲突检测、注册失败、注销和事件分发测试。 |
| `tests/WindowSnapper.Hotkeys.Tests/DefaultHotkeysTests.cs` | 新增 | 默认快捷键命令和唯一性测试。 |
| `tests/WindowSnapper.Hotkeys.Tests/GlobalUsings.cs` | 新增 | 测试项目全局 using。 |
| `WindowSnapper.sln` | 修改 | 加入 `WindowSnapper.Hotkeys.Tests`。 |

---

## 3. 涉及的类、函数、模块

- `DefaultHotkeys.All`
- `HotkeyParser.Parse(string, HotkeyCommand)`
- `HotkeyParser.ParseChord(string)`
- `HotkeyParser.FormatChord(HotkeyModifiers, HotkeyKey)`
- `HotkeyManager.RegisterDefaultHotkeys()`
- `HotkeyManager.RegisterMany(IEnumerable<HotkeyDefinition>)`
- `HotkeyManager.Register(HotkeyDefinition)`
- `HotkeyManager.UnregisterAll()`
- `HotkeyManager.HotkeyPressed`
- `IHotkeyRegistrar`

---

## 4. 需要重点测试的功能

- [ ] `Ctrl+Alt+Left` 解析为 `SnapLeftHalf`、`Control|Alt`、`Left`。
- [ ] `Ctrl+Alt+1` 解析为 `SnapZone1`、`Control|Alt`、`D1`。
- [ ] 空字符串、缺少非修饰键、不支持 token、多个非修饰键返回 `InvalidArgument`。
- [ ] 默认快捷键包含左/右/上/下半屏、区域 1-4、布局选择器。
- [ ] 默认快捷键 chord 唯一。
- [ ] `HotkeyManager.RegisterMany` 检测重复 chord 并返回冲突错误。
- [ ] registrar 注册失败时 `HotkeyManager` 不抛异常、不记录为已注册。
- [ ] `Dispose` 或 `UnregisterAll` 会调用 registrar 注销全部快捷键。
- [ ] registrar 报告按键触发时，`HotkeyManager` 分发 `HotkeyPressed` 事件。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

```bash
dotnet test tests/WindowSnapper.Hotkeys.Tests/WindowSnapper.Hotkeys.Tests.csproj
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
| 真实 `RegisterHotKey` / `UnregisterHotKey` 自动化测试 | 本轮 Hotkeys 是业务抽象层，真实 Win32 registrar 尚未实现；真实全局快捷键依赖桌面会话。 |
| WPF code-behind 事件测试 | 本轮未接入 UI，不应把分发逻辑写进 WPF。 |
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本次只涉及 Hotkeys 纯逻辑模块，不需要发布验证。 |

---

## 7. 已知风险

- 本轮 implement 未运行 dotnet 验证命令，可能存在编译问题，需要 test-agent 验证。
- `IHotkeyRegistrar` 只是平台抽象；后续需要在 Win32 模块实现基于 `RegisterHotKey` / `UnregisterHotKey` 的 registrar。
- 默认快捷键已硬编码在 `DefaultHotkeys`，后续 Storage/App 接入时应支持配置文件覆盖。
- 当前 parser 只支持 MVP 所需按键：方向键、1-4、Space 和 Ctrl/Alt/Shift/Win 修饰键。

---

## 8. 需要人工验证的部分

- [ ] 后续 Win32 registrar 接入后，在 Windows 桌面环境验证真实全局快捷键注册、冲突和注销。
- [ ] 应用退出流程接入后，确认调用 `HotkeyManager.Dispose()` 或 `UnregisterAll()`。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 仅执行了文档/源码读取和 `rg` 静态搜索检查。
- 未实现 WPF UI、Win32 `RegisterHotKey` 具体 registrar、网络、遥测或云同步。
- Hotkey 命令使用 `HotkeyCommand` 枚举，没有把命令建模为 magic string。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
