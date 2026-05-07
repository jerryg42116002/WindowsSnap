# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。  
> 目的：把“写代码”和“测试验证”解耦，避免实现阶段和测试阶段互相污染上下文。  
> implement 阶段不得运行 dotnet 验证命令；只在本文件中记录建议命令和测试范围。

---

## 1. 本次实现摘要

- 任务名称：实现 WindowSnapper.Storage 模块
- 实现日期：2026-05-07
- 相关需求：实现本地 JSON 配置读写、用户布局 JSON 读取校验、配置迁移和默认配置。
- 实现内容：新增 `AppSettings`、`SettingsStorage`、`LayoutStorage`、`ConfigMigration`、`DefaultSettingsFactory`、`StoragePaths` 和原子 JSON 写入辅助逻辑；新增 Storage 单元测试覆盖配置创建、读取、损坏恢复、默认字段、版本迁移和布局 JSON 校验。

---

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `src/WindowSnapper.Storage/AppSettings.cs` | 新增 | 全局配置模型，包含默认字段值。 |
| `src/WindowSnapper.Storage/DefaultSettingsFactory.cs` | 新增 | 创建默认配置。 |
| `src/WindowSnapper.Storage/ConfigMigration.cs` | 新增 | 配置 version 迁移和默认值补齐。 |
| `src/WindowSnapper.Storage/SettingsStorage.cs` | 新增 | 读取/创建/保存 `config.json`，损坏时 `.bak` 备份并恢复默认配置。 |
| `src/WindowSnapper.Storage/LayoutStorage.cs` | 新增 | 读取/保存用户自定义布局 JSON，并调用 `LayoutValidator` 校验。 |
| `src/WindowSnapper.Storage/StoragePaths.cs` | 新增 | 默认路径和测试可注入路径。 |
| `src/WindowSnapper.Storage/JsonStorageOptions.cs` | 新增 | Storage JSON 序列化选项。 |
| `src/WindowSnapper.Storage/AtomicJsonFile.cs` | 新增 | 临时文件写入后替换目标文件。 |
| `src/WindowSnapper.Storage/StorageAssemblyMarker.cs` | 删除 | 移除占位 marker。 |
| `tests/WindowSnapper.Storage.Tests/SettingsStorageTests.cs` | 新增 | 配置读写/恢复/迁移测试。 |
| `tests/WindowSnapper.Storage.Tests/LayoutStorageTests.cs` | 新增 | 布局 JSON 读取和非法布局错误测试。 |
| `tests/WindowSnapper.Storage.Tests/TemporaryStorage.cs` | 新增 | 测试临时路径辅助。 |
| `tests/WindowSnapper.Storage.Tests/StorageGlobalUsings.cs` | 新增 | 测试项目导入 Storage 命名空间。 |
| `tests/WindowSnapper.Storage.Tests/PlaceholderTests.cs` | 删除 | 移除占位测试。 |

---

## 3. 涉及的类、函数、模块

- `AppSettings`
- `DefaultSettingsFactory.Create()`
- `ConfigMigration.Migrate(AppSettings?)`
- `SettingsStorage.LoadOrCreateAsync(...)`
- `SettingsStorage.SaveAsync(...)`
- `LayoutStorage.LoadLayoutsAsync(...)`
- `LayoutStorage.LoadLayoutAsync(...)`
- `LayoutStorage.SaveLayoutAsync(...)`
- `StoragePaths.CreateDefault()`

---

## 4. 需要重点测试的功能

- [ ] 配置不存在时创建默认 `%APPDATA%/WindowSnapper/config.json` 等价路径中的默认配置。
- [ ] 合法配置 JSON 能正常读取用户字段。
- [ ] 损坏配置 JSON 会备份为 `config.json.bak`，再恢复默认配置。
- [ ] 缺少新增字段时使用 `AppSettings` 默认值。
- [ ] 旧 version 配置迁移到 `ConfigMigration.CurrentVersion`。
- [ ] 保存配置时先写 `.tmp`，再替换目标文件。
- [ ] 合法用户布局 JSON 读取成功。
- [ ] 非法用户布局 JSON 返回 `ResultErrorCode.InvalidArgument` 和包含文件名/校验原因的错误信息。
- [ ] Storage 不依赖 WPF、Win32、网络、遥测或云同步。

---

## 5. 推荐测试命令

> implement-agent 只填写本节，不运行这些命令。test-agent 根据本节执行验证。

```bash
dotnet test tests/WindowSnapper.Storage.Tests/WindowSnapper.Storage.Tests.csproj
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
| `dotnet publish src/WindowSnapper.App/WindowSnapper.App.csproj -c Release` | 本次只涉及 Storage 纯本地文件模块，不需要发布验证。 |
| 真实 Win32/WPF/托盘行为验证 | 本次未实现 UI、Win32、托盘或真实窗口移动。 |
| 网络相关验证 | Storage 不应包含网络、遥测或云同步。 |

---

## 7. 已知风险

- 本轮 implement 未运行 dotnet 验证命令，可能存在编译或运行时问题，需要 test-agent 验证。
- Storage 依赖当前 Core 的 `Result<T>` 和 Layouts 的 `LayoutDefinition` / `LayoutValidator`，这些模块仍需由 test-agent 一并验证。
- `IReadOnlyList<string>` 和 record 构造模型的 `System.Text.Json` 反序列化行为需要通过测试确认。
- 原子写入采用同目录 `.tmp` 文件再 `File.Move(..., overwrite: true)` 替换目标文件。

---

## 8. 需要人工验证的部分

- [ ] 无需人工验证真实桌面窗口；本轮是本地 Storage 模块。
- [ ] Windows 环境下确认 `StoragePaths.CreateDefault()` 对应 `%APPDATA%/WindowSnapper` 和 `%LOCALAPPDATA%/WindowSnapper`。

---

## 9. implement-agent 备注

- 本轮未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`。
- 仅执行了文档/源码读取和 `rg` 静态搜索检查。
- 未实现 UI、Win32、网络、遥测、云同步或日志记录逻辑。
- 未记录完整窗口标题、浏览器 URL、用户文件路径或用户输入内容。

---

## 10. test-agent 测试结果

> test-agent 每次测试后在本节下面追加结果，不要删除旧结果。
