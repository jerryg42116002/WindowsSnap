# AGENTS Memory Workflow Appendix

> 本文档用于追加到 `AGENTS.md`。
> 目的：说明 `memory-agent` 的用途、调用时机、与 `context-pack-agent` 的区别和推荐工作流。

## memory-agent 用途

`memory-agent` 用于在阶段性开发完成后维护项目长期记忆文档。

它读取当前任务交接文件、测试结果、审查报告、必要的 `git status` / `git diff` 和相关源码摘要，把未来仍有价值的信息沉淀到：

```text
.agent_memory/PROJECT_MEMORY.md
.agent_memory/DECISIONS.md
.agent_memory/BUG_PATTERNS.md
```

memory-agent 不写业务代码，不测试代码，不复制完整对话历史。

## 何时调用

适合调用：

- 一个功能完成后。
- 测试和 review 完成后。
- 做出重要架构决策后。
- 修复重要 bug 后。
- 项目方向发生变化后。
- 新开对话前想沉淀长期信息时。

不适合调用：

- 只是临时问答。
- 只是生成一次性 prompt。
- 当前任务还没完成。
- 信息还没有验证。
- 只是想压缩当前任务上下文。

## 与 context-pack-agent 的区别

| 项目 | memory-agent | context-pack-agent |
|---|---|---|
| 目标 | 保存长期有效项目记忆 | 压缩当前任务上下文 |
| 输出位置 | `.agent_memory/` | `.agent_handoff/CONTEXT_PACK.md` |
| 生命周期 | 跨任务、跨会话长期保留 | 当前任务或下一轮对话使用 |
| 记录内容 | 项目定位、架构决策、bug 模式、防复发规则 | 当前任务目标、修改范围、关键文件、下一步指令 |
| 不应包含 | 完整对话、大段源码、临时想法 | 完整 AGENTS、大段源码、长期历史全集 |

简单判断：

- 只影响下一步任务：使用 `context-pack-agent`。
- 未来多轮开发仍应记住：使用 `memory-agent`。

## 推荐工作流

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

## 固定规则

- memory-agent 必须优先读取 `AGENTS.md`。
- memory-agent 应读取 `.agent_handoff/IMPLEMENT_PLAN.md`、`.agent_handoff/TEST_PLAN.md`、`.agent_handoff/REVIEW_REPORT.md`、`.agent_handoff/CONTEXT_PACK.md` 和必要的 `git diff`。
- memory-agent 只记录长期有效信息。
- 信息不确定时写“待确认”，不要编造。
- 内容已存在时不要重复追加。
- 旧内容明显过时时，应移动到“过时记录”或历史小节，不要直接删除。
- 所有 memory 文档使用中文。
