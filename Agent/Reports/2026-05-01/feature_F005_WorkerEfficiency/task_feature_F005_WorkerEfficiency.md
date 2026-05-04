# Agent Task Card — F005 工人工作效率统计与反馈

## 基本信息

- 任务 ID：feature_F005_WorkerEfficiency
- 候选ID：F005
- 创建时间：2026-05-01
- 提出人：ProjectDirectorAgent（自动发现）
- 当前状态：Running
- 风险等级：Low
- 本次任务目录：Agent/Reports/2026-05-01/feature_F005_WorkerEfficiency/
- 全局候选报告路径：Agent/Reports/feature_discovery.md

## 原始候选

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 |
|---|---|---|---|---|
| [TODO] | F005 | 工人工作效率统计与反馈 | 成长奖励 | Worker 任务系统完整但无效率统计；无生产速率、空闲时间、任务完成率追踪 |

## 用户需求

> 自动发现：Worker 任务系统（Build/Carry/Gather/Eat/Exercise/Wear/Sleep/Plant）完整，任务派发、执行、完成链路已通，但无任何效率反馈。GameplaySessionStats 中 RecordWorkerTaskCompleted/RecordWorkerDeath 方法已实现但从未被调用。需要补齐工人工作效率统计，让玩家了解殖民地运营状态。

## 主 Agent 分析

- 任务分类：gameplay_feature（成长奖励/效率统计）
- 游戏业务类型：成长奖励
- 目标模块：Worker、WorkerTask、GameplaySessionStats
- 主要影响路径：
  - `Scripts/2D/Character/Worker/Task/AWorkerTask.cs` — Finish() 和 Start() 添加统计通知
  - `Scripts/2D/Character/Worker/AWorker.cs` — Death() 添加死亡统计
  - `Scripts/2D/Gameplay/WorkerEfficiencyTracker.cs` — 新增效率追踪器
  - `Scripts/2D/Editor/WorkerEfficiencyMenu.cs` — 新增 Editor 调试菜单
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — 接入已有的 RecordWorkerTaskCompleted
- 不应触碰的路径：
  - `Resources/SO`、`Resources/Tilemap`、`Resources/Images`
  - `Scenes`、`StreamingAssets`、`AddressableAssetsData`
  - `Scripts/2D/Data/`、`Scripts/2D/Manager/ArchiveManager.cs`
  - `Scripts/2D/NetworkConnect.cs`、Photon 同步逻辑

## 子 Agent 分工

| 子 Agent | 职责 | 输入 | 输出 |
|---|---|---|---|
| AINPCAgent | 在 Worker 任务完成/死亡节点接入统计 | AWorkerTask.Finish、AWorker.Death | 修改后的脚本 |
| ToolAgent | 创建 Editor 菜单验证统计 | WorkerEfficiencyTracker.BuildSummaryText() | Editor 菜单项 |

## Skill 调用计划

| Skill | 调用原因 | 输入 | 预期输出 |
|---|---|---|---|
| ScriptGenerateSkill | 生成 WorkerEfficiencyTracker 及 Editor 菜单 | Worker 系统 API | 独立脚本草案 |

## 上下文快照

- 相关脚本：
  - `Scripts/2D/Character/Worker/Task/AWorkerTask.cs` — Start/Finish/Execute/IsCanWork
  - `Scripts/2D/Character/Worker/AWorker.cs` — Worker 基类，含 WorkerData（CurHungry/CurTired/Task）
  - `Scripts/2D/Character/Worker/WorkerManager.cs` — Worker 管理器
  - `Scripts/2D/Character/Worker/WorkerTaskManager.cs` — 任务管理器
  - `Scripts/2D/Gameplay/GameplaySessionStats.cs` — 已有 RecordWorkerTaskCompleted/RecordWorkerDeath（死代码）
- 相关资源：无
- 相关场景：无
- 相关配置：无

## 风险与约束

- 是否涉及 Scene：否
- 是否涉及 Prefab：否
- 是否涉及 ScriptableObject：否
- 是否涉及 AssetBundle/StreamingAssets：否
- 是否涉及存档格式：否
- 是否涉及 Photon/网络同步：否（Worker 效率统计为本地运行时数据）
- 是否需要兼容旧数据：否
- 风险等级：低

## 业务规则说明

1. **任务完成统计**：Worker 每完成一个任务，记录任务类型、完成时间、所属 Worker
2. **工作效率计算**：基于任务耗时（maxProgress）和任务数量计算每分钟完成任务数
3. **任务类型分布**：按 Build/Carry/Gather/Eat/Exercise/Wear/Sleep/Plant 分类统计
4. **Worker 排名**：按完成任务总数排名，可查询最高效 Worker
5. **死亡统计**：Worker 死亡时记录，关联效率数据
6. **全局汇总**：接入 GameplaySessionStats.RecordWorkerTaskCompleted 激活已有死代码
7. **所有统计仅在本地进行，不同步网络，不保存存档**

## 数据流说明

```
AWorkerTask.Start(worker)
  -> WorkerEfficiencyTracker.Instance.RecordTaskStarted(worker, task)

AWorkerTask.Execute(worker) [每帧]
  -> 扣减疲劳值、累计进度
  -> 进度满后调用 Finish(worker)

AWorkerTask.Finish(worker)
  -> WorkerTaskManager.Instance.CompleteTask(task)
  -> WorkerEfficiencyTracker.Instance.RecordTaskCompleted(worker, task)
  -> GameplaySessionStats.Instance.RecordWorkerTaskCompleted(task.TaskType)

AWorker.Death()
  -> WorkerEfficiencyTracker.Instance.RecordWorkerDeath(worker)
  -> GameplaySessionStats.Instance.RecordWorkerDeath()
```

## 执行步骤

1. 新增 `Scripts/2D/Gameplay/WorkerEfficiencyTracker.cs`：效率追踪器核心（Singleton）
2. 新增 `Scripts/2D/Editor/WorkerEfficiencyMenu.cs`：Editor 调试菜单
3. 修改 `AWorkerTask.cs`：在 Start() 和 Finish() 中添加 2 行效率通知
4. 修改 `AWorker.cs`：在 Death() 中添加 1 行死亡通知
5. 验证编译和逻辑正确性

## 验证步骤

1. 编译验证：确认 Unity 编译无错误（本环境无法运行 Unity，通过静态代码检查验证）
2. 静态检查：验证类名、命名空间、方法签名、空引用保护
3. Play Mode 验证：需要人工在 Unity 中进入 Play Mode，分配 Worker 任务后通过 Editor 菜单查看统计

## 回滚方案

- 回滚路径：删除新增的 WorkerEfficiencyTracker.cs 和 WorkerEfficiencyMenu.cs；移除 AWorkerTask.cs 和 AWorker.cs 中的新增加调用行
- 回滚顺序：直接 revert 修改的 2 个文件 + 删除 2 个新增文件
- 需要保留的数据：无
- 回滚后验证：编译通过即可

## 结果区

- 最终状态：[DONE]
- 已完成内容：
  1. 新增 WorkerEfficiencyTracker 核心效率追踪器（Singleton），追踪每个 Worker 的任务完成、任务类型分布、死亡统计
  2. 新增 WorkerEfficiencyMenu Editor 调试菜单（5 项），提供 Play Mode 下的效率数据查看入口
  3. 在 AWorkerTask.Start 中接入 RecordTaskStarted（+1 行）
  4. 在 AWorkerTask.Finish 中接入 RecordTaskCompleted（+1 行），同步激活 GameplaySessionStats.RecordWorkerTaskCompleted（死代码→激活）
  5. 在 AWorker.Death 中接入 RecordWorkerDeath（+1 行），同步激活 GameplaySessionStats.RecordWorkerDeath（死代码→激活）
  6. 提供 WorkerEfficiencyRecord 数据模型，包含任务频率、最常见任务类型、存活状态等查询
- 修改的文件：
  - `Scripts/2D/Character/Worker/Task/AWorkerTask.cs` — Start() +1行、Finish() +1行
  - `Scripts/2D/Character/Worker/AWorker.cs` — Death() +1行
- 新增的文件：
  - `Scripts/2D/Gameplay/WorkerEfficiencyTracker.cs` — 工人效率追踪器核心（~300行）
  - `Scripts/2D/Editor/WorkerEfficiencyMenu.cs` — Editor 调试菜单（~210行）
- 新增的游戏业务能力：
  - **Worker 任务效率追踪**：每个 Worker 每完成任务，自动记录任务类型和效率指标
  - **任务类型分布统计**：按 8 种任务类型（Build/Carry/Gather/Eat/Exercise/Wear/Sleep/Plant）分类统计
  - **Worker 效率排名**：按完成任务总数排名，可查询最高效 Worker
  - **工作效率计算**：基于累计预计任务耗时计算每分钟完成任务数
  - **Worker 死亡统计**：Worker 死亡时自动记录，关联效率数据
  - **死代码激活**：GameplaySessionStats 中 RecordWorkerTaskCompleted 和 RecordWorkerDeath 从死代码变为实时调用
  - **Editor 调试菜单**：5 项菜单（效率摘要/最高效 Worker/Worker 列表/全局任务分布/GameplaySessionStats Worker 统计）
- 玩家侧效果：
  - 殖民地中每个 Worker 的工作成果被自动追踪（完成任务数、工作效率、常用任务类型）
  - Worker 死亡被记录，方便评估殖民地运营风险和 Worker 管理决策
  - 可通过 Editor 菜单实时查看效率报告
- 开发侧接入方式：
  - WorkerEfficiencyTracker.Instance 是全局单例，在任务完成/Worker 死亡节点自动接入
  - TaskCompleted、WorkerDied、WorkerEfficiencyChanged 事件可订阅用于 UI 更新
  - BuildSummaryText() 返回格式化文本，可用于 Debug 或 UI 显示
  - GetAllRecords() 返回所有 Worker 效率记录的数据列表
- 验证结果：静态验证全部通过（9 维度 40+ 检查项），Play Mode 待人工完成
- 验证记录路径：Agent/Reports/2026-05-01/feature_F005_WorkerEfficiency/validation_feature_F005.md
- 未完成项：无
- 剩余风险：
  - Play Mode 端到端验证需人工在 Unity 中完成
  - Worker 效率报告中的工作效率基于 maxProgress 估算（默认 2 秒），实际任务耗时受 Worker 移动时间影响
  - GameplaySessionStats 中的 RecordWorkerDeath 同时被 Player.Death 中的死亡惩罚系统使用，但 AWorker.Death 不走 Player.Death 流程，无冲突
- 后续建议：
  - 可基于 WorkerEfficiencyChanged 事件接入殖民地管理 UI 面板
  - 可实现 Worker 工作状态实时指示器（空闲/工作中/饥饿/死亡）
  - 可扩展追踪 Worker 因饥饿无法接任务的次数和频率
  - 可接入存档系统持久化跨会话效率数据
  - 可增加 Worker 空闲时间占比统计（需追踪 Seek 状态时长）
