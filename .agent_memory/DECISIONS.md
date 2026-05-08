# DECISIONS.md

> 本文件记录重要架构决策。
> 格式：时间 + 决策 + 原因 + 影响。
> 不记录普通实现细节，只记录未来仍可能影响开发方向的决定。

## 决策记录

### 2026-05-07 - 使用 memory-agent 维护长期项目记忆

**决策：**

- 新增 `memory-agent` skill，用于在阶段性开发完成后维护 `.agent_memory/PROJECT_MEMORY.md`、`.agent_memory/DECISIONS.md` 和 `.agent_memory/BUG_PATTERNS.md`。

**原因：**

- 项目已经形成 plan/implement/test/review/context-pack 的多 agent 工作流，长期开发中容易因为上下文过长遗忘稳定决策、已踩 bug 和当前状态。
- `.agent_handoff/` 更适合当前任务交接，不适合保存跨阶段长期记忆。

**影响：**

- 新会话或重要阶段结束后，可以读取 `.agent_memory/` 获取长期稳定信息。
- memory-agent 只维护文档，不写业务代码、不运行长时间测试、不复制完整对话历史。

**替代方案：**

- 只依赖 `.agent_handoff/CONTEXT_PACK.md` 压缩上下文；缺点是它偏当前任务，容易丢失长期历史。
- 把长期记忆直接写入 `AGENTS.md`；缺点是 AGENTS 会越来越长，混合规则和历史信息。

**后续注意：**

- memory-agent 记录前应去重；不确定信息标记“待确认”。
- 稳定工作流规则可在 review 后追加到 `AGENTS.md`。

### 2026-05-07 - 区分 .agent_handoff 与 .agent_memory

**决策：**

- `.agent_handoff/` 用于当前任务阶段交接；`.agent_memory/` 用于跨任务、跨会话的长期项目记忆。

**原因：**

- 当前任务计划、测试结果和上下文包可能快速过时。
- 长期记忆需要保留项目定位、稳定架构约束、重要决策和防复发规则。

**影响：**

- context-pack-agent 应优先压缩当前任务，不应承担长期历史维护。
- memory-agent 不应复制完整交接文件，只提取长期有效信息。

**替代方案：**

- 将所有历史都保存在 `.agent_handoff/`；缺点是上下文噪声高，且难以区分当前有效信息和历史信息。

**后续注意：**

- 过时的长期信息应移动到“过时记录”，不要直接删除。

### 2026-05-07 - Workspace Snapshot 默认不保存敏感窗口信息

**决策：**

- Workspace Snapshot MVP 默认只保存 `processName`、`className`、`monitorDeviceName`、`relativeRect` 和 `windowState` 等恢复位置所需字段，不保存完整窗口标题、浏览器 URL、用户文件路径或命令行参数。

**原因：**

- 窗口标题、URL、文件路径和命令行参数可能包含隐私信息。
- MVP 以隐私安全和稳定性优先，接受同类窗口匹配不完美。

**影响：**

- 多个同进程同 class 窗口只能按枚举顺序做有限匹配。
- 后续增强匹配能力时仍不能默认引入敏感字段。

**替代方案：**

- 使用窗口标题或命令行参数提高匹配准确率；因隐私风险暂不采用。

**后续注意：**

- 如果未来支持更强匹配，应采用明确 opt-in，并避免长期保存敏感原文。

### 2026-05-07 - implement 与 test 阶段分离

**决策：**

- implement-agent 只实现和更新 `.agent_handoff/TEST_PLAN.md`，不运行 `dotnet restore/build/test/publish`；test-agent 负责验证。

**原因：**

- 项目可能涉及 Windows-only WPF/Win32 行为，在非 Windows 或受限环境中无法完整验证。
- 分离实现和测试能让交接更清晰，也避免 implement 阶段长时间调试扩大范围。

**影响：**

- implement 阶段最终回复必须明确未运行 dotnet 验证。
- test-agent 必须先读取 TEST_PLAN，再按聚焦测试到全量测试逐步验证。

**替代方案：**

- 每次实现后立即全量测试；缺点是与当前 agent 分工和环境限制冲突。

**后续注意：**

- 如果用户明确要求验证，应切换到 test-agent 或用户指定的验证阶段。
