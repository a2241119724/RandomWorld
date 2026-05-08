# 游戏业务功能候选发现报告

> 全局候选功能总表，统一维护于 `Agent/Reports/feature_discovery.md`。
> 每次任务完成后，必须回写对应候选的状态和处理说明。

## 扫描范围

- `Scripts/2D/Character/` — 角色、玩家、敌人、工人、状态机
- `Scripts/2D/Item/` — 物品、背包、建造、掉落
- `Scripts/2D/UI/` — UI 面板、HUD、交互提示
- `Scripts/2D/Manager/` — 管理器（资源、存档、天气、协程、日志）
- `Scripts/2D/Gameplay/` — 玩法相关脚本
- `Scripts/2D/Data/` — 数据层
- `Scripts/2D/Editor/` — Editor 工具
- `Agent/Reports/` — 历史任务记录

## 全局候选功能列表

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| [DONE] | F001 | 玩家战斗数据统计与连击反馈系统 | 战斗反馈 | GameplaySessionStats 已完整实现但从未被调用；击杀、伤害、暴击、连击数据已可追踪但无任何接入 | 提升战斗爽感和即时反馈，连击计数展示 | 为后续成就/任务/评分系统提供数据基础 | 低 | 中 | P0 | GameplayAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-26/feature_F001_CombatStats/；任务卡：task_feature_F001_CombatStats.md；验证记录：validation_feature_F001.md；修改文件：Character.cs、Player.cs、CommonEnemyDeadState.cs、SeekEnemyDeadState.cs、GameplayStatsMenu.cs（新增）；新增能力：战斗伤害统计、敌人击杀计数、连击追踪、暴击统计、玩家死亡统计、经验值统计、Editor 会话统计查看；验证：静态验证通过，Play Mode 测试待人工完成；剩余风险：无；后续：可接入 HUD UI 展示实时 combo
| [DONE] | F002 | 敌人波次与难度动态缩放系统 | 关卡玩法 | EnemyManager.GenEnemy() 固定60秒生成，无波次概念，无难度递增 | 提升关卡挑战性和节奏感 | 为关卡设计和难度曲线提供基础 | 中 | 中 | P0 | AINPCAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-27/feature_F002_WaveSystem/；任务卡：task_feature_F002_WaveSystem.md；验证记录：validation_feature_F002.md；修改文件：EnemyManager.cs（最小标志位）；新增文件：WaveManager.cs、WaveManagerMenu.cs；新增能力：波次递增生成、波间休息、生成间隔控制、最大存活限制、难度缩放因子、生命周期事件、Editor 菜单、随机生成位置、玩家死亡容错；验证：静态验证通过，Play Mode 待人工完成；剩余风险：难度缩放因子未接入 EnemyCreator、需人工在 Unity 中完成 Play Mode 验证；后续：接入 EnemyCreator 应用难度缩放、接入 HUD 显示波次信息、在 GlobalInit 中配置自动启动
| [DONE] | F003 | 玩家死亡惩罚与重生延迟系统 | 玩家体验 | Player.Death() 仅设 HP=100，无任何惩罚、延迟或反馈 | 增加生存压力和决策意义 | 使死亡有实际游戏意义 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-27/feature_F003_DeathPenalty/；任务卡：task_feature_F003_DeathPenalty.md；验证记录：validation_feature_F003.md；修改文件：Player.cs、PrefabConstant.cs；新增文件：DeathPenaltyManager.cs、DeathMenuPanel.cs；新增能力：3秒重生延迟、地图随机位置重生、10%经验惩罚、死亡界面（DeathMenuPanel，缺失Prefab时降级Tip）、重生无敌保护、移动/攻击限制、30%HP恢复+MP完全恢复；验证：静态验证通过，Play Mode 待人工完成；剩余风险：DeathMenu Prefab需人工创建；后续：创建DeathMenu Prefab并加入AssetBundle |
| [DONE] | F004 | 会话结束统计数据模型与报告 | 关卡结算 | 关卡流程有胜负结果但缺少统一统计数据；GameplaySessionStats 已有数据但无保存/展示 | 提升结算反馈和成长感 | 为 UI 面板和数据分析提供基础 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-28/feature_F004_SessionResult/；任务卡：task_feature_F004_SessionResult.md；验证记录：validation_feature_F004.md；新增文件：SessionResultData.cs、SessionResultManager.cs、SessionResultMenu.cs；新增能力：会话结算数据模型、多维度战斗评分（0-10000）、星级评价（1-5）、S/A/B/C/D等级评级、结算历史管理（最近20条）、Editor菜单集成（Capture/Show/History/Clear）、OnResultCaptured事件通知；验证：静态验证全部通过，Play Mode 待人工完成；剩余风险：无；后续：接入自动采集（Player.Death/WaveClear）、创建结算UI面板、接入存档
| [DONE] | F005 | 工人工作效率统计与反馈 | 成长奖励 | Worker 任务系统完整但无效率统计；无生产速率、空闲时间、任务完成率追踪；GameplaySessionStats 中 RecordWorkerTaskCompleted/RecordWorkerDeath 死代码从未激活 | 了解殖民地运营状态，感知 Worker 工作效率 | 为殖民地管理 UI 提供数据，激活已有死代码 | 低 | 中 | P1 | AINPCAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-05-01/feature_F005_WorkerEfficiency/；任务卡：task_feature_F005_WorkerEfficiency.md；验证记录：validation_feature_F005.md；修改文件：AWorkerTask.cs（Start +1行、Finish +1行）、AWorker.cs（Death +1行）；新增文件：WorkerEfficiencyTracker.cs、WorkerEfficiencyMenu.cs；新增能力：Worker 任务效率追踪（完成任务数/任务类型分布/工作效率计算）、Worker 效率排名（最高效 Worker 查询）、Worker 死亡统计、死代码激活（GameplaySessionStats.RecordWorkerTaskCompleted/RecordWorkerDeath）、Editor 调试菜单（5项：效率摘要/最高效Worker/Worker列表/全局任务分布/GameplaySessionStats Worker统计）；验证：静态验证全部通过（9维度40+检查项），Play Mode 待人工完成；剩余风险：无；后续：可接入殖民地管理 UI、增加空闲时间占比统计、接入存档
| [DONE] | F006 | 玩家物品收集统计与里程碑提示 | 收集反馈 | 物品拾取无统计；GameplaySessionStats 已有 RecordItemCollected 但从未调用 | 提升收集成就感和目标感 | 为收集类成就提供数据基础 | 低 | 低 | P1 | ItemDataAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-28/feature_F006_ItemCollectionMilestone/；任务卡：task_feature_F006_ItemCollectionMilestone.md；验证记录：validation_feature_F006.md；修改文件：ItemMap.cs（仅 OnTriggerEnter2D 新增调用+变量提取）；新增文件：ItemCollectionTracker.cs、ItemCollectionMenu.cs；新增能力：物品收集统计自动记录、12级里程碑即时反馈（1~10000）、Tip 提示降级保护、MilestoneReached 事件、Editor 调试菜单；验证：静态验证全部通过（7维度），Play Mode 待人工完成；剩余风险：无；后续：可在 Unity 中 Play Mode 验证 Tip 弹窗效果、接入成就系统、接入收集图鉴 UI |
| [TODO] | F007 | 战斗评分与关卡星级计算系统（增强版） | 关卡结算 | 战斗数据已有但无评分逻辑；无星级评定 | 提升关卡重玩价值和目标感 | 为关卡评价 UI 提供数据 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill | **基础评分系统已被 F004 覆盖（CombatScore/StarRating/GradeText）。** 本候选如需实现，应扩展为增强版：关卡特定评价维度（时间限制、无伤奖励、特殊敌人击败）、多关卡独立评分、评分排行榜。当前暂不推荐优先实现，除非有明确的关卡差异化评价需求 |

| [DONE] | F008 | 玩家受击无敌帧保护系统 | 玩家体验 | Player.ReduceHp 无受伤间隔保护，可被连续攻击瞬间击杀 | 防止被多敌人同时攻击秒杀，提升操作容错率 | 提供通用 i-frame 机制，可复用于技能/道具 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-28/feature_F008_InvincibilityFrame/；任务卡：task_feature_F008_InvincibilityFrame.md；验证记录：validation_feature_F008.md；修改文件：Player.cs（仅此一个文件，新增 invincibilityDuration/lastDamageTime 字段、InvincibilityDuration/IsInvincible 属性、ReduceHp 无敌帧检查逻辑）；新增能力：受击无敌帧保护（默认0.5秒）、可配置无敌时长、无敌状态查询；验证：静态验证全部通过（编译、逻辑、空引用、破坏性、边界条件），Play Mode 待人工完成；剩余风险：无；后续：可在 HUD 添加无敌帧视觉提示、可扩展为技能/道具通用 i-frame 接口 |
| [DONE] | F009 | 连击增益奖励系统 | 战斗反馈 | GameplaySessionStats 追踪连击但无任何游戏性收益 | 激励连续击杀，提升战斗深度和爽感 | 为后续技能/装备连击增益提供基础 | 低 | 中 | P1 | GameplayAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-30/feature_F009_ComboBonus/；任务卡：task_feature_F009_ComboBonus.md；验证记录：validation_feature_F009.md；新增文件：ComboBonusManager.cs、ComboBonusMenu.cs；修改文件：Character.cs（ReduceHp +3行连击伤害加成）、Player.cs（AddExperienceValue +3行连击经验加成）；新增能力：6级连击等级配置（1/5/10/20/50/100）、递进式伤害倍率（1.0x~2.0x）和经验倍率（1.0x~5.0x）、连击里程碑即时Tip提示、连击中断检测与反馈、OnComboChanged/OnComboMilestoneReached/OnComboBroken事件、静态倍率查询方法、Editor调试菜单（4项）；验证：静态验证全部通过（35+检查项），Play Mode 待人工完成；剩余风险：无；后续：接入HUD实时连击显示、添加连击特效、接入成就系统 |
| [DONE] | F010 | 波次事件反馈与波间提示系统 | 关卡玩法 | WaveManager 有5个公开事件但零订阅者；波次开始/结束/清空/休息无任何玩家反馈 | 让玩家感知波次节奏，提升关卡沉浸感 | 补齐波次系统缺失的玩家反馈层 | 低 | 低 | P1 | AINPCAgent + UIAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-30/feature_F010_WaveEventFeedback/；任务卡：task_feature_F010_WaveEventFeedback.md；验证记录：validation_feature_F010.md；新增文件：Scripts/2D/Gameplay/WaveEventFeedback.cs、Scripts/2D/Editor/WaveEventFeedbackMenu.cs；修改文件：无（零侵入）；新增能力：5种波次事件即时Tip提示（波次来袭/清除/通关/休息/状态同步）、WaveFeedbackState数据层供HUD查询、OnWaveFeedbackChanged/OnWaveTipRequested公开事件、Editor调试菜单（5项：查看状态/启用/禁用/WaveManager运行时状态/模拟Tip测试）、全依赖降级保护；验证：静态验证全部通过（9维度40+检查项），Play Mode 待人工完成；剩余风险：无；后续：HUD波次状态显示组件、波次特效、通关成就 |
| [DONE] | F011 | 会话结算自动触发与结果接入 | 关卡结算 | SessionResultManager.CaptureResult 从未被自动调用；OnResultCaptured 无订阅者 | 玩家死亡或通关后自动获得战斗评价 | 补齐结算系统缺失的自动触发链路 | 低 | 低 | P1 | GameplayAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-04-30/feature_F011_SessionResultAutoTrigger/；任务卡：task_feature_F011_SessionResultAutoTrigger.md；验证记录：validation_feature_F011.md；新增文件：SessionResultAutoTrigger.cs、SessionResultAutoTriggerMenu.cs；修改文件：Player.cs（Death方法 +1行调用）；新增能力：玩家死亡自动采集结算数据、波次通关自动采集结算数据、结算摘要 Tip 提示、OnAutoCaptureResult 公开事件、Editor 调试菜单（5项）、全依赖降级保护（Trigger未挂载时直连、GlobalInit缺失时降级 Debug.Log）；验证：静态验证全部通过（35+检查项），Play Mode 待人工完成；剩余风险：无；后续：可在 Unity Play Mode 验证实际采集效果、可选将 Trigger 组件挂载到场景 GameObject |
| [DONE] | F012 | 天气环境对玩法的影响系统 | 玩家体验 | WeatherManager 每日随机天气但零玩法影响；EnvironmentManager 温度/湿度/能量数据未被玩法使用 | 天气变化带来玩法差异和策略深度 | 激活已有的环境数据系统 | 低 | 中 | P2 | GameplayAgent + MapAgent | ScriptGenerateSkill | **已完成**。任务目录：Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/；任务卡：task_feature_F012_WeatherGameplayEffect.md；验证记录：validation_feature_F012.md；修改文件：WeatherManager.cs、EnvironmentManager.cs、Player.cs、ASeek.cs、AWorkerTask.cs；新增文件：WeatherGameplayTool.cs、WeatherGameplayEffect.cs、WeatherGameplayHUD.cs、WeatherGameplayEffectMenu.cs；新增业务能力：天气当前状态与事件、晴/雨/雪天气倍率、玩家移动速度影响、工人移动速度影响、工人任务进度影响、环境灵气恢复影响、天气变化 Tip、可选天气 HUD；UI 生成方式：未手写 Game.unity，未创建 ResourcesLocal Prefab，已改用 Editor 菜单 `工具/天气玩法影响/创建天气 HUD 到 Game 场景`；验证：静态检查通过，Unity 编译和 Play Mode 待人工完成；工具类复用：复用 Tool.IsUIInputActive、Tool.GetComponentInChildren，新增 Scripts/2D/Tool/WeatherGameplayTool.cs；具体业务脚本通过 WeatherGameplayEffect 调用 WeatherGameplayTool；剩余风险：天气倍率和 HUD 菜单需 Unity Play Mode 体验验证 |
| [TODO] | F013 | 工人饥饿疲劳状态效果与视觉反馈 | 成长奖励 | Worker 有 hunger/fatigue 衰减但降到0无任何后果；无视觉状态指示 | 让工人管理有实际意义和紧迫感 | 补齐生存模拟缺失的惩罚层 | 中 | 中 | P2 | AINPCAgent | ScriptGenerateSkill | 新增 WorkerConditionManager，饥饿/疲劳归零时触发减速/停工，添加状态图标 |

## 推荐优先开发

1. **F013 — 工人饥饿疲劳状态效果与视觉反馈**（P2，中风险，需评估 Worker 行为修改影响）
2. **F007 — 战斗评分与关卡星级计算系统（增强版）**（P1，基础评分已被 F004 覆盖，需扩展增强版维度）

## 高/中风险候选及跳过原因

- F013 本次未选择：需要触碰 Worker 状态和停工逻辑，风险高于 F012，保留 `[TODO]` 等后续单独评估。
- F007 本次未选择：基础评分已被 F004 覆盖，增强版需要更明确的关卡差异化规则，保留 `[TODO]`。

## 历史任务记录

### 2026-05-09

- **F012 [DONE]** — 天气环境对玩法的影响系统
  - 任务目录：`Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/`
  - 任务卡：`task_feature_F012_WeatherGameplayEffect.md`
  - 验证记录：`validation_feature_F012.md`
  - 修改文件：`Scripts/2D/Manager/WeatherManager.cs`、`Scripts/2D/Data/EnvironmentManager.cs`、`Scripts/2D/Character/Player/Player.cs`、`Scripts/2D/Core/Seek/ASeek.cs`、`Scripts/2D/Character/Worker/Task/AWorkerTask.cs`
  - 新增文件：`Scripts/2D/Tool/WeatherGameplayTool.cs`、`Scripts/2D/Gameplay/WeatherGameplayEffect.cs`、`Scripts/2D/UI/WeatherGameplayHUD.cs`、`Scripts/2D/Editor/WeatherGameplayEffectMenu.cs`
  - 新增能力：WeatherManager 当前天气状态与事件、晴/雨/雪天气倍率、玩家移动速度影响、工人移动速度影响、工人任务进度影响、环境灵气恢复影响、天气变化 Tip、可选天气 HUD 数据源与生成菜单
  - UI 生成方式：未直接写入 `Game.unity`，未创建 `ResourcesLocal` Prefab；已提供 Editor 菜单 `工具/天气玩法影响/创建天气 HUD 到 Game 场景`，用于通过 Unity Editor 安全生成独立 HUD
  - 工具类复用：复用 `Tool.IsUIInputActive()` 和 `Tool.GetComponentInChildren<T>()`；新增 `WeatherGameplayTool` 承载天气倍率、天气中文名、摘要文本和安全倍率计算
  - 具体调用链：`Player`、`ASeek`、`AWorkerTask`、`EnvironmentManager` 读取 `WeatherGameplayEffect`；`WeatherGameplayEffect` 调用 `WeatherGameplayTool` 统一计算公共逻辑
  - 验证结果：静态检查通过；确认运行时代码未引用 `UnityEditor`；`.meta` 文件已创建；Unity 编译和 Play Mode 待人工完成
  - 剩余风险：天气倍率需 Play Mode 手感微调；HUD 菜单需在 Unity Editor 中实际执行验证

### 2026-05-01

- **F005 [DONE]** — 工人工作效率统计与反馈
  - 任务目录：`Agent/Reports/2026-05-01/feature_F005_WorkerEfficiency/`
  - 任务卡：`task_feature_F005_WorkerEfficiency.md`
  - 验证记录：`validation_feature_F005.md`
  - 修改文件：`Scripts/2D/Character/Worker/Task/AWorkerTask.cs`（Start +1行、Finish +1行）、`Scripts/2D/Character/Worker/AWorker.cs`（Death +1行）
  - 新增文件：`Scripts/2D/Gameplay/WorkerEfficiencyTracker.cs`、`Scripts/2D/Editor/WorkerEfficiencyMenu.cs`
  - 新增能力：Worker 任务效率追踪（完成任务数/任务类型分布/工作效率计算）、Worker 效率排名（最高效 Worker 查询）、Worker 死亡统计、死代码激活（GameplaySessionStats.RecordWorkerTaskCompleted/RecordWorkerDeath）、Editor 调试菜单（5项：效率摘要/最高效Worker/Worker列表/全局任务分布/GameplaySessionStats Worker统计）
  - 验证结果：静态验证全部通过（9维度 40+检查项），Play Mode 待人工完成
  - 剩余风险：无（仅新增2个独立文件 + 修改2个已有文件各1-2行，零侵入资源/场景/存档）
  - 后续建议：接入殖民地管理 UI、增加空闲时间占比统计、接入存档系统持久化效率数据

### 2026-04-30

- **F011 [DONE]** — 会话结算自动触发与结果接入
  - 任务目录：`Agent/Reports/2026-04-30/feature_F011_SessionResultAutoTrigger/`
  - 任务卡：`task_feature_F011_SessionResultAutoTrigger.md`
  - 验证记录：`validation_feature_F011.md`
  - 新增文件：`Scripts/2D/Gameplay/SessionResultAutoTrigger.cs`、`Scripts/2D/Editor/SessionResultAutoTriggerMenu.cs`
  - 修改文件：`Scripts/2D/Character/Player/Player.cs`（Death 方法 +1 行调用）
  - 新增能力：玩家死亡自动采集结算数据、波次通关自动采集结算数据、结算摘要 Tip 提示、OnAutoCaptureResult 公开事件、Editor 调试菜单（5 项：状态查看/模拟死亡采集/模拟波次通关采集/最新报告/清空历史）、全依赖降级保护（Trigger 未挂载时直连 SessionResultManager、GlobalInit 缺失时降级 Debug.Log）
  - 验证结果：静态验证全部通过（9 维度 35+ 检查项），Play Mode 待人工完成
  - 剩余风险：无（仅新增 2 个独立文件 + 修改 1 行调用，零侵入资源/场景/存档）
  - 后续建议：在 Unity Play Mode 验证实际采集效果、可选将 SessionResultAutoTrigger 组件挂载到场景 GameObject、接入 HUD 结算 UI

- **F010 [DONE]** — 波次事件反馈与波间提示系统
  - 任务目录：`Agent/Reports/2026-04-30/feature_F010_WaveEventFeedback/`
  - 任务卡：`task_feature_F010_WaveEventFeedback.md`
  - 验证记录：`validation_feature_F010.md`
  - 新增文件：`Scripts/2D/Gameplay/WaveEventFeedback.cs`、`Scripts/2D/Editor/WaveEventFeedbackMenu.cs`
  - 修改文件：无（零侵入）
  - 新增能力：5种波次事件即时 Tip 提示（波次来袭/清除/通关/休息/状态同步）、WaveFeedbackState 数据层供 HUD 查询、OnWaveFeedbackChanged/OnWaveTipRequested 公开事件、Editor 调试菜单（5项：查看状态/启用/禁用/事件订阅详情/模拟Tip测试）、全依赖降级保护（GlobalInit缺失→Debug.Log、WaveManager缺失→静默跳过）
  - 验证结果：静态验证全部通过（9维度 40+检查项），Play Mode 待人工完成
  - 剩余风险：无（仅新增2个独立文件，零侵入资源/场景/存档）
  - 后续建议：HUD波次状态显示组件、波次开始特效、波间准备音效、接入成就系统

- **F009 [DONE]** — 连击增益奖励系统
  - 任务目录：`Agent/Reports/2026-04-30/feature_F009_ComboBonus/`
  - 任务卡：`task_feature_F009_ComboBonus.md`
  - 验证记录：`validation_feature_F009.md`
  - 新增文件：`Scripts/2D/Gameplay/ComboBonusManager.cs`、`Scripts/2D/Editor/ComboBonusMenu.cs`
  - 修改文件：`Scripts/2D/Character/Character.cs`（ReduceHp +3行连击伤害加成）、`Scripts/2D/Character/Player/Player.cs`（AddExperienceValue +3行连击经验加成）
  - 新增能力：6 级连击等级配置表（1/5/10/20/50/100 连击阈值）、递进式伤害倍率（1.0x~2.0x）和经验倍率（1.0x~5.0x）、连击里程碑即时 Tip 反馈、连击中断检测与中断提示、OnComboChanged/OnComboMilestoneReached/OnComboBroken 三大事件、静态倍率查询 API、Editor 调试菜单（4 项，Tools > Combo Bonus >）
  - 验证结果：静态验证全部通过（7 维度 35+ 检查项），Play Mode 待人工完成
  - 剩余风险：无（仅新增 2 个独立文件 + 修改 2 处已有文件各 3 行，零侵入资源/场景/存档）
  - 后续建议：在 Unity Editor Play Mode 验证连击增益实际战斗效果、接入 HUD 实时连击显示、接入成就系统、添加连击特效

### 2026-04-28

- **F006 [DONE]** — 玩家物品收集统计与里程碑提示
  - 任务目录：`Agent/Reports/2026-04-28/feature_F006_ItemCollectionMilestone/`
  - 任务卡：`task_feature_F006_ItemCollectionMilestone.md`
  - 验证记录：`validation_feature_F006.md`
  - 修改文件：`Scripts/2D/Map/ItemMap.cs`（OnTriggerEnter2D 中新增 RecordItemCollected 调用和变量提取）
  - 新增文件：`Scripts/2D/Gameplay/ItemCollectionTracker.cs`、`Scripts/2D/Editor/ItemCollectionMenu.cs`
  - 新增能力：物品收集自动统计（补齐 RecordItemCollected 死代码）、12 级收集里程碑（1~10000）即时 Tip 反馈、MilestoneReached 事件、Editor 调试菜单（Show Stats/Show Milestones/Reset Milestones）
  - 验证结果：静态验证全部通过（7 个维度），Play Mode 待人工完成
  - 剩余风险：无（仅新增 2 个独立文件 + 修改 ItemMap.cs 1 处，零侵入资源/场景/存档）
  - 后续建议：在 Unity 中 Play Mode 验证里程碑 Tip 弹窗效果、接入成就系统、接入收集图鉴 UI

- **F004 [DONE]** — 会话结束统计数据模型与报告
  - 任务目录：`Agent/Reports/2026-04-28/feature_F004_SessionResult/`
  - 任务卡：`task_feature_F004_SessionResult.md`
  - 验证记录：`validation_feature_F004.md`
  - 新增文件：`Scripts/2D/Gameplay/SessionResultData.cs`、`Scripts/2D/Gameplay/SessionResultManager.cs`、`Scripts/2D/Editor/SessionResultMenu.cs`
  - 新增能力：会话结算数据模型（SessionResultData）、多维度加权战斗评分（击杀35%+连击25%+生存20%+效率15%+收集5%）、星级评价（1-5星）、S/A/B/C/D等级评级、结算历史管理（最近20条）、Editor 菜单集成（Capture/Show Latest/Show History/Clear History）、OnResultCaptured 事件通知
  - 验证结果：静态验证全部通过（编译、逻辑、空引用、破坏性、代码风格、边界条件、评分模拟），Play Mode 待人工完成
  - 剩余风险：无（仅新增 3 个独立文件，零侵入，不修改任何已有代码或资源）
  - 后续建议：在 Player.Death/WaveClear 等关键节点接入自动采集、创建结算 UI 面板展示评分和星级、接入存档系统持久化结算数据

- **F008 [DONE]** — 玩家受击无敌帧保护系统
  - 任务目录：`Agent/Reports/2026-04-28/feature_F008_InvincibilityFrame/`
  - 任务卡：`task_feature_F008_InvincibilityFrame.md`
  - 验证记录：`validation_feature_F008.md`
  - 修改文件：`Scripts/2D/Character/Player/Player.cs`（新增 invincibilityDuration/lastDamageTime 字段、InvincibilityDuration/IsInvincible 公开属性、ReduceHp 无敌帧检查逻辑）
  - 新增能力：受击无敌帧保护（默认0.5秒）、可配置无敌时长（设为0禁用）、无敌状态查询（IsInvincible 属性）、与重生保护兼容的优先级检查链
  - 验证结果：静态验证全部通过（编译、逻辑、空引用、破坏性、代码风格、边界条件），Play Mode 待人工完成
  - 剩余风险：无
  - 后续建议：在 HUD 添加无敌帧视觉提示、扩展为技能/道具通用 i-frame 接口

### 2026-04-27

- **F002 [DONE]** — 敌人波次与难度动态缩放系统
  - 任务目录：`Agent/Reports/2026-04-27/feature_F002_WaveSystem/`
  - 任务卡：`task_feature_F002_WaveSystem.md`
  - 验证记录：`validation_feature_F002.md`
  - 修改文件：`Scripts/2D/Character/Enemy/EnemyManager.cs`（新增 IsWaveControlEnabled 静态标志 + GenEnemy 协程跳过逻辑）
  - 新增文件：`Scripts/2D/Gameplay/WaveManager.cs`、`Scripts/2D/Editor/WaveManagerMenu.cs`
  - 新增能力：波次递增敌人生成、波间休息（默认15秒）、波内生成间隔控制、最大同时存活限制、难度缩放因子（基于已完成波次）、波次生命周期事件（OnWaveStart/OnWaveEnd/OnAllWavesCleared/OnRestStart/OnWaveStateChanged）、Editor 菜单集成（Tools > Wave Manager）、随机生成位置支持（TileMap.GenCanReachPos）、玩家死亡容错
  - 验证结果：静态验证通过（命名空间、Unity API、空引用保护、事件安全、协程生命周期、代码风格、不破坏性检查），Play Mode 待人工完成
  - 剩余风险：难度缩放因子仅数值计算未接入 EnemyCreator 实际属性修改、WaveManager 未配置自动启动
  - 后续建议：接入 EnemyCreator.DoCreate 应用难度缩放、在 HUD 显示波次信息、在 GlobalInit 中配置自动启动、扩展 Boss 波次和波间奖励

- **F003 [DONE]** — 玩家死亡惩罚与重生延迟系统
  - 任务目录：`Agent/Reports/2026-04-27/feature_F003_DeathPenalty/`
  - 任务卡：`task_feature_F003_DeathPenalty.md`
  - 验证记录：`validation_feature_F003.md`
  - 修改文件：`Scripts/2D/Character/Player/Player.cs`、`Scripts/2D/Constant/PrefabConstant.cs`
  - 新增文件：`Scripts/2D/Gameplay/DeathPenaltyManager.cs`、`Scripts/2D/UI/Panel/DeathMenuPanel.cs`
  - 新增能力：3秒重生延迟、地图全范围随机可到达位置重生（TileMap.GenCanReachPos）、10%经验值死亡惩罚、死亡界面面板（DeathMenuPanel，遵循ABasePanel模式，缺失Prefab时自动降级为Tip文本提示）、ESC无法关闭死亡界面、重生期间无敌保护（免疫伤害）、移动/攻击行为限制、30%HP恢复+MP完全恢复、降级安全保护（try-catch防止Prefab缺失导致崩溃）
  - 验证结果：静态验证通过（命名空间、Unity API、继承链、空引用保护、降级路径、风格一致性全部确认），Play Mode 测试待人工完成
  - 人工接入：需在 Unity 中创建 "DeathMenu" Prefab（含Countdown/DeathCount Text子对象）并加入 AssetBundle

### 2026-04-26

- **F001 [DONE]** — 玩家战斗数据统计与连击反馈系统
  - 任务目录：`Agent/Reports/2026-04-26/feature_F001_CombatStats/`
  - 任务卡：`task_feature_F001_CombatStats.md`
  - 验证记录：`validation_feature_F001.md`
  - 修改文件：`Character.cs`、`Player.cs`、`CommonEnemyDeadState.cs`、`SeekEnemyDeadState.cs`
  - 新增文件：`Scripts/2D/Editor/GameplayStatsMenu.cs`
  - 新增能力：战斗伤害统计、敌人击杀计数（含类型分组）、连击追踪（4秒超时）、暴击统计、玩家死亡统计（含连击重置）、经验值累计统计、Editor 菜单会话统计查看
  - 验证结果：静态验证通过，Play Mode 测试待人工完成
