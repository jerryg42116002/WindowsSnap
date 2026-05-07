# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现 WindowSnapper.Layouts 模块
- 实现日期：2026-05-07
- 相关需求：实现布局模型、布局计算、布局校验、内置布局和 Layouts 单元测试。
- 实现内容：新增 LayoutDefinition、ZoneDefinition、ZoneRect、LayoutEngine、LayoutValidator、BuiltinLayouts；补充覆盖 WorkArea、margin、gap、负坐标、竖屏、越界校验和内置布局 id 的测试。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Layouts/LayoutDefinition.cs` | 新增 | 布局定义模型。 |
| `src/WindowSnapper.Layouts/ZoneDefinition.cs` | 新增 | 归一化区域定义模型。 |
| `src/WindowSnapper.Layouts/ZoneRect.cs` | 新增 | 计算后的 zone id 与目标 RectInt。 |
| `src/WindowSnapper.Layouts/LayoutEngine.cs` | 新增 | 基于 MonitorInfo.WorkArea、layout 和 zone id 计算目标 RectInt。 |
| `src/WindowSnapper.Layouts/LayoutValidator.cs` | 新增 | 校验布局和 zone 坐标规则。 |
| `src/WindowSnapper.Layouts/LayoutValidationError.cs` | 新增 | 结构化校验错误。 |
| `src/WindowSnapper.Layouts/LayoutValidationErrorCode.cs` | 新增 | 校验错误枚举，避免内部 magic string。 |
| `src/WindowSnapper.Layouts/BuiltinLayouts.cs` | 新增 | 提供 MVP 要求的 12 个稳定内置布局 id。 |
| `tests/WindowSnapper.Layouts.Tests/LayoutEngineTests.cs` | 新增 | 布局计算测试。 |
| `tests/WindowSnapper.Layouts.Tests/LayoutValidatorTests.cs` | 新增 | 布局校验测试。 |
| `tests/WindowSnapper.Layouts.Tests/BuiltinLayoutsTests.cs` | 新增 | 内置布局 id 测试。 |
| `tests/WindowSnapper.Layouts.Tests/LayoutGlobalUsings.cs` | 新增 | 测试项目导入 Layouts 命名空间。 |

---

## 3. 涉及的类、函数、模块

- `LayoutDefinition`
- `ZoneDefinition`
- `ZoneRect`
- `LayoutEngine.CalculateTargetRect(MonitorInfo, LayoutDefinition, string)`
- `LayoutEngine.CalculateZoneRect(MonitorInfo, LayoutDefinition, string)`
- `LayoutValidator.Validate(LayoutDefinition?)`
- `LayoutValidator.GetErrors(LayoutDefinition?)`
- `BuiltinLayouts.All`
- `BuiltinLayouts.FindById(string)`

---

## 4. 需要重点测试的功能

- [ ] 1920x1080 WorkArea 下 left-half 得到 `RectInt(0, 0, 960, 1080)`。
- [ ] 1920x1080 WorkArea 下 right-half 得到 `RectInt(960, 0, 960, 1080)`。
- [ ] `margin=8` 时外边缘正确收缩。
- [ ] `gap=8` 时左右相邻区域之间间距为 8。
- [ ] WorkArea 为负坐标时 X 坐标保持负值。
- [ ] 竖屏 WorkArea 下比例计算正确。
- [ ] 计算使用 `MonitorInfo.WorkArea`，不使用 `Bounds`。
- [ ] `x + width > 1` 校验失败并返回 `ZoneRightOutOfRange`。
- [ ] `y + height > 1` 校验失败并返回 `ZoneBottomOutOfRange`。
- [ ] 内置布局 id 非空、唯一，并包含 AGENTS.md 要求的 12 个 id。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

```bash
dotnet test tests/WindowSnapper.Layouts.Tests/WindowSnapper.Layouts.Tests.csproj
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
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本次只涉及 Layouts 纯逻辑模块，不需要发布验证。 |
| 真实 Win32/WPF/托盘行为验证 | 本次未实现 UI、Win32、托盘或真实窗口移动。 |

---

## 7. 已知风险

- 本轮 implement 未运行 dotnet 验证命令，可能存在编译问题，需要 test-agent 验证。
- 现有 Core 模块仍处于未验证状态；Layouts 依赖 `RectInt`、`MonitorInfo`、`Result<T>`。
- margin/gap 语义已固定为：margin 作用于贴近 WorkArea 外边缘的边，gap 作用于内部边，相邻内部边合计形成完整 gap。

---

## 8. 需要人工验证的部分

- [ ] 无需人工验证真实桌面窗口；本轮是纯逻辑模块。
- [ ] 后续接入窗口移动时确认调用方传入的是目标窗口所在显示器的 `WorkArea`。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 仅执行了文档/源码读取和 `rg` 静态搜索检查。
- 未实现 UI、Win32、文件读写或托盘逻辑。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
