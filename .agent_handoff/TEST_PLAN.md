# TEST_PLAN.md

> 本文件由 implement-agent 更新，由 test-agent 读取。
> 目的：把“写代码”和“测试验证”解耦。

## 1. 本次实现摘要

- 任务名称：新增 memory-agent skill 与长期记忆工作流文档
- 实现日期：2026-05-07
- 相关需求：
  - 新增标准 Codex skill：`memory-agent`。
  - 新增 `.agent_memory/` 长期记忆文档模板。
  - 新增面向 AGENTS.md 的 workflow appendix。
  - 新增面向用户的 memory-agent 工作流说明。
  - 本轮只做文档和 skill 工作流，不写业务代码，不运行 dotnet 验证命令。
- 实现内容：
  - 创建 `.agents/skills/memory-agent/SKILL.md`，包含 YAML front matter 和完整工作规则。
  - 创建 `.agent_memory/PROJECT_MEMORY.md`，记录项目定位、技术栈、架构规则、当前能力、路线、隐私安全原则。
  - 创建 `.agent_memory/DECISIONS.md`，记录重要架构和工作流决策。
  - 创建 `.agent_memory/BUG_PATTERNS.md`，记录已知 bug 模式和防复发规则。
  - 创建 `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md`，用于后续追加到 `AGENTS.md`。
  - 创建 `README_MEMORY_AGENT_WORKFLOW.md`，面向用户说明 memory-agent 的用途和推荐流程。

## 2. 修改文件列表

| 文件 | 修改类型 | 说明 |
|---|---|---|
| `.agents/skills/memory-agent/SKILL.md` | 新增 | 标准 Codex skill，定义 memory-agent 的角色、适用场景、输入输出、筛选规则和最终回复格式。 |
| `.agent_memory/PROJECT_MEMORY.md` | 新增 | 长期项目记忆模板，并预填当前稳定项目事实。 |
| `.agent_memory/DECISIONS.md` | 新增 | 架构决策记录模板，并记录 memory-agent、handoff/memory 分工、隐私和 implement/test 分离决策。 |
| `.agent_memory/BUG_PATTERNS.md` | 新增 | Bug 模式模板，并记录 StoragePaths 同步、implement 阶段验证边界、memory/context 职责混用等防复发规则。 |
| `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md` | 新增 | 可追加到 `AGENTS.md` 的 memory-agent 工作流说明。 |
| `README_MEMORY_AGENT_WORKFLOW.md` | 新增 | 面向用户的 memory-agent 使用说明。 |
| `.agent_handoff/TEST_PLAN.md` | 修改 | 更新本轮文档/skill 验证交接。 |

## 3. 涉及的类、函数、模块

- 不涉及业务代码、C# 类、函数或项目引用。
- 涉及 agent workflow 文档：
  - `memory-agent`
  - `.agent_memory/PROJECT_MEMORY.md`
  - `.agent_memory/DECISIONS.md`
  - `.agent_memory/BUG_PATTERNS.md`
  - `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md`
  - `README_MEMORY_AGENT_WORKFLOW.md`

## 4. 需要重点测试的功能

- [ ] `.agents/skills/memory-agent/SKILL.md` 文件路径正确。
- [ ] `SKILL.md` YAML front matter 包含：
  - `name: memory-agent`
  - `description: ...`
- [ ] `SKILL.md` 包含用户要求的结构：
  - 角色定位
  - 适用场景
  - 不适用场景
  - 输入文件
  - 输出文件
  - 工作原则
  - 信息筛选规则
  - PROJECT_MEMORY.md 更新规则
  - DECISIONS.md 更新规则
  - BUG_PATTERNS.md 更新规则
  - 去重与过时信息处理规则
  - 最终回复格式
- [ ] `.agent_memory/PROJECT_MEMORY.md`、`.agent_memory/DECISIONS.md`、`.agent_memory/BUG_PATTERNS.md` 均存在，且 Markdown 结构稳定。
- [ ] `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md` 说明 memory-agent 用途、何时调用、与 context-pack-agent 区别和推荐工作流。
- [ ] `README_MEMORY_AGENT_WORKFLOW.md` 面向用户说明 memory-agent、完整对话历史限制、`.agent_memory` 与 `.agent_handoff` 区别、调用方式和推荐流程。
- [ ] 文档内容使用中文。
- [ ] 文档未复制完整对话历史、未包含大段源码、未包含敏感信息。

## 5. 推荐测试命令

> implement-agent 只填写建议命令，不运行这些命令。

本轮是文档/skill 工作流变更，不需要运行业务测试。建议 test-agent 使用轻量静态检查：

```bash
test -f .agents/skills/memory-agent/SKILL.md
test -f .agent_memory/PROJECT_MEMORY.md
test -f .agent_memory/DECISIONS.md
test -f .agent_memory/BUG_PATTERNS.md
test -f docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md
test -f README_MEMORY_AGENT_WORKFLOW.md
rg "^name: memory-agent$|^description:" .agents/skills/memory-agent/SKILL.md
rg "角色定位|适用场景|不适用场景|输入文件|输出文件|工作原则|信息筛选规则|PROJECT_MEMORY.md 更新规则|DECISIONS.md 更新规则|BUG_PATTERNS.md 更新规则|去重与过时信息处理规则|最终回复格式" .agents/skills/memory-agent/SKILL.md
git diff --check
```

可选人工阅读：

```bash
sed -n '1,260p' .agents/skills/memory-agent/SKILL.md
sed -n '1,220p' docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md
sed -n '1,220p' README_MEMORY_AGENT_WORKFLOW.md
```

## 6. 不建议自动运行的测试或命令

| 命令/测试 | 原因 |
|---|---|
| `dotnet restore` | 本轮未修改 .NET 项目或业务源码，文档/skill 验证不需要恢复依赖。 |
| `dotnet build` | 本轮未修改 C# 编译输入。 |
| `dotnet test` | 本轮未修改业务逻辑或测试代码。 |
| `dotnet publish` | 本轮不涉及发布产物。 |

## 7. 已知风险

- `.agent_memory/PROJECT_MEMORY.md` 根据当前 README、AGENTS 和交接文件预填了部分项目状态；其中 Repeat Hotkey Cycle 和 Workspace Snapshot 的测试验证状态仍标记为“待确认”。
- `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md` 尚未追加到 `AGENTS.md`；用户需要决定何时合并。
- 当前工作区已有其他未验证代码改动，本轮未回退、未测试、未审查这些业务改动。

## 8. 需要人工验证的部分

- [ ] 人工阅读 `SKILL.md`，确认 memory-agent 的边界符合团队预期。
- [ ] 人工决定是否将 `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md` 追加到 `AGENTS.md`。
- [ ] 后续实际调用 `$memory-agent` 时，确认它不会复制完整对话历史或写入临时信息。

## 9. implement-agent 备注

- implement 阶段未运行 `dotnet restore`、`dotnet build`、`dotnet test`、`dotnet publish`；验证交由 test-agent 执行。
- 本轮只创建文档和 Codex skill 文件，没有修改业务代码。
