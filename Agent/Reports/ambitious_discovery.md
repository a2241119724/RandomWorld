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
| [TODO] | A002 | 天气驱动的生存压力系统（天气BUFF/DEBUFF+环境HUD+采集/移动影响） | 环境与生存 | WeatherManager 每日随机天气但无玩法影响，EnvironmentManager 温度/湿度/灵气仅显示数据 | 天气从纯视觉变成策略变量，雨雪晴带来不同路线和采集决策 | 激活环境系统，为后续天气事件、灾害、装备抗性提供基础 | 中 | 高 | P1 | GameplayAgent + MapAgent + UIAgent | ScriptGenerateSkill + TestSkill | Scripts/2D/Manager, Scripts/2D/Data, Scripts/2D/Character, Scripts/2D/UI | 候选保留。需谨慎处理移动速度、Worker效率和Photon本地/远端差异 |
| [TODO] | A003 | 工人生存状态闭环（饥饿疲劳惩罚+状态图标+自动任务优先级） | 殖民地管理 / 生存模拟 | Worker 有饥饿和疲劳衰减，低值只影响接任务但缺少明显惩罚和可见反馈 | 让殖民地管理更有压力，玩家能及时救援低状态工人 | 补齐 Worker 状态系统，可复用到医疗、休息、排班 | 中 | 高 | P1 | AINPCAgent + UIAgent | ScriptGenerateSkill + CodeReviewSkill + TestSkill | Scripts/2D/Character/Worker, Scripts/2D/UI/Character, Scripts/2D/UI/Panel | 候选保留。需要避免破坏 Worker 状态机和寻路优先级 |
| [TODO] | A004 | 波次Boss与波间奖励系统（精英波+奖励选择+难度缩放接入） | 关卡与玩法 | WaveManager 已有波次基础，但难度缩放未接入敌人属性，也无Boss/奖励阶段 | 提升关卡节奏、重玩价值和阶段目标 | 扩展波次系统为完整关卡流程模板 | 高 | 高 | P1 | AINPCAgent + GameplayAgent + UIAgent | ScriptGenerateSkill + ConfigGenerateSkill + TestSkill | Scripts/2D/Gameplay, Scripts/2D/Character/Enemy, Scripts/2D/UI, Resources/SO | 候选保留。涉及敌人属性和奖励平衡，需独立任务处理 |
| [SKIPPED] | A005 | Photon实时多人PvP竞技场 | 多人玩法 | 项目存在 Photon，但当前玩法、存档、地图同步偏合作/房间流程，无PvP边界 | 可能带来巨大玩法变化 | 需要重构同步、输入、伤害归属和房间状态 | 极高 | 极高 | P2 | MultiplayerAgent | NetworkSkill | NetworkConnect, Photon设置, Character, Map, UI, Scene | 自动跳过。涉及Photon深度改造、同步权威性和不可控破坏风险，不适合作为本次自动大改候选 |

## 推荐优先开发

1. **A002 — 天气驱动的生存压力系统**：P1，中风险，玩法提升明显，但会触碰角色数值和效率规则。
2. **A003 — 工人生存状态闭环**：P1，中风险，殖民地管理价值高，但会影响 Worker 状态机。
3. **A004 — 波次Boss与波间奖励系统**：P1，高风险，适合后续单独做关卡流程升级。

## 历史已完成候选去重依据

- `feature_discovery.md` 中 F001/F009 已完成战斗统计和连击增益，A001 不重复实现战斗统计，只消费其数据并补齐 HUD/结算展示。
- `feature_discovery.md` 中 F002/F010 已完成波次系统和 Tip 反馈，A001 不改波次生成，只显示波次状态和事件流。
- `feature_discovery.md` 中 F004/F011 已完成结算数据模型和自动触发，A001 不改评分算法，只展示结果面板。
- `feature_discovery.md` 中 F006/F005 已完成收集和 Worker 统计，A001 将这些统计纳入体验中枢但不重复修改统计来源。

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
