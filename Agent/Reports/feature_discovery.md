# 游戏业务功能候选发现报告

> 全局候选功能总表统一维护在 `Agent/Reports/feature_discovery.md`。
> 每次任务完成后，必须回写候选状态、任务路径、修改文件、验证摘要和剩余风险。

## 扫描范围

- `Agent/README.md`
- `Agent/Docs/ImplementationRoadmap.md`
- `Agent/Docs/SkillCatalog.md`
- `Agent/Config/agent_registry.json`
- `Agent/Config/task_router.json`
- `Agent/Templates/agent_task_card.md`
- `Agent/Reports/` 历史任务卡与验证记录
- `Scenes/Game.unity`
- `ResourcesLocal/Prefabs`
- `Scripts/2D/Tool`
- `Scripts/2D/Enum`
- `Scripts/2D/Constant`
- `Scripts/2D/Character`
- `Scripts/2D/Gameplay`
- `Scripts/2D/UI`
- `Scripts/2D/Manager`
- `Scripts/2D/Data`
- `Resources/SO`、`Resources/Images` 只读扫描
- `StreamingAssets`、`AddressableAssetsData` 只读扫描

## 全局候选功能列表

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| [DONE] | F001 | 玩家战斗数据统计与连击反馈系统 | 战斗反馈 | `GameplaySessionStats` 数据未接入战斗事件 | 提升战斗反馈与连击目标感 | 为成就、任务、评分提供数据 | 低 | 中 | P0 | GameplayAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-26/feature_F001_CombatStats/`；修改 `Character.cs`、`Player.cs`、敌人死亡状态和 `GameplayStatsMenu.cs`；静态验证通过，Play Mode 待人工。 |
| [DONE] | F002 | 敌人波次与难度动态缩放系统 | 关卡玩法 | `EnemyManager.GenEnemy()` 固定生成，缺少波次节奏 | 提升关卡挑战和节奏 | 为波次 HUD、通关、评分提供基础 | 中 | 中 | P0 | AINPCAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-27/feature_F002_WaveSystem/`；新增 `WaveManager.cs`、`WaveManagerMenu.cs`，修改 `EnemyManager.cs`；静态验证通过。 |
| [DONE] | F003 | 玩家死亡惩罚与重生延迟系统 | 玩家体验 | `Player.Death()` 只有回血，缺少惩罚和反馈 | 增加生存压力 | 让死亡流程具备扩展点 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-27/feature_F003_DeathPenalty/`；新增 `DeathPenaltyManager.cs`、`DeathMenuPanel.cs`，修改 `Player.cs`、`PrefabConstant.cs`；DeathMenu Prefab 仍需人工创建。 |
| [DONE] | F004 | 会话结束统计数据模型与报告 | 关卡结算 | 有战斗数据但缺少结算模型 | 提升结算反馈和成长感 | 为结算 UI 和数据分析打底 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-28/feature_F004_SessionResult/`；新增 `SessionResultData.cs`、`SessionResultManager.cs`、`SessionResultMenu.cs`；静态验证通过。 |
| [DONE] | F005 | 工人工作效率统计与反馈 | 成长奖励 | Worker 任务完成缺少效率统计 | 帮助玩家理解殖民地运营效率 | 激活 Worker 统计数据 | 低 | 中 | P1 | AINPCAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-05-01/feature_F005_WorkerEfficiency/`；新增 `WorkerEfficiencyTracker.cs`、`WorkerEfficiencyMenu.cs`，修改 `AWorkerTask.cs`、`AWorker.cs`；静态验证通过。 |
| [DONE] | F006 | 玩家物品收集统计与里程碑提示 | 收集反馈 | 物品拾取未激活统计与里程碑 | 提升收集成就感 | 为成就与图鉴打底 | 低 | 低 | P1 | ItemDataAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-28/feature_F006_ItemCollectionMilestone/`；新增 `ItemCollectionTracker.cs`、`ItemCollectionMenu.cs`，修改 `ItemMap.cs`；静态验证通过。 |
| [SKIPPED] | F007 | 战斗评分与关卡星级计算系统（增强版） | 关卡结算 | 基础评分已由 F004 覆盖 | 需要更细关卡目标后才有新增价值 | 避免重复实现评分 | 中 | 中 | P1 | GameplayAgent | ScriptGenerateSkill | 本次跳过：F004 已包含 `CombatScore`、`StarRating`、`GradeText`；增强版需要明确关卡差异化规则，否则边界不清。 |
| [DONE] | F008 | 玩家受击无敌帧保护系统 | 玩家体验 | 玩家可被连续命中瞬间击杀 | 提升操作容错 | 为技能/道具 i-frame 打底 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-28/feature_F008_InvincibilityFrame/`；修改 `Player.cs`；静态验证通过。 |
| [DONE] | F009 | 连击增益奖励系统 | 战斗反馈 | 连击统计没有玩法收益 | 鼓励连续击杀 | 为连击技能、装备和成就打底 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-30/feature_F009_ComboBonus/`；新增 `ComboBonusManager.cs`、`ComboBonusMenu.cs`，修改 `Character.cs`、`Player.cs`；静态验证通过。 |
| [DONE] | F010 | 波次事件反馈与波间提示系统 | 关卡玩法 | `WaveManager` 有事件但没有玩家提示 | 让玩家感知波次节奏 | 补齐波次反馈层 | 低 | 低 | P1 | AINPCAgent + UIAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-30/feature_F010_WaveEventFeedback/`；新增 `WaveEventFeedback.cs`、`WaveEventFeedbackMenu.cs`；静态验证通过。 |
| [DONE] | F011 | 会话结算自动触发与结果接入 | 关卡结算 | `SessionResultManager.CaptureResult()` 未自动调用 | 死亡或通关后自动获得评价 | 补齐结算触发链路 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-04-30/feature_F011_SessionResultAutoTrigger/`；新增 `SessionResultAutoTrigger.cs`、`SessionResultAutoTriggerMenu.cs`，修改 `Player.cs`；静态验证通过。 |
| [DONE] | F012 | 天气环境对玩法的影响系统 | 玩家体验 | 天气和环境数据未影响玩法 | 天气带来策略差异 | 激活环境系统 | 低 | 中 | P2 | GameplayAgent + MapAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/`；新增 `WeatherGameplayTool.cs`、`WeatherGameplayEffect.cs`、`WeatherGameplayHUD.cs`、`WeatherGameplayEffectMenu.cs`，修改天气、环境、移动和任务进度接入点；UI 采用 Editor 菜单生成。 |
| [DONE] | F013 | 工人饥饿疲劳状态效果与视觉反馈 | 成长奖励 | Worker 有 `CurHungry/CurTired` 衰减，但缺少统一状态、效率后果和 HUD | 让工人管理有实际反馈和紧迫感 | 沉淀 Worker 状态枚举、常量、工具、HUD 数据源 | 中 | 中 | P2 | AINPCAgent + GameplayAgent + UIAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-05-09/feature_F013_WorkerCondition/`；任务卡：`task_feature_F013_WorkerCondition.md`；验证：`validation_feature_F013.md`；新增 `WorkerConditionState.cs`、`WorkerConditionConstant.cs`、`WorkerConditionTool.cs`、`WorkerConditionManager.cs`、`WorkerConditionHUD.cs`、`WorkerConditionMenu.cs`；修改 `ASeek.cs`、`AWorkerTask.cs`、`GlobalInit.cs`；UI 未直接写入 `Game.unity`，提供菜单 `工具/工人状态/创建工人状态 HUD 到 Game 场景`；静态验证通过，Unity 编译和 Play Mode 待人工。 |
| [DONE] | F014 | 工人补给缺口提示系统 | 交互提示 | 吃饭/睡觉依赖食物和床位，但缺口缺少玩家提示 | 让玩家知道为什么工人无法恢复 | 与 F013 状态事件联动，形成补给目标提示 | 低 | 中 | P1 | AINPCAgent + UIAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-05-09/feature_F014_WorkerSupplyShortage/`；任务卡：`task_feature_F014_WorkerSupplyShortage.md`；验证：`validation_feature_F014.md`；新增 `WorkerSupplyIssueType.cs`、`WorkerSupplyConstant.cs`、`WorkerSupplyTool.cs`、`WorkerSupplyIssueManager.cs`、`WorkerSupplyHUD.cs`、`WorkerSupplyIssueMenu.cs`；修改 `GlobalInit.cs`；新增只读补给缺口统计，提示食物不足、缺床、饥饿、疲劳和临界停工；UI 未直接写入 `Game.unity`，未创建 `ResourcesLocal` Prefab，提供菜单 `工具/工人补给提示/创建补给缺口 HUD 到 Game 场景`；静态验证通过，Unity 编译和 Play Mode 待人工。 |
| [DONE] | F015 | 任务队列 HUD 摘要 | UI 数据表现 | `WorkerTaskManager.GetTaskInfo()` 主要给 DebugUI 使用，缺少玩家可读任务概览 | 玩家能快速理解当前任务压力 | 复用任务管理器已有统计，低侵入 | 低 | 中 | P2 | UIAgent + AINPCAgent | ScriptGenerateSkill | 任务目录：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/`；任务卡：`task_feature_F015_WorkerTaskQueueHUD.md`；验证：`validation_feature_F015.md`；新增 `WorkerTaskQueueSnapshot.cs`、`WorkerTaskHudConstant.cs`、`WorkerTaskSummaryTool.cs`、`WorkerTaskQueueHUD.cs`、`WorkerTaskQueueHUDMenu.cs`；修改 `WorkerTaskManager.cs`，新增只读任务队列快照和 HUD 摘要接口；UI 未直接写入 `Game.unity`，未创建 `ResourcesLocal` Prefab，提供菜单 `工具/任务队列 HUD/创建任务队列 HUD 到 Game 场景`；静态检查通过，Unity 编译和 Play Mode 待人工。 |
| [TODO] | F016 | 任务队列拥堵 Tip 与优先级建议 | 任务目标提示 | F015 已提供任务队列快照，但等待任务过多时仍缺少主动提醒 | 玩家能在任务积压时及时调整建造、采集、搬运或休息安排 | 复用 F015 快照与压力阈值，补齐主动提示层 | 低 | 低 | P2 | UIAgent + AINPCAgent | ScriptGenerateSkill | 可新增只读提示管理器，基于等待中任务数量和任务类型分布节流显示 Tip，不改变任务优先级和 Worker 调度。 |

## 推荐优先开发

1. F016 任务队列拥堵 Tip 与优先级建议：复用 F015 的任务队列快照和压力阈值，适合低侵入主动提示。

## 被跳过候选及原因

- F007：基础战斗评分、星级和等级文案已经由 F004 实现；增强版需要明确关卡目标差异化规则，本次自动流程不适合继续扩写，标记为 `[SKIPPED]`。

## 历史已完成候选去重依据

- F001、F009 均涉及连击与战斗反馈，但 F001 是统计层，F009 是奖励倍率层，保留为不同候选。
- F004、F011 均涉及结算，但 F004 是数据模型，F011 是自动触发链路，保留为不同候选。
- F005、F013 均涉及 Worker，但 F005 是效率统计，F013 是饥饿/疲劳状态效果，保留为不同候选。
- F013、F014 均涉及 Worker 状态，但 F013 是状态效果与效率后果，F014 是补给缺口解释和 UI 提示，保留为不同候选。
- F005、F015 均涉及 Worker 任务，但 F005 是已完成任务效率统计，F015 是当前任务队列压力展示，保留为不同候选。
- F014、F015 均为 Worker 运营 HUD，但 F014 展示补给缺口，F015 展示任务队列压力，保留为不同候选。
- A001 已有综合体验 HUD，但它聚合会话与结算数据；F015 专注 WorkerTaskManager 当前队列明细，保留为低侵入专项候选。
- F007 与 F004 的基础评分能力重叠，已标记跳过，避免重复开发。

## 已发现可复用公共代码

- `Scripts/2D/Tool/Tool.cs`
  - `GetComponentInChildren<T>()`
  - `IsUIInputActive()`
- `Scripts/2D/Tool/WeatherGameplayTool.cs`
  - `ApplyMultiplier()`
- `Scripts/2D/Tool/WorkerConditionTool.cs`
  - `GetSafeRatio()`
  - `GetState()`
- `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
  - `BuildSnapshot()`
  - `BuildHudText()`
  - `BuildPlainText()`
  - `GetTaskDisplayName()`
- `Scripts/2D/Constant/PrefabConstant.cs`
- `Scripts/2D/Constant/ResourceConstant.cs`
- `Scripts/2D/Constant/TagConstant.cs`
- `Scripts/2D/Constant/LayerConstant.cs`
- `Scripts/2D/Constant/WorkerConditionConstant.cs`
  - `GameSceneName`
  - `FontResourcePath`
- `Scripts/2D/Constant/WorkerTaskHudConstant.cs`
  - `HudRefreshInterval`
  - `HudToggleKey`
  - `MenuRoot`
  - `HudRootName`
- `Scripts/2D/Enum/PackageTypeEnum.cs`
- `Scripts/2D/Enum/WorkerConditionState.cs`
- `AWorkerTask.WorkerTaskTypeEnum`
  - Worker 任务系统现有任务类型枚举，本次 F015 继续复用，未新增重复枚举。

## 本次新增公共代码

- `Scripts/2D/Gameplay/WorkerTaskQueueSnapshot.cs`
  - 统一表达任务总数、等待中数量、进行中数量和任务类型统计，供 HUD、Editor 菜单和后续运营面板复用。
- `Scripts/2D/Constant/WorkerTaskHudConstant.cs`
  - 统一维护 F015 HUD 刷新间隔、热键、菜单路径、节点名、默认文案、压力阈值和 UI 尺寸。
- `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
  - 统一计算任务队列快照、任务类型中文名、压力标签、HUD RichText 文案和 Editor 纯文本摘要。

## 本次完成候选摘要

- 候选 ID：F015
- 最终状态：[DONE]
- 任务目录：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/`
- 任务卡路径：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/task_feature_F015_WorkerTaskQueueHUD.md`
- 验证记录路径：`Agent/Reports/2026-05-09/feature_F015_WorkerTaskQueueHUD/validation_feature_F015.md`
- 修改文件：
  - `Scripts/2D/Character/Worker/WorkerTaskManager.cs`
  - `Scripts/2D/Gameplay/WorkerTaskQueueSnapshot.cs`
  - `Scripts/2D/Constant/WorkerTaskHudConstant.cs`
  - `Scripts/2D/Tool/WorkerTaskSummaryTool.cs`
  - `Scripts/2D/UI/WorkerTaskQueueHUD.cs`
  - `Scripts/2D/Editor/WorkerTaskQueueHUDMenu.cs`
- 新增业务能力：提供工人任务队列只读快照和 HUD 摘要，显示任务总量、等待中数量、进行中数量，以及建造、搬运、采集、吃饭、睡觉等类型分布。
- UI 生成方式：未手写 `Game.unity`，未创建 `ResourcesLocal` Prefab；提供 Editor 菜单 `工具/任务队列 HUD/创建任务队列 HUD 到 Game 场景` 安全生成独立 HUD。
- 验证结果：静态检查通过；新增运行时代码未引用 Editor API；新增脚本和任务卡均有 `.meta`；Unity 编译和 Play Mode 待人工验证。
- Tool 复用：复用 `Tool.GetComponentInChildren<T>()`、`Tool.IsUIInputActive()`。
- Tool 新增：`WorkerTaskSummaryTool.cs`。
- Enum 复用：`AWorkerTask.WorkerTaskTypeEnum`。
- Enum 新增：无，避免重复定义任务类型。
- Constant 复用：`WorkerConditionConstant.GameSceneName`、`WorkerConditionConstant.FontResourcePath`。
- Constant 新增：`WorkerTaskHudConstant.cs`。
- 剩余风险：HUD 场景生成、字体加载、屏幕位置、Canvas 层级和 Play Mode 任务刷新节奏需在 Unity Editor 内验证。
