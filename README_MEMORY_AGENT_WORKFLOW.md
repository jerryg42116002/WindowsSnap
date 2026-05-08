# Memory Agent Workflow

本文档面向使用者说明 `memory-agent` 是什么、为什么需要它，以及如何与现有 agent 工作流配合。

## memory-agent 是什么

`memory-agent` 是一个 Codex skill，用于在一个开发阶段结束后维护项目长期记忆。

它会把长期有效的信息写入：

```text
.agent_memory/PROJECT_MEMORY.md
.agent_memory/DECISIONS.md
.agent_memory/BUG_PATTERNS.md
```

这些文件帮助后续新会话快速理解：

- 项目定位。
- 技术栈。
- 架构边界。
- 当前已实现能力。
- 当前正在开发的能力。
- 重要架构决策。
- 已踩过的 bug。
- 防复发规则。
- 长期隐私和安全原则。

## 为什么不能维护完整对话历史

完整对话历史通常包含大量临时内容：

- 一次性 prompt。
- 中间假设。
- 已废弃方案。
- 重复的文件内容。
- 调试输出。
- 大段源码。

这些内容会让后续 agent 更难判断什么是长期有效事实。`memory-agent` 只保留未来仍有价值的信息，并且不记录用户隐私、完整窗口标题、浏览器 URL、用户文件路径或命令行参数等敏感信息。

## .agent_memory 与 .agent_handoff 的区别

| 目录 | 用途 | 生命周期 |
|---|---|---|
| `.agent_handoff/` | 当前任务的计划、测试交接、审查报告、上下文包 | 当前任务或短期下一步 |
| `.agent_memory/` | 长期项目记忆、稳定决策、bug 模式 | 跨任务、跨会话长期保留 |

建议：

- 当前任务怎么继续，写入 `.agent_handoff/`。
- 未来项目长期要记住什么，写入 `.agent_memory/`。

## 如何调用 memory-agent

在一个功能完成、测试和审查结束后调用：

```text
$memory-agent
请读取 AGENTS.md、.agent_handoff、git diff 和相关源码，将长期有效信息更新到 .agent_memory/。
```

如果只是要为下一轮对话压缩上下文，应调用：

```text
$context-pack-agent
```

## 推荐流程示例

复杂任务结束后：

```text
$test-agent
    ↓
$review-agent
    ↓
$memory-agent
    ↓
$context-pack-agent
```

新任务开始前：

```text
$context-pack-agent
    ↓
$plan-agent
    ↓
$implement-agent
```

## 使用建议

- 重要功能完成后再沉淀记忆。
- 未验证的信息写“待确认”。
- 不要让 memory 文档变成流水账。
- 发现旧信息过时，应移动到“过时记录”，不要直接删除。
- 如果 memory-agent 发现某个稳定工作流规则应成为仓库规则，可先写入 `docs/AGENTS_MEMORY_WORKFLOW_APPENDIX.md`，再由用户决定是否追加到 `AGENTS.md`。
