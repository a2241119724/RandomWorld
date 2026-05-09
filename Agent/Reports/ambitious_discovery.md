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
| [DONE] | A006 | 殖民地运营指挥中心（人力状态+任务阻塞诊断+补给目标+建议HUD） | 殖民地管理 / Worker运营反馈 / UI与表现 | F013-F016 已分别完成工人状态、补给缺口、任务队列和拥堵 Tip，但玩家仍缺少统一答案：当前殖民地卡在哪里、为什么任务没人接、下一步该处理食物/床位/材料/人手还是可达性 | 把零散 Worker 运营信息合成一块可见指挥面板，帮助玩家快速定位问题并形成下一步目标 | 为后续任务优先级、教程目标、殖民地事件和运营评分提供统一只读诊断层 | 中高 | 高 | P0 | AINPCAgent + UIAgent + GameplayAgent + ToolAgent | ScriptGenerateSkill + CodeReviewSkill + SceneAnalyzeSkill + EditorToolSkill + TestSkill | Scripts/2D/Enum, Scripts/2D/Constant, Scripts/2D/Tool, Scripts/2D/Gameplay, Scripts/2D/UI, Scripts/2D/Editor, GlobalInit, WorkerTaskManager, Game.unity, ResourcesLocal/Prefabs/UI | **已完成**。任务目录：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/`；任务卡：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/task_ambitious_A006_Colony_Command_Center.md`；验证记录：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/validation_ambitious_A006.md`；回滚方案：`rollback_ambitious_A006.md`。新增 `ColonyCommandAlertLevel`、`WorkerTaskBlockReason`、`ColonyCommandCenterConstant`、`ColonyCommandCenterTool`、`ColonyCommandCenterReport`、`ColonyCommandCenterManager`、`ColonyCommandCenterHUD`、`ColonyCommandCenterMenu` 及 `.meta`；修改 `WorkerTaskManager.cs`、`GlobalInit.cs`。新增能力：人力/任务/补给/拥堵统一指挥报告、等待任务阻塞原因诊断、运行时动态 HUD、F8 显示隐藏、警告 Tip、Game 场景安装菜单、ResourcesLocal Prefab 生成菜单。UI 未直接手写 `Game.unity`，未直接写入 Prefab YAML；采用运行时动态 UI + Editor 菜单。静态验证通过；Unity 编译/Play Mode 待人工复验；回滚方案已记录。 |
| [DONE] | A007 | 成就系统（成就定义+条件检测+解锁弹窗+成就面板+成就点数） | 收集与进度 / UI与表现 | F001-F016 已提供完整战斗统计、收集统计、波次记录、Worker 效率和条件数据，A001 已有体验中枢、A006 已有指挥中心，但玩家缺少跨局的长期成就目标和解锁反馈 | 提供跨局长期目标、解锁成就感、收集驱动力和重玩价值 | 为每日任务、赛季挑战、排行榜和社交分享提供成就数据基础 | 中 | 高 | P0 | GameplayAgent + UIAgent + ItemDataAgent | ScriptGenerateSkill + ConfigGenerateSkill + EditorToolSkill + TestSkill | Scripts/2D/Enum, Scripts/2D/Constant, Scripts/2D/Tool, Scripts/2D/Gameplay, Scripts/2D/UI, Scripts/2D/Editor, Scripts/2D/Character, GlobalInit, Game.unity, ResourcesLocal/Prefabs/UI | **已完成**。任务目录：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/`；任务卡：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/task_ambitious_A007_Achievement_System.md`；验证记录：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/validation_ambitious_A007.md`；新增 `AchievementCategory`、`AchievementState`、`AchievementConstant`、`AchievementTool`、`AchievementData`、`AchievementManager`、`AchievementPopup`、`AchievementPanel`、`AchievementMenu` 及 `.meta`；修改 `GlobalInit.cs`。新增能力：20个预定义成就（战斗×6、收集×3、生存×4、波次×3、工人×3）、实时进度跟踪、解锁弹窗通知、成就浏览面板（F7切换）、成就点数系统、Editor 安装/卸载/验证菜单。UI 采用运行时动态创建（优先级4）+ Editor 菜单辅助；未直接写入 `Game.unity`；未创建 `ResourcesLocal` Prefab。静态验证通过；Unity 编译/Play Mode 待人工复验。Tool 新增 `AchievementTool.cs`（10个可复用方法）、Enum 新增 `AchievementCategory.cs`/`AchievementState.cs`、Constant 新增 `AchievementConstant.cs`。剩余风险：版面布局需在 Unity 中人工调整；成就进度仅内存存储不支持跨会话持久化。 |
| [DONE] | A008 | 主动技能系统（技能冷却+技能效果+技能HUD+技能升级树） | 战斗体验升级 | 玩家只有鼠标点击基础攻击，缺少主动技能释放、冷却管理和技能成长 | 显著丰富战斗操作维度，增加策略深度和操作爽感 | 为技能树、职业、装备附加技能和 PvP 提供技能框架 | 中高 | 高 | P1 | GameplayAgent + AINPCAgent + UIAgent | ScriptGenerateSkill + ConfigGenerateSkill + CodeReviewSkill + TestSkill | Scripts/2D/Character/Player, Scripts/2D/Gameplay, Scripts/2D/UI, Scripts/2D/Enum, Scripts/2D/Constant, Game.unity, ResourcesLocal/Prefabs/UI | **已完成**。任务目录：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/`；任务卡：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/task_ambitious_A008_ActiveSkill_System.md`；验证记录：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/validation_ambitious_A008.md`；新增 `SkillType.cs`、`SkillEffectType.cs`、`SkillConstant.cs`、`SkillTool.cs`、`SkillData.cs`、`SkillManager.cs`、`SkillHUD.cs`、`SkillMenu.cs` 及 `.meta`；修改 `Player.cs`（+39行，HandleSkillInput）、`InputKeyConstant.cs`（+29行，Q/E/R/F快捷键）、`GlobalInit.cs`（+5行，初始化和Tick）。新增能力：4个主动技能（旋风斩AOE/冲刺位移无敌/力量爆发攻击Buff/治疗之光回血）、技能冷却管理、MP消费系统、技能升级（1-5级+升级点数）、Buff计时、运行时动态技能HUD（4按钮栏+冷却覆盖+法力显示+等级+快捷键）、Editor安装/移除/验证菜单。UI生成方式：运行时动态创建独立Canvas `Ambitious_A008_SkillHUD_Canvas`（sortingOrder=80）+Editor菜单辅助；未直接写入 `Game.unity`；未创建 `ResourcesLocal` Prefab。静态验证通过；Unity编译/Play Mode待人工复验。Tool新增 `SkillTool.cs`（10个方法）；Enum新增 `SkillType.cs`/`SkillEffectType.cs`；Constant新增 `SkillConstant.cs` + 修改 `InputKeyConstant.cs`。剩余风险：技能数值/平衡待调优、HUD布局待Unity观察、冲刺方向依赖Animator Direction参数、技能等级无持久化存档。后续建议：接入波次/Boss奖励获取升级点数、添加技能特效/音效、实现技能存档持久化。 |
| [DONE] | A009 | 浮动战斗文字系统（伤害/暴击/治疗/状态文字+动画+颜色分级） | 战斗体验升级 / UI与表现 | 战斗有伤害和暴击数据但缺少即时浮动文字视觉反馈，DamageUI 能力有限 | 显著提升战斗打击感和信息可读性，让每次伤害可见 | 为战斗反馈、BUFF/DEBUFF 提示和战斗教学提供统一文字层 | 中 | 中 | P1 | UIAgent + GameplayAgent | ScriptGenerateSkill + SceneAnalyzeSkill + EditorToolSkill + TestSkill | Scripts/2D/UI, Scripts/2D/Enum, Scripts/2D/Constant, Scripts/2D/Tool, Scripts/2D/Gameplay, Scripts/2D/Editor, Scripts/2D/Character/Character.cs, Scripts/2D/GlobalInit.cs | **已完成**。任务目录：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/`；任务卡：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/task_ambitious_A009_Floating_Combat_Text.md`；验证记录：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/validation_ambitious_A009.md`；新增 `FloatingTextType.cs`、`FloatingTextConstant.cs`、`FloatingTextTool.cs`、`FloatingTextUI.cs`、`FloatingTextManager.cs`、`FloatingTextMenu.cs` 及 `.meta`；修改 `Character.cs`（+4行）、`GlobalInit.cs`（+2行）。新增能力：7种浮动文字类型（伤害/暴击/治疗/连击/经验/闪避/状态）、暴击弹出缩放动画、对象池（30默认/60最大）、世界→屏幕坐标转换、6种公开Spawn接口、Editor安装/移除/验证菜单。UI 生成方式：运行时动态创建 `Ambitious_A009_FloatingText_Canvas`（sortingOrder=100）+ Editor 菜单。静态验证通过；Unity 编译/Play Mode 待人工复验。Tool 新增 `FloatingTextTool.cs`（12个方法）、Enum 新增 `FloatingTextType.cs`、Constant 新增 `FloatingTextConstant.cs`。残馀风险：字体 LegacyRuntime.ttf 高版本兼容性、Canvas sortingOrder 层级冲突、数值/动画参数需在 Unity 中手感调优。 |

## 推荐优先开发

1. **A008 已完成**：主动技能系统已实现4个技能（旋风斩/冲刺/力量爆发/治疗之光）、冷却管理、MP消费、技能升级和运行时技能HUD。
2. **A002 继续推进**：天气生存压力系统仍有采集掉落、天气事件和装备抗性未展开。
3. **A003 继续推进**：工人生存状态闭环仍有自动任务优先级与补给缺口调度未完成。

## 历史已完成候选去重依据

- `feature_discovery.md` 中 F001/F009 已完成战斗统计和连击增益，A001 不重复实现战斗统计，只消费其数据并补齐 HUD/结算展示。
- `feature_discovery.md` 中 F002/F010 已完成波次系统和 Tip 反馈，A001 不改波次生成，只显示波次状态和事件流。
- `feature_discovery.md` 中 F004/F011 已完成结算数据模型和自动触发，A001 不改评分算法，只展示结果面板。
- `feature_discovery.md` 中 F006/F005 已完成收集和 Worker 统计，A001 将这些统计纳入体验中枢但不重复修改统计来源。
- `feature_discovery.md` 中 F012 已完成天气移动、工人效率和灵气恢复影响，A002 本次标记为 `[PARTIAL]`，避免重复实现。
- `feature_discovery.md` 中 F013 已完成工人饥饿/疲劳惩罚、状态工具和 HUD 菜单，A003 本次标记为 `[PARTIAL]`，避免重复实现。
- `feature_discovery.md` 中 F002/F010 只提供波次基础和 Tip，未覆盖 Boss 属性、奖励选择和玩家本局 Buff，A004 本次可安全升级。
- `feature_discovery.md` 中 F013-F016 只提供 Worker 状态、补给、任务队列和拥堵提示的专项能力；A006 不重复实现这些底层能力，而是聚合为指挥中心并补充等待任务阻塞原因诊断。
- A007 不与 A001（体验中枢）重复：A001 提供会话内实时 HUD 和结算面板，A007 提供跨局持久成就目标和条件检测，共享 F001-F016 的数据源但不重复实现统计逻辑。
- A007 不与 F006（物品收集里程碑）重复：F006 是本局收集统计提示，A007 是跨局成就条件、点数和解锁通知。
- A008 不与 A004（波次 Boss 奖励）重复：A004 是波间三选一临时奖励 Buff，A008 是玩家主动释放技能+冷却+成长。

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

### 2026-05-10

- **A008 [DONE] — 主动技能系统**
  - 任务目录：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/`
  - 任务卡：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/task_ambitious_A008_ActiveSkill_System.md`
  - 验证记录：`Agent/Reports/2026-05-10/ambitious_A008_ActiveSkill_System/validation_ambitious_A008.md`
  - 新增文件：`Scripts/2D/Enum/SkillType.cs`、`Scripts/2D/Enum/SkillEffectType.cs`、`Scripts/2D/Constant/SkillConstant.cs`、`Scripts/2D/Tool/SkillTool.cs`、`Scripts/2D/Gameplay/SkillData.cs`、`Scripts/2D/Gameplay/SkillManager.cs`、`Scripts/2D/UI/SkillHUD.cs`、`Scripts/2D/Editor/SkillMenu.cs` 及 `.meta`
  - 修改文件：`Scripts/2D/Character/Player/Player.cs`（+39行）、`Scripts/2D/Constant/InputKeyConstant.cs`（+29行）、`Scripts/2D/GlobalInit.cs`（+5行）
  - 新增能力：4个主动技能（旋风斩AOE/冲刺位移无敌/力量爆发攻击Buff/治疗之光回血）、技能冷却管理、MP消费系统、技能升级（1-5级）、Buff计时、运行时动态技能HUD、Editor安装/移除/验证菜单
  - UI 生成方式：运行时动态创建独立Canvas `Ambitious_A008_SkillHUD_Canvas`（sortingOrder=80）+ Editor菜单辅助；未直接写入 `Game.unity`；未创建 `ResourcesLocal` Prefab
  - Tool：复用 `Tool.IsUIInputActive()`，新增 `SkillTool.cs`（10个公共静态方法）
  - Enum：新增 `SkillType.cs`（5值）、`SkillEffectType.cs`（7值）
  - Constant：新增 `SkillConstant.cs`（7个分组），修改 `InputKeyConstant.cs`（追加4个技能快捷键）
  - 验证结果：静态验证通过（无 UnityEditor 运行时引用、namespace一致、Singleton模式一致）；`.meta` 由Unity生成；Unity编译和Play Mode待人工复验
  - 剩余风险：技能数值平衡待Play Mode调优；HUD布局待Unity观察；技能等级无持久化存档；后续需接入波次奖励获取升级点数

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

- **A007 [DONE] — 成就系统**
  - 任务目录：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/`
  - 任务卡：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/task_ambitious_A007_Achievement_System.md`
  - 验证记录：`Agent/Reports/2026-05-09/ambitious_A007_Achievement_System/validation_ambitious_A007.md`
  - 新增文件：`Scripts/2D/Enum/AchievementCategory.cs`、`Scripts/2D/Enum/AchievementState.cs`、`Scripts/2D/Constant/AchievementConstant.cs`、`Scripts/2D/Tool/AchievementTool.cs`、`Scripts/2D/Gameplay/AchievementData.cs`、`Scripts/2D/Gameplay/AchievementManager.cs`、`Scripts/2D/UI/AchievementPopup.cs`、`Scripts/2D/UI/AchievementPanel.cs`、`Scripts/2D/Editor/AchievementMenu.cs` 及 `.meta`
  - 修改文件：`Scripts/2D/GlobalInit.cs`
  - 新增能力：20个预定义成就（战斗×6、收集×3、生存×4、波次×3、工人×3）、实时进度跟踪、解锁弹窗通知、成就浏览面板（F7切换）、成就点数系统、Editor 安装/卸载/验证菜单
  - UI 生成方式：运行时动态创建（优先级4）+ Editor 菜单辅助；弹窗 Canvas `Ambitious_A007_AchievementPopup_Canvas`（sortingOrder=200）、面板 Canvas `Ambitious_A007_AchievementPanel_Canvas`（sortingOrder=150）
  - Tool：复用 `Tool.IsUIInputActive()`，新增 `AchievementTool.cs`
  - Enum：新增 `AchievementCategory.cs`、`AchievementState.cs`
  - Constant：新增 `AchievementConstant.cs`
  - 验证结果：静态验证通过（无 UnityEditor 运行时引用、.meta 齐全、namespace 一致）；Unity 编译和 Play Mode 待人工复验
  - 剩余风险：面板布局需在 Unity 中人工调整；成就进度仅内存存储不支持跨会话持久化

- **A006 [DONE] — 殖民地运营指挥中心**
  - 任务目录：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/`
  - 任务卡：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/task_ambitious_A006_Colony_Command_Center.md`
  - 验证记录：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/validation_ambitious_A006.md`
  - 回滚方案：`Agent/Reports/2026-05-09/ambitious_A006_Colony_Command_Center/rollback_ambitious_A006.md`
  - 新增文件：`Scripts/2D/Enum/ColonyCommandAlertLevel.cs`、`Scripts/2D/Enum/WorkerTaskBlockReason.cs`、`Scripts/2D/Constant/ColonyCommandCenterConstant.cs`、`Scripts/2D/Tool/ColonyCommandCenterTool.cs`、`Scripts/2D/Gameplay/ColonyCommandCenterReport.cs`、`Scripts/2D/Gameplay/ColonyCommandCenterManager.cs`、`Scripts/2D/UI/ColonyCommandCenterHUD.cs`、`Scripts/2D/Editor/ColonyCommandCenterMenu.cs` 及 `.meta`
  - 修改文件：`Scripts/2D/Character/Worker/WorkerTaskManager.cs`、`Scripts/2D/GlobalInit.cs`
  - 新增能力：殖民地人力/任务/补给/拥堵统一报告、等待任务阻塞原因诊断、警戒等级、行动建议、运行时动态 HUD、F8 显示隐藏、警告 Tip、Editor 场景安装和 ResourcesLocal Prefab 生成菜单
  - UI 生成方式：未直接写入 `Game.unity`，未直接手写 `ResourcesLocal` Prefab；运行时自动动态创建 `Ambitious_A006_ColonyCommandCenter_Canvas` 与 `Ambitious_A006_ColonyCommandCenterHUD_Root`，并提供 Editor 菜单安全落场景/Prefab
  - Tool：复用 `WorkerTaskSummaryTool`、`WorkerTaskCongestionTool`、`WorkerConditionTool`、`WorkerSupplyTool` 和 `Tool.IsUIInputActive()`，新增 `ColonyCommandCenterTool.cs`
  - Enum：复用 Worker 相关枚举，新增 `ColonyCommandAlertLevel.cs`、`WorkerTaskBlockReason.cs`
  - Constant：复用 Worker 状态/补给/队列/拥堵常量，新增 `ColonyCommandCenterConstant.cs`
  - 验证结果：新增 `.meta` 存在；新增运行时代码无 `using UnityEditor`；`Scenes/Game.unity` 无 A006 写入；`git diff --check` 通过但有 LF/CRLF 提醒；命令行环境无 .NET SDK，Unity 编译和 Play Mode 待人工复验
  - 剩余风险：HUD 右上角布局、字号和诊断文案需在 Unity Play Mode 中调优；任务私有字段未来改名时诊断会降级为通用原因

- **A009 [DONE] — 浮动战斗文字系统**
  - 任务目录：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/`
  - 任务卡：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/task_ambitious_A009_Floating_Combat_Text.md`
  - 验证记录：`Agent/Reports/2026-05-09/ambitious_A009_Floating_Combat_Text/validation_ambitious_A009.md`
  - 新增文件：`Scripts/2D/Enum/FloatingTextType.cs`、`Scripts/2D/Constant/FloatingTextConstant.cs`、`Scripts/2D/Tool/FloatingTextTool.cs`、`Scripts/2D/UI/FloatingTextUI.cs`、`Scripts/2D/Gameplay/FloatingTextManager.cs`、`Scripts/2D/Editor/FloatingTextMenu.cs` 及 `.meta`
  - 修改文件：`Scripts/2D/Character/Character.cs`（+4行）、`Scripts/2D/GlobalInit.cs`（+2行）
  - 新增能力：7种浮动文字类型（伤害/暴击/治疗/连击/经验/闪避/状态）、暴击/连击弹出缩放动画、对象池（30默认/60最大）、世界→屏幕坐标转换、6种公开Spawn接口、Editor安装/移除/验证菜单
  - UI 生成方式：运行时动态创建 `Ambitious_A009_FloatingText_Canvas`（sortingOrder=100）+ Editor 菜单；未直接写入 `Game.unity`，未创建 ResourcesLocal Prefab
  - Tool：新增 `FloatingTextTool.cs`（12个公共静态方法）
  - Enum：新增 `FloatingTextType.cs`（7个枚举值）
  - Constant：新增 `FloatingTextConstant.cs`（颜色×7、字号×7、动画参数×8、池配置、节点名、文案、菜单路径）
  - 验证结果：静态验证通过（无 UnityEditor 运行时引用、namespace 一致、Singleton 模式一致）；Unity 编译和 Play Mode 待人工复验
  - 剩余风险：LegacyRuntime.ttf 字体高版本兼容性、Canvas sortingOrder=100 层级冲突、数值/动画参数需在 Play Mode 中调优
