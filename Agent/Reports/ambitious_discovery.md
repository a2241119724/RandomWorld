# 游戏体验升级候选发现报告

> 全局候选功能总表，统一维护于 `Agent/Reports/ambitious_discovery.md`。
> 本报告只记录中大型游戏体验升级候选，避免与 `feature_discovery.md` 和 `efficiency_discovery.md` 的低侵入候选混写。

## 扫描范围

- `Agent/README.md`、`Agent/Docs/ImplementationRoadmap.md`、`Agent/Docs/SkillCatalog.md`、`Agent/Config/agent_registry.json`、`Agent/Config/task_router.json`、`Agent/Templates/agent_task_card.md`
- `Agent/Reports/feature_discovery.md`、`Agent/Reports/efficiency_discovery.md`（当前不存在）、历史 `task_*.md` 和 `validation_*.md`
- `Scenes/Game.unity`、`Scenes/Menu.unity`、`Scenes/RigisterOrLogin.unity`
- `ResourcesLocal/Prefabs`、`Resources/SO`、`Resources/Tilemap`、`Resources/Images`
- `Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Character`、`Scripts/2D/Map`、`Scripts/2D/Item`、`Scripts/2D/Manager`、`Scripts/2D/Data`

## 全局候选功能列表

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 预计影响范围 | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| [DONE] | A001 | 沉浸式会话体验中枢（实时HUD+事件流+结算面板+可生成Prefab工具） | UI与表现 / 战斗反馈 / 关卡结算 | F001/F002/F004/F009/F010/F011 已完成底层统计、波次、连击、自动结算，但历史任务多次留下“接入HUD/结算UI/创建Prefab”的后续建议；Game.unity 已有复杂Canvas，手写Scene YAML风险高 | 把击杀、连击、波次、经验、收集、评分和星级转成持续可见反馈，显著提升战斗爽感、目标感和结算成就感 | 为后续成就、任务、奖励、关卡评级和主HUD提供统一可复用的展示层 | 中 | 高 | P0 | UIAgent + GameplayAgent + SceneAgent | ScriptGenerateSkill + SceneAnalyzeSkill + EditorToolSkill + TestSkill | Scripts/2D/UI/Panel/PanelUI/ForegroundUI, Scripts/2D/Editor, Game.unity, ResourcesLocal/Prefabs/UI, Agent/Reports | **已完成**。任务目录：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/`；任务卡：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/task_ambitious_A001_Experience_Hub.md`；验证记录：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/validation_ambitious_A001.md`；新增文件：`AmbitiousExperienceHub.cs`、`AmbitiousExperienceHubInstaller.cs` 及对应 `.meta`、任务卡、验证记录、回滚方案；新增能力：运行时自动 HUD、事件流、结算面板、评分预览、Game 场景安装工具、ResourcesLocal Prefab 生成工具；UI 生成方式：未直接写入 `Game.unity`，未手写 Prefab，默认运行时动态创建，另提供 Editor 菜单生成；验证：静态验证通过，Unity 编译/Play Mode 待人工环境验证；回滚方案已静态验证；剩余风险：需在 Unity 中观察排序、字体和布局 |
| [PARTIAL] | A002 | 天气驱动的生存压力系统（天气BUFF/DEBUFF+环境HUD+采集/移动影响） | 环境与生存 | WeatherManager 每日随机天气但无玩法影响，EnvironmentManager 温度/湿度/灵气仅显示数据 | 天气从纯视觉变成策略变量，雨雪晴带来不同路线和采集决策 | 激活环境系统，为后续天气事件、灾害、装备抗性提供基础 | 中 | 高 | P1 | GameplayAgent + MapAgent + UIAgent | ScriptGenerateSkill + TestSkill | Scripts/2D/Manager, Scripts/2D/Data, Scripts/2D/Character, Scripts/2D/UI | **部分覆盖**。普通功能 `F012` 已完成天气对玩家移动、工人移动、工人任务进度和灵气恢复的影响，并提供 Editor 菜单生成天气 HUD：`Agent/Reports/2026-05-09/feature_F012_WeatherGameplayEffect/`。采集掉落、天气事件和装备抗性仍未展开，故保持 `[PARTIAL]` 去重，不作为本次候选。 |
| [PARTIAL] | A003 | 工人生存状态闭环（饥饿疲劳惩罚+状态图标+自动任务优先级） | 殖民地管理 / 生存模拟 | Worker 有饥饿和疲劳衰减，低值只影响接任务但缺少明显惩罚和可见反馈 | 让殖民地管理更有压力，玩家能及时救援低状态工人 | 补齐 Worker 状态系统，可复用到医疗、休息、排班 | 中 | 高 | P1 | AINPCAgent + UIAgent | ScriptGenerateSkill + CodeReviewSkill + TestSkill | Scripts/2D/Character/Worker, Scripts/2D/UI/Character, Scripts/2D/UI/Panel | **部分覆盖**。普通功能 `F013` 已完成饥饿/疲劳状态枚举、常量、工具、倍率惩罚、Tip 和 HUD 菜单：`Agent/Reports/2026-05-09/feature_F013_WorkerCondition/`。自动任务优先级与补给缺口调度仍未完成，故保持 `[PARTIAL]` 去重，不作为本次候选。 |
| [DONE] | A004 | 波次Boss与波间奖励系统（精英波+奖励选择+难度缩放接入） | 关卡与玩法 | WaveManager 已有波次基础，但难度缩放未接入敌人属性，也无Boss/奖励阶段 | 提升关卡节奏、重玩价值和阶段目标 | 扩展波次系统为完整关卡流程模板 | 高 | 高 | P1 | AINPCAgent + GameplayAgent + UIAgent | ScriptGenerateSkill + ConfigGenerateSkill + TestSkill | Scripts/2D/Gameplay, Scripts/2D/Character/Enemy, Scripts/2D/UI, Resources/SO | **已完成**。任务目录：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/`；任务卡：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/task_ambitious_A004_Wave_Boss_Rewards.md`；验证记录：`validation_ambitious_A004.md`；新增 `WavePhaseType`、`WaveRewardType`、`WaveBossRewardConstant`、`WaveBossRewardTool`、`WaveBossRewardManager`、`WaveBossRewardPanel`、`WaveBossRewardMenu` 及 `.meta`；修改 `WaveManager.cs`、`Character.cs`、`Player.cs`。新增能力：每 3 波 Boss、敌人难度缩放、Boss 视觉/属性强化、波间三选一奖励、玩家本局伤害/减伤/移动 Buff、运行时动态奖励面板、Game 场景 Editor 安装/移除菜单。UI 未直接手写 `Game.unity`，未创建 `ResourcesLocal` Prefab；采用 Editor 菜单 + 运行时动态 Canvas。静态验证通过；Unity 编译/Play Mode 待人工复验；回滚方案已记录。 |
| [SKIPPED] | A005 | Photon实时多人PvP竞技场 | 多人玩法 | 项目存在 Photon，但当前玩法、存档、地图同步偏合作/房间流程，无PvP边界 | 可能带来巨大玩法变化 | 需要重构同步、输入、伤害归属和房间状态 | 极高 | 极高 | P2 | MultiplayerAgent | NetworkSkill | NetworkConnect, Photon设置, Character, Map, UI, Scene | 自动跳过。涉及Photon深度改造、同步权威性和不可控破坏风险，不适合作为本次自动大改候选 |

## 推荐优先开发

1. **后续 A002 增强**：天气采集掉落、天气事件和装备抗性仍有扩展价值，但需在 F012 基础上独立评估。
2. **后续 A003 增强**：工人补给缺口、自动吃饭/睡觉优先级和床位/食物引导仍有扩展价值。
3. **A004 已完成**：Boss 波、难度缩放和波间奖励已作为本次中大型体验升级落地。

## 历史已完成候选去重依据

- `feature_discovery.md` 中 F001/F009 已完成战斗统计和连击增益，A001 不重复实现战斗统计，只消费其数据并补齐 HUD/结算展示。
- `feature_discovery.md` 中 F002/F010 已完成波次系统和 Tip 反馈，A001 不改波次生成，只显示波次状态和事件流。
- `feature_discovery.md` 中 F004/F011 已完成结算数据模型和自动触发，A001 不改评分算法，只展示结果面板。
- `feature_discovery.md` 中 F006/F005 已完成收集和 Worker 统计，A001 将这些统计纳入体验中枢但不重复修改统计来源。
- `feature_discovery.md` 中 F012 已完成天气移动、工人效率和灵气恢复影响，A002 本次标记为 `[PARTIAL]`，避免重复实现。
- `feature_discovery.md` 中 F013 已完成工人饥饿/疲劳惩罚、状态工具和 HUD 菜单，A003 本次标记为 `[PARTIAL]`，避免重复实现。
- `feature_discovery.md` 中 F002/F010 只提供波次基础和 Tip，未覆盖 Boss 属性、奖励选择和玩家本局 Buff，A004 本次可安全升级。

## 跳过候选

- A005：Photon PvP 改造涉及同步权威性、地图状态、伤害归属和房间流程，当前边界不可控，自动标记为 `[SKIPPED]`。

## 历史任务记录

### 2026-05-07

- **A001 [DONE] — 沉浸式会话体验中枢**
  - 任务目录：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/`
  - 任务卡：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/task_ambitious_A001_Experience_Hub.md`
  - 验证记录：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/validation_ambitious_A001.md`
  - 回滚方案：`Agent/Reports/2026-05-07/ambitious_A001_Experience_Hub/rollback_ambitious_A001.md`
  - 新增文件：`Scripts/2D/UI/Panel/PanelUI/ForegroundUI/AmbitiousExperienceHub.cs`、`Scripts/2D/Editor/AmbitiousExperienceHubInstaller.cs` 及 `.meta`
  - 新增能力：实时 HUD、玩家状态条补充、波次状态、评分预览、事件流、自动结算面板、Game 场景安装菜单、ResourcesLocal Prefab 生成菜单
  - UI 生成方式：默认运行时动态创建独立 Canvas；未直接手写 `Game.unity` 或 Prefab YAML；Editor 菜单可按需生成场景节点或 Prefab
  - 验证结果：静态验证通过；`git diff --check` 通过；`Scenes/Game.unity`、`ResourcesLocal`、既有核心 UI 文件无 diff；Unity 编译和 Play Mode 待人工环境验证
  - 剩余风险：需在 Unity 中观察 Canvas 排序、默认字体和实际布局

### 2026-05-09

- **A004 [DONE] — 波次Boss与波间奖励系统**
  - 任务目录：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/`
  - 任务卡：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/task_ambitious_A004_Wave_Boss_Rewards.md`
  - 验证记录：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/validation_ambitious_A004.md`
  - 回滚方案：`Agent/Reports/2026-05-09/ambitious_A004_Wave_Boss_Rewards/rollback_ambitious_A004.md`
  - 新增文件：`Scripts/2D/Enum/WavePhaseType.cs`、`Scripts/2D/Enum/WaveRewardType.cs`、`Scripts/2D/Constant/WaveBossRewardConstant.cs`、`Scripts/2D/Tool/WaveBossRewardTool.cs`、`Scripts/2D/Gameplay/WaveBossRewardManager.cs`、`Scripts/2D/UI/WaveBossRewardPanel.cs`、`Scripts/2D/Editor/WaveBossRewardMenu.cs` 及 `.meta`
  - 修改文件：`Scripts/2D/Gameplay/WaveManager.cs`、`Scripts/2D/Character/Character.cs`、`Scripts/2D/Character/Player/Player.cs`
  - 新增能力：Boss 波、普通敌人难度缩放、Boss 属性/视觉强化、波间三选一奖励、玩家本局伤害/减伤/移动奖励、运行时动态奖励面板、Editor 菜单安装和移除 UI
  - UI 生成方式：未直接写入 `Game.unity`，未手写 `ResourcesLocal` Prefab；提供菜单 `工具/智能体/波次Boss奖励/创建奖励面板到 Game 场景`，并在奖励出现时运行时动态创建独立 Canvas
  - Tool：复用 `WeatherGameplayTool.ApplyMultiplier()` 与 `Tool.IsUIInputActive()`，新增 `WaveBossRewardTool.cs`
  - Enum：新增 `WavePhaseType.cs`、`WaveRewardType.cs`
  - Constant：新增 `WaveBossRewardConstant.cs`
  - 验证结果：`.meta` 存在、运行时代码无新增 `UnityEditor` 引用、`git diff --check` 通过但有 CRLF 提醒；命令行环境无 .NET SDK，Unity 编译和 Play Mode 待人工复验
  - 剩余风险：Boss 数值、奖励上限和 UI 尺寸需在 Unity Play Mode 中手感调优
