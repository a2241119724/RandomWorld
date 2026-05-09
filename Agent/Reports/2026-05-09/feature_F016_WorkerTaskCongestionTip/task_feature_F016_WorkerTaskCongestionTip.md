# F016 任务队列拥堵 Tip 与优先级建议任务卡

## 基本信息

- 任务 ID：feature_F016_WorkerTaskCongestionTip
- 候选 ID：F016
- 原始候选：任务队列拥堵 Tip 与优先级建议
- 当前状态：Done
- 本次任务目录：`Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/`
- 全局候选报告路径：`Agent/Reports/feature_discovery.md`
- 任务分类：游戏业务功能 / 任务目标提示 / UI 反馈
- 游戏业务类型：任务目标提示
- 玩家价值：当工人任务等待过多时主动提醒玩家，帮助其暂停扩张、补充工人或调整任务开关。
- 开发价值：复用 F015 的任务队列快照和压力阈值，为后续任务优先级 UI、调度策略和运营面板提供只读建议层。
- 负责 Agent：UIAgent + AINPCAgent
- 需要的 Skill：ScriptGenerateSkill + TestSkill
- 风险等级：低

## 影响路径

- `Scripts/2D/Enum/WorkerTaskCongestionLevel.cs`
- `Scripts/2D/Constant/WorkerTaskCongestionConstant.cs`
- `Scripts/2D/Tool/WorkerTaskCongestionTool.cs`
- `Scripts/2D/Gameplay/WorkerTaskCongestionAdvisor.cs`
- `Scripts/2D/Editor/WorkerTaskCongestionAdvisorMenu.cs`
- `Scripts/2D/GlobalInit.cs`
- `Agent/Reports/feature_discovery.md`
- `Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/`

## 不应触碰路径

- 不修改存档结构、`ArchiveManager`、`ASaveData`。
- 不修改 Photon、RPC、网络同步逻辑。
- 不修改 AssetBundle、Addressables、StreamingAssets 运行时资源结构。
- 不删除、重命名或覆盖已有 Scene、Prefab、ScriptableObject、材质、图片、动画、音效或配置资源。
- 不改变 `WorkerTaskManager` 的任务派发、优先级、完成和取消语义。

## 功能边界

- 只读取 `WorkerTaskManager.CreateTaskQueueSnapshot()` 生成的只读任务队列快照。
- 只计算拥堵等级、主积压任务类型和玩家建议文案。
- 只通过现有 `GlobalInit.ShowTip()` 和 `ResourcesLocal/Prefabs/Tip.prefab` 展示提示，不新增或调整真实任务优先级。
- 提示按刷新间隔和冷却时间节流，避免任务长期拥堵时刷屏。
- Editor 菜单只用于查看建议、启用/禁用监控和手动触发一次 Tip。

## 业务规则说明

- 等待中任务数低于 F015 中等压力阈值时视为平稳。
- 等待中任务数达到 `WorkerTaskHudConstant.MediumWaitingTaskThreshold` 时视为繁忙，可在摘要中给出建议。
- 等待中任务数达到 `WorkerTaskHudConstant.HighWaitingTaskThreshold` 时视为拥堵，允许触发 Tip。
- 等待中任务数达到临界阈值时视为严重拥堵，Tip 文案强调暂停新增任务。
- 若某一任务类型占等待任务的大头，优先给出该类型建议；否则给出多类型积压建议。

## 数据流说明

1. `GlobalInit.WorkerUpdate()` 每帧调用 `WorkerTaskCongestionAdvisor.Instance.Tick()`。
2. `WorkerTaskCongestionAdvisor` 内部按 `WorkerTaskCongestionConstant.MonitorRefreshInterval` 节流。
3. 管理器读取 `WorkerTaskManager.CreateTaskQueueSnapshot()`。
4. `WorkerTaskCongestionTool.BuildReport()` 生成 `WorkerTaskCongestionReport`。
5. 报告变化时派发事件，并在满足拥堵等级和冷却限制时请求 Tip。
6. Tip 展示优先走现有 `GlobalInit.ShowTip()`；不可用时降级为 `Debug.Log`。

## UI 接入策略

- 已检查 `Scenes/Game.unity`，真实路径为 `Scenes/Game.unity`，文件较大且已有复杂 UI 层级。
- 本功能为短时 Tip 反馈，不需要常驻面板；优先复用现有 `GlobalInit.ShowTip()` 与 `ResourcesLocal/Prefabs/Tip.prefab`。
- 不直接手写 `Game.unity` YAML，避免破坏已有 Canvas、引用和脚本绑定。
- 不创建新的 `ResourcesLocal` Prefab，因为项目已有 `Tip.prefab` 且 `PrefabConstant.TIP` 已统一维护资源名。
- 降级方案：运行时代码动态复用现有 Tip UI；新增 Editor 菜单查看和手动触发建议。

## Scene / Prefab / ResourcesLocal 生成策略

- `Game.unity`：不直接修改。
- `ResourcesLocal` Prefab：不新增，复用 `ResourcesLocal/Prefabs/Tip.prefab`。
- Editor 菜单：新增 `工具/任务队列拥堵提示/`，支持运行时查看、开关和手动触发。
- 运行时代码动态 UI：通过 `GlobalInit.ShowTip()` 实例化现有 `Tip.prefab`。

## Tool 复用策略

- 已检查 `Scripts/2D/Tool/Tool.cs`：本功能不需要 UI 查找或输入焦点判断。
- 已检查 `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`：复用其快照数据来源、任务类型中文名和 F015 阈值语义。
- 已检查 `Scripts/2D/Tool/WorkerSupplyTool.cs`：面向补给缺口，不承载任务拥堵建议。
- 本次计划新增公共工具：`WorkerTaskCongestionTool`，负责拥堵等级、主积压类型、建议文案、颜色和签名生成。

## Enum 复用策略

- 已检查 `Scripts/2D/Enum`：无任务拥堵等级枚举。
- 继续复用 `AWorkerTask.WorkerTaskTypeEnum` 表达任务类型，不重复定义任务类型。
- 本次计划新增公共枚举：`WorkerTaskCongestionLevel`，表达 None、Smooth、Busy、Congested、Critical。

## Constant 复用策略

- 已检查 `PrefabConstant.cs`：复用 `PrefabConstant.TIP` 间接资源链路，不新增 Tip 资源名。
- 已检查 `WorkerTaskHudConstant.cs`：复用 F015 的任务压力阈值作为拥堵判断基础。
- 本次计划新增公共常量：`WorkerTaskCongestionConstant`，维护刷新间隔、Tip 冷却、菜单路径、默认文案、临界阈值和建议规则。

## 分层说明

- 公共枚举沉淀到 `Scripts/2D/Enum`：任务拥堵等级。
- 公共常量沉淀到 `Scripts/2D/Constant`：刷新、冷却、菜单和默认文案。
- 公共函数沉淀到 `Scripts/2D/Tool`：拥堵等级、建议文案和主积压类型计算。
- 业务状态管理放入 `Scripts/2D/Gameplay`：节流、事件、Tip 请求和当前报告。
- Editor 专用逻辑放入 `Scripts/2D/Editor`：运行时查看和开关菜单。

## 执行步骤

1. 新增任务拥堵等级枚举。
2. 新增任务拥堵提示常量。
3. 新增任务拥堵建议工具。
4. 新增运行时 `WorkerTaskCongestionAdvisor`。
5. 修改 `GlobalInit.WorkerUpdate()` 接入节流 Tick。
6. 新增 Editor 菜单。
7. 完成静态检查、验证记录和全局候选回写。

## 验证步骤

1. 检查新增运行时代码不引用 `UnityEditor`。
2. 检查新增 Tool / Enum / Constant 注释完整且无重复任务类型定义。
3. 检查 `GlobalInit` 仅新增只读 Tick，不改变 Worker 状态衰减和补给提示顺序。
4. 检查 `Scenes/Game.unity` 和 `ResourcesLocal` 未被直接修改。
5. 尝试执行可用的静态语法或文本检查；Unity 编译和 Play Mode 若无法运行，则记录原因。

## 回滚方案

- 删除本次新增脚本及 `.meta`：
  - `Scripts/2D/Enum/WorkerTaskCongestionLevel.cs`
  - `Scripts/2D/Constant/WorkerTaskCongestionConstant.cs`
  - `Scripts/2D/Tool/WorkerTaskCongestionTool.cs`
  - `Scripts/2D/Gameplay/WorkerTaskCongestionAdvisor.cs`
  - `Scripts/2D/Editor/WorkerTaskCongestionAdvisorMenu.cs`
- 移除 `GlobalInit.WorkerUpdate()` 中新增的 `WorkerTaskCongestionAdvisor.Instance.Tick()` 调用。
- 将 `Agent/Reports/feature_discovery.md` 中 F016 状态恢复为 `[TODO]`。

## 结果区

- 最终状态：`[DONE]`
- 已完成内容：新增任务队列拥堵等级、拥堵提示常量、拥堵建议工具、运行时拥堵提示管理器和 Editor 菜单，并在 `GlobalInit.WorkerUpdate()` 接入只读 Tick。
- 修改文件：
  - `Scripts/2D/Enum/WorkerTaskCongestionLevel.cs`
  - `Scripts/2D/Constant/WorkerTaskCongestionConstant.cs`
  - `Scripts/2D/Tool/WorkerTaskCongestionTool.cs`
  - `Scripts/2D/Gameplay/WorkerTaskCongestionAdvisor.cs`
  - `Scripts/2D/Editor/WorkerTaskCongestionAdvisorMenu.cs`
  - `Scripts/2D/GlobalInit.cs`
  - `Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/task_feature_F016_WorkerTaskCongestionTip.md`
  - `Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/validation_feature_F016.md`
- 新增游戏业务能力：当等待中工人任务达到拥堵阈值时，自动生成任务积压原因与优先级建议，并复用现有 Tip UI 主动提醒玩家。
- 玩家侧效果：玩家能看到“建造/搬运/采集/吃饭/睡觉”等主要积压类型和处理建议，及时暂停扩张、补充资源或调整工人任务开关。
- UI 生成位置：未直接写入 `Game.unity`；未创建新的 `ResourcesLocal` Prefab；复用 `ResourcesLocal/Prefabs/Tip.prefab` 与 `GlobalInit.ShowTip()` 动态显示。
- 开发侧接入方式：自动接入 `GlobalInit.WorkerUpdate()`；也可在 Play Mode 使用 `工具/任务队列拥堵提示/查看拥堵建议` 和 `立即触发一次拥堵 Tip`。
- 验证结果：静态检查通过；运行时代码无编辑器 API 引用；新增 `.meta` 齐全；`Scenes` 和 `ResourcesLocal` 未被写入 F016 内容；Unity 编译和 Play Mode 待人工验证。
- 验证记录路径：`Agent/Reports/2026-05-09/feature_F016_WorkerTaskCongestionTip/validation_feature_F016.md`
- 未完成项：未在 Unity Editor 内执行菜单与 Play Mode 拥堵场景验证。
- 剩余风险：Tip 文本长度、屏幕位置和拥堵阈值体感需要在 Unity Editor / Play Mode 中观察；`git diff --check` 仅提示 `GlobalInit.cs` 未来可能按仓库配置转为 CRLF。
- 是否复用 `Scripts/2D/Tool`：是，复用 `WorkerTaskSummaryTool.GetTaskDisplayName()` 和 F015 快照语义。
- 是否新增或修改 Tool：新增 `Scripts/2D/Tool/WorkerTaskCongestionTool.cs`，用于拥堵等级、主积压类型、建议文案和颜色计算。
- 新增公共工具类或函数路径及用途：`WorkerTaskCongestionTool.BuildReport()`、`GetCongestionLevel()`、`GetPrimaryWaitingSummary()`、`BuildAdviceText()` 供 Tip、Editor 菜单和后续 HUD 复用。
- 是否复用 `Scripts/2D/Enum`：继续复用 `AWorkerTask.WorkerTaskTypeEnum`，没有重复定义任务类型。
- 是否新增或修改 Enum：新增 `Scripts/2D/Enum/WorkerTaskCongestionLevel.cs`，表达 None、Smooth、Busy、Congested、Critical。
- 新增公共枚举路径及用途：`WorkerTaskCongestionLevel` 用于统一 Tip、Editor 菜单和后续任务目标提示中的拥堵状态。
- 是否复用 `Scripts/2D/Constant`：复用 `WorkerTaskHudConstant.MediumWaitingTaskThreshold` 与 `WorkerTaskHudConstant.HighWaitingTaskThreshold`，保持 F015 HUD 和 F016 Tip 阈值一致。
- 是否新增或修改 Constant：新增 `Scripts/2D/Constant/WorkerTaskCongestionConstant.cs`，维护刷新间隔、Tip 冷却、菜单路径、默认文案和严重拥堵阈值。
- 新增公共常量路径及用途：`WorkerTaskCongestionConstant` 供运行时管理器、工具和 Editor 菜单统一引用。
- 后续建议：若 Play Mode 中 Tip 文本过长，可新增常驻“小目标建议条”或将建议接入 F015 HUD；若需要真实调度，另开高风险候选评估任务优先级策略。
- 是否存在未抽取的重复逻辑、重复枚举、重复常量或魔法值：未发现；任务类型继续复用现有枚举，Tip 资源名继续走 `PrefabConstant.TIP` 链路。
