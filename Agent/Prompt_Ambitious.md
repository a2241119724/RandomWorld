请基于 ./Agent 中的 Agent 体系，自动完成一次“发现游戏体验升级机会 -> 选择最具改造价值的候选 -> 生成任务卡 -> 开发大规模新功能 -> 验证记录 -> 更新候选完成状态”的完整流程。

本次任务目标：
- 自动为当前 Unity 游戏项目发现一个能显著提升游戏体验的中大型新功能。
- 自动选择一个中等风险或高风险、有明确边界、能显著丰富游戏玩法或提升玩家体验的候选。
- 允许对项目进行较大幅度的改动，包括新增多个关联脚本、修改 Scene/Prefab/ScriptableObject（需有回滚方案）、新增 UI 组件、扩展游戏系统等。
- 一次只开发一个新功能，但该功能可以包含多个关联的子模块。
- 允许跨 Scripts/2D、Resources、ResourcesLocal、Prefab、Scene 等路径进行协调修改。
- 如果本次功能包含 UI、HUD、弹窗、面板、浮动文字、引导提示、结算界面、奖励界面、成长界面、关卡选择界面等表现内容，应尽量生成可直接在 Unity 中使用的 UI 资源，而不是只写纯代码或说明。

重要要求：
- 不要向我提问。
- 不要等待我确认。
- 如果遇到边界完全不可控、可能完全破坏项目可运行性的候选，则自动跳过它，选择下一个有明确边界的候选。
- 一次只实现一个游戏体验升级功能（可以包含多个子模块）。
- 候选功能总表必须统一维护在：
  - Agent/Reports/ambitious_discovery.md
- 不要在每个任务目录下重复创建 ambitious_discovery.md。
- 每次任务必须在 Agent/Reports/<今天日期>/ 下创建独立任务文件夹，任务卡、验证记录、回滚方案、补充报告等本次任务相关输出都必须放入该文件夹中，避免和同一天其他任务混在一起。
- 游戏体验升级任务的输出目录必须使用固定前缀 `ambitious_`，用于和 Prompt_Feature.md 的 `feature_` 以及 Prompt_Efficiency.md 的 `efficiency_` 输出隔离，避免同一天同候选ID或同短名任务发生路径冲突。
- 独立任务文件夹命名格式建议为：
  - Agent/Reports/<今天日期>/ambitious_<候选ID>_<功能名安全短名>/
  - 如果在选择候选前无法确定候选ID，则先使用：
    - Agent/Reports/<今天日期>/ambitious_run_<HHmmss>/
    - 选定候选后，可继续使用该目录，也可重命名为 `ambitious_<候选ID>_<功能名安全短名>`。
- 同一天多次执行时，不得覆盖已有任务目录；如目录已存在，自动追加时间戳或序号。
- 优先选择能显著改变游戏体验、丰富玩法深度、提升玩家沉浸感的功能。
- 优先选择“业务逻辑完整闭环 + UI/资源可见接入”的中大型功能。
- 如果候选包含 UI 表现层，不要只实现数据层或管理器；应优先尝试把 UI 直接落到 `Game.unity` 或 `ResourcesLocal` 预制体中。
- 允许修改 Scene、Prefab、ScriptableObject、StreamingAssets，但必须满足以下安全条件：
  - 修改前必须在任务卡中记录当前状态和回滚方案。
  - Unity 资源修改必须保留或同步更新 `.meta` 文件。
  - 对 Scene 的修改应尽量通过新增 GameObject、Canvas 子节点或挂载新脚本的方式实现，避免修改已有节点的核心属性。
  - 对 Prefab 的修改应尽量通过新增组件、子节点或独立新 Prefab 的方式实现，避免破坏已有 Prefab 变体层级。
  - 对 ScriptableObject 的修改应保留已有字段值，仅追加新字段或新实例。
  - 对 StreamingAssets 的修改必须是纯新增文件，不覆盖已有配置。
  - 涉及 Photon 同步逻辑或 AssetBundle 配置的修改仍属高风险区域，需特别谨慎并在回滚方案中明确保护措施。
- 如果所有候选都属于不可控高风险，则降级为 Prompt_Feature.md 风格的低侵入实现，并在任务卡中明确降级原因。
- 生成候选功能列表时，必须为每个候选分配唯一编号（前缀 A，如 A001、A002）和完成状态，便于区分 feature_ 和 efficiency_ 的候选。
- 完成功能实现与验证后，必须回写 Agent/Reports/ambitious_discovery.md 中对应候选的状态标记。
- 扫描历史记录时，必须递归检查 Agent/Reports/ 下所有日期目录及其子任务目录中的 task_*.md 和 validation_*.md。
- 同时必须读取 Agent/Reports/ambitious_discovery.md，避免重复实现已经 `[DONE]` 的候选。
- 如果历史任务目录中遗留存在旧版 ambitious_discovery.md，也需要兼容读取，但新的候选总表只允许写入 Agent/Reports/ambitious_discovery.md。
- **所有生成的代码注释必须使用中文。**

## UI / Scene / Prefab 生成规则

当游戏体验升级功能包含 UI 相关内容时，包括但不限于：
- 完整 HUD
- 血量条、技能冷却、连击计数、波次进度
- 击杀反馈、伤害数字、浮动文字
- 结算面板、星级面板、奖励面板
- 技能升级界面、成长树界面、装备界面
- 任务面板、成就面板、图鉴面板
- 新手引导提示、交互提示、失败原因提示
- 主菜单、关卡选择、设置、商店、角色配置界面

必须优先按照以下顺序实现：

### 1. 优先在 Game.unity 场景中生成 UI

如果项目中存在 `Game.unity` 场景，应优先尝试在该场景中生成本功能需要的 UI 内容。

要求：
- 先在项目中搜索 `Game.unity` 的真实路径，不要凭空假定路径。
- 常见路径可能包括但不限于：
  - Assets/Scenes/Game.unity
  - Assets/Game.unity
  - Assets/Resources/Scenes/Game.unity
  - 其他项目实际使用路径
- 如果可以安全修改 `Game.unity`，应优先在该场景中新增独立 UI 根节点，例如：
  - Canvas
  - EventSystem
  - Ambitious_<候选ID>_<功能名>_Root
  - HUD_Root
  - SettlementPanel
  - RewardPopup
  - SkillTreePanel
  - DamageTextLayer
  - GuideTipLayer
- 如果场景中已经存在 Canvas，应优先在已有 Canvas 下新增独立子节点。
- 如果场景中不存在 Canvas，可新增一个独立 Canvas，但必须避免影响已有摄像机、渲染顺序、输入系统和场景流程。
- 新增 UI 节点必须尽量独立，不得破坏已有 UI 层级、已有引用、已有脚本绑定和已有对象命名。
- 新增 UI 对象命名必须带有 `Ambitious_`、候选ID或功能短名，便于定位和回滚。
- 如果需要挂载脚本，应只挂载本次新增脚本或明确兼容的已有脚本。
- 可以新增 UI Binder、UI Controller、ViewModel、Runtime Manager 等脚本，但应保持 UI 表现层与业务逻辑解耦。
- 不得直接修改已有核心 UI 节点字段，除非该修改是必要的、边界清晰的，并且必须在任务卡中说明原因和回滚方式。
- 如果无法确认 Unity Scene YAML 可安全修改，不要直接手写大段 Scene YAML，应改为生成 Editor 菜单工具或 ResourcesLocal 预制体方案。

### 2. 其次在 ResourcesLocal 下创建 UI 预制体

如果无法安全修改 `Game.unity`，或者项目更适合通过预制体接入 UI，则应在 `ResourcesLocal` 下创建对应位置的 UI 预制体。

优先路径建议：
- Assets/ResourcesLocal/UI/<功能名安全短名>/
- Assets/ResourcesLocal/Prefabs/UI/<功能名安全短名>/
- Assets/ResourcesLocal/HUD/<功能名安全短名>/
- Assets/ResourcesLocal/Popup/<功能名安全短名>/
- Assets/ResourcesLocal/Panels/<功能名安全短名>/
- Assets/ResourcesLocal/<已有项目约定目录>/<功能名安全短名>/

要求：
- 创建独立 UI Prefab，例如：
  - Ambitious_<候选ID>_<功能名安全短名>HUD.prefab
  - Ambitious_<候选ID>_<功能名安全短名>Panel.prefab
  - Ambitious_<候选ID>_<功能名安全短名>Popup.prefab
  - Ambitious_<候选ID>_<功能名安全短名>SettlementView.prefab
  - Ambitious_<候选ID>_<功能名安全短名>SkillTreeView.prefab
- 如果项目已有 ResourcesLocal 的 UI/Prefab 目录规范，必须优先遵循已有规范。
- 新增 Prefab 必须保留 `.meta` 文件。
- 不得覆盖已有 Prefab。
- Prefab 应尽量包含完整 UI 层级、默认文案、基础样式、挂载脚本和必要组件。
- Prefab 需要的运行时脚本应放在合适路径，例如：
  - Scripts/2D/UI/
  - Scripts/2D/Gameplay/
  - Scripts/2D/Player/
  - Scripts/2D/Level/
  - Scripts/2D/Rewards/
  - Scripts/2D/Progression/
- 如果 Prefab 需要图片、字体、材质等引用，但无法安全判断项目资源，应使用 Unity 默认 UI 组件或已有可安全引用资源。
- 如果无法生成真实 Prefab 文件，应改为生成 Editor 菜单工具，由开发者在 Unity 中点击菜单后自动创建 Prefab。

### 3. 再次生成 Editor 菜单工具

如果不能直接安全修改 `Game.unity`，也不能可靠创建 Prefab，应优先生成 Editor 菜单工具。

菜单路径建议：
- Tools/Agent/Ambitious/Create <功能名> UI In Game Scene
- Tools/Agent/Ambitious/Create <功能名> Prefab In ResourcesLocal
- Tools/Agent/Ambitious/Install <功能名> System
- Tools/Agent/Ambitious/Rebuild <功能名> UI

Editor 工具要求：
- 放在 Editor 目录或项目已有 Editor 工具目录下。
- 菜单命名必须清晰。
- 工具执行时应检查 `Game.unity` 是否存在。
- 工具执行时应检查目标目录是否存在，不存在则自动创建。
- 工具不得覆盖已有 Prefab 或已有场景对象。
- 工具应尽量支持重复执行时安全退出或生成带序号的新对象。
- 工具生成的 UI 节点、Prefab、配置文件必须有清晰命名和回滚说明。
- 工具代码注释必须使用中文。

### 4. 然后使用运行时代码动态创建 UI

如果 Editor 工具也不可行，可新增运行时代码动态创建 UI。

要求：
- 新增运行时 UI 管理器或可选挂载组件。
- 运行时自动检查 Canvas 和 EventSystem。
- 如果不存在必要 UI 根节点，可动态创建独立节点。
- 动态创建的 UI 必须使用独立命名，避免污染已有 UI。
- UI 默认状态应可控，避免一进入游戏就破坏原有流程。
- 如果是调试型或示例型 UI，应提供开关，避免影响正式游戏体验。
- 必须说明接入方式和风险边界。

### 5. 最后才退回纯代码或说明

只有在以下情况才允许把 UI 无法生成的部分退回到代码或说明中：
- 找不到 `Game.unity`。
- 无法安全修改 `Game.unity`。
- 无法确认 Unity Scene YAML / Prefab YAML 的结构。
- 当前环境无法运行 Unity Editor，也无法可靠生成 Prefab。
- 项目缺少必要 UI 包或组件。
- 资源路径、字体、图片、Canvas 结构无法确定。
- 自动修改可能破坏已有资源引用。

退回代码实现时，必须在任务卡中明确写出：
- 为什么没有直接生成到 `Game.unity`。
- 为什么没有创建 `ResourcesLocal` 预制体。
- 是否提供了 Editor 菜单工具。
- 是否提供了运行时代码动态创建 UI。
- 哪些 UI 部分仍需人工接入。
- 后续应挂载到哪个场景对象、Canvas 或 Prefab 下。
- 如何验证 UI 接入是否成功。

### UI 实现优先级总结

UI 相关游戏体验升级功能的实现优先级必须是：

1. 能安全修改 `Game.unity` 时，优先在 `Game.unity` 中创建独立 UI 节点。
2. 不能安全改 `Game.unity` 时，优先在 `ResourcesLocal` 下创建独立 UI Prefab。
3. 不能直接创建 Prefab 时，生成 Editor 菜单工具，用于在 Unity 中自动创建 UI 或 Prefab。
4. 以上都不可行时，才用运行时代码动态创建 UI。
5. 最后才允许只写业务逻辑、ViewModel、数据层或人工接入说明。

如果选择了第 3、4、5 种方式，必须在任务卡、回滚方案和验证记录中写明原因。

## 候选状态规则

- `[TODO]`：待处理，尚未实现。
- `[DONE]`：已完成，已实现并完成可行验证。
- `[SKIPPED]`：已跳过，通常因为边界不可控、破坏性过大或涉及不可逆修改。
- `[BLOCKED]`：受阻，已分析但因缺少环境、依赖、权限或无法验证而未完成。
- `[PARTIAL]`：部分完成，已实现核心能力，但仍存在明确未完成项（如 UI 接入、资源微调）。

候选功能列表格式必须包含：

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 预计影响范围 | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

示例：

| [TODO] | A001 | 完整击杀奖励反馈系统（连击HUD+击杀弹窗+经验浮动+收集统计） | 战斗与奖励反馈 | 战斗脚本有击杀事件但缺少完整的多层奖励反馈链路 | 大幅提升战斗爽感、即时反馈和成长获得感 | 可复用为后续成就、任务、排行榜的数据基础 | 中 | 高 | P0 | GameplayFeatureAgent | RuntimeFeatureSkill | Scripts/2D/Gameplay, Scripts/2D/UI, Game.unity, ResourcesLocal/UI | 推荐优先实现，优先在 Game.unity 生成 HUD 和击杀反馈 UI；无法安全修改场景时创建 ResourcesLocal UI Prefab |
| [TODO] | A002 | 关卡流程完整重构（波次→Boss→结算→星级评定） | 关卡与玩法 | 关卡有胜负判定但缺少波次递进、Boss阶段和星级反馈 | 大幅提升关卡节奏感和重玩价值 | 为后续关卡编辑器提供标准流程模板 | 高 | 高 | P0 | LevelFeatureAgent | DataModelSkill | Scripts/2D/Level, Scripts/2D/UI, Game.unity, ResourcesLocal/Prefabs/UI | 需要修改场景流程和结算 UI，需详细回滚方案 |
| [SKIPPED] | A003 | 实时多人PvP对战系统 | 多人玩法 | 项目有Photon但无PvP逻辑 | 极高 | 极高 | 极高 | 极高 | P2 | MultiplayerAgent | NetworkSkill | 全局 | 涉及Photon深度改造，边界不可控，自动跳过 |
| [DONE] | A004 | 玩家角色成长树与技能解锁系统 | 成长与养成 | 玩家属性缺少长期成长链路 | 大幅提升长期游玩动力 | 为后续任务和成就提供解锁条件基础 | 中 | 高 | P0 | GameplayFeatureAgent | RuntimeFeatureSkill | Scripts/2D/Gameplay, Scripts/2D/Player, Scripts/2D/UI, Resources/SO, ResourcesLocal/UI | 已完成；任务卡：Agent/Reports/2026-04-30/ambitious_A004_Skill_Tree/task_ambitious_A004_Skill_Tree.md；验证记录：Agent/Reports/2026-04-30/ambitious_A004_Skill_Tree/validation_ambitious_A004.md |

## 游戏体验升级候选重点方向

1. 战斗体验升级类
   - 完整击杀反馈链（连击计数→连击特效→连击语音→击杀弹窗→经验结算→收集统计）
   - 技能系统（主动技能+冷却+技能特效+技能升级）
   - 受击反馈系统（屏幕震动+受击特效+无敌帧视觉+血量警告）
   - 战斗评分系统（连击分+时间分+受击扣分+最终评级）
   - BUFF/DEBUFF系统（增益效果+减益效果+持续伤害+状态图标）
   - 弹幕/BOSS特殊攻击模式系统

2. 成长与养成升级类
   - 角色成长树（多分支属性成长+技能解锁+资源消耗+成长重置）
   - 装备系统（装备槽位+属性加成+稀有度+装备掉落+装备对比）
   - 角色皮肤/外观系统（解锁条件+外观切换+战斗中显示）
   - 局外养成系统（金币→永久升级→跨局继承属性）
   - 天赋/符文系统（局前配置→战斗内生效→多套方案切换）

3. 关卡与玩法升级类
   - 完整波次管理系统（波次配置→波次过渡→Boss波→奖励波→无尽模式）
   - 关卡星级评定系统（时间分+击杀分+受击分+收集分→星级→奖励倍率）
   - 关卡目标多样化（生存目标+击杀目标+收集目标+护送目标+限时目标）
   - 随机事件系统（关卡内随机BUFF/陷阱/宝箱/精英怪）
   - 关卡选择/解锁地图（关卡节点→星级解锁→路径选择→Boss关卡）

4. UI 与表现升级类
   - 完整 HUD 系统（血量条+技能冷却+连击计数+波次进度+分数+小地图）
   - 结算面板完整实现（星级动画+数据统计+奖励展示+重试/下一关按钮）
   - 主菜单/大厅界面（关卡选择+角色配置+商店+设置+成就）
   - 过渡动画系统（关卡加载过渡+波次过渡+结算过渡+死亡过渡）
   - 伤害数字/浮动文字系统（伤害数值弹出+暴击特效+治疗数字+状态文字）

5. 资源与收集升级类
   - 局内金币/资源掉落系统（掉落物生成+拾取+磁铁效果+收集统计）
   - 宝箱/奖励房间系统（关卡内宝箱→打开动画→随机奖励→稀有度）
   - 收集品图鉴系统（收集品定义→图鉴UI→收集进度→收集奖励）
   - 每日/每周任务系统（任务刷新→任务进度→任务奖励→红点提示）
   - 成就系统完整实现（成就条件→成就解锁通知→成就列表→成就点数）

6. 叙事与氛围升级类
   - 简单剧情对话系统（对话触发→对话UI→选项分支→剧情记录）
   - 环境叙事元素（关卡内可交互叙事物件+文字提示+收集品描述）
   - 音乐/音效管理系统（BGM切换+音效优先级+音量控制+音频混合）
   - 天气/环境效果系统（关卡内天气变化→视觉特效→游戏性影响）

## 执行步骤

1. 读取并理解以下文件：
   - Agent/README.md
   - Agent/Docs/ImplementationRoadmap.md
   - Agent/Docs/SkillCatalog.md
   - Agent/Config/agent_registry.json
   - Agent/Config/task_router.json
   - Agent/Templates/agent_task_card.md

2. 读取全局候选功能发现报告：
   - Agent/Reports/ambitious_discovery.md

   如果该文件不存在，则自动创建。
   如果该文件已存在，则必须读取其中已有候选，尤其是 `[DONE]`、`[SKIPPED]`、`[BLOCKED]` 和 `[PARTIAL]` 状态，避免重复生成或重复实现同一功能。

3. 只读扫描项目上下文，重点检查：
   - README、Agent 文档、历史任务卡中的后续建议
   - `Game.unity` 的真实路径及其现有 UI 层级
   - `ResourcesLocal` 下已有 UI、Prefab、HUD、Popup、Panel、Guide、Settlement、Reward、Skill、Achievement 等目录结构
   - Scripts/2D 中已有的游戏业务脚本、管理器、玩家控制、敌人逻辑、关卡逻辑、UI 逻辑、奖励逻辑、任务逻辑
   - Scripts/2D 中的 TODO、FIXME、空方法、临时实现、重复模式
   - 已有的 Feature/Efficiency 任务卡和验证记录，了解已实现和已跳过的功能
   - 已有系统中“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查 / 有事件但无反馈 / 有结果但无统计 / 有系统但无深度”的半完成链路
   - 玩家输入、战斗、关卡、资源、奖励、任务、成就、UI、音效、动画等模块中可大刀阔斧扩展的机会
   - Resources/SO、Resources/Tilemap、Resources/Images、ResourcesLocal、Prefab、Scene 中的业务配置和资源使用信号
   - 存档、Photon、AssetBundle、资源引用等高风险区域，仅做了解性只读检查
   - Agent/Reports/ambitious_discovery.md 中已有的候选功能状态
   - Agent/Reports/feature_discovery.md 和 Agent/Reports/efficiency_discovery.md 中已有的候选功能状态，作为去重参考
   - Agent/Reports/ 下所有历史日期目录及其子任务目录中的 task_*.md 和 validation_*.md
   - 历史记录中已经标记为 `[DONE]` 的候选，避免重复实现同一功能
   - 历史任务目录中遗留的旧版 ambitious_discovery.md，仅作为兼容读取依据，不再作为新的写入目标

4. 生成或更新全局功能发现报告：
   - Agent/Reports/ambitious_discovery.md

   报告必须包含：
   - 全局候选功能列表
   - 扫描范围
   - 每个候选的唯一候选ID（前缀 A，如 A001、A002）
   - 每个候选的状态标记：`[TODO]`、`[DONE]`、`[SKIPPED]`、`[BLOCKED]` 或 `[PARTIAL]`
   - 每个候选的业务类型、来源信号、玩家价值、开发价值、风险、成本、优先级、推荐 Agent、推荐 Skill、预计影响范围
   - 推荐优先开发的 1-3 个游戏体验升级功能
   - 被跳过的高风险候选及原因
   - 已完成候选的任务卡路径、修改文件和验证记录摘要
   - 历史已完成候选的去重依据

   注意：
   - 不要覆盖已有候选状态。
   - 对已有 `[DONE]` 候选不得重新改为 `[TODO]`。
   - 如果发现相似候选，应合并到已有候选的处理说明中，不要重复分配新候选ID。
   - 新增候选时，应延续已有候选ID编号，例如已有 A001-A003，则新增从 A004 开始。
   - 候选必须以游戏体验升级功能为主，不要选择纯工程工具、纯扫描器、纯报告生成器。
   - 功能范围应明显大于 Prompt_Feature.md 的 feature 候选，具有“大刀阔斧”的特征。
   - UI 类候选不得因为涉及 Scene/Prefab 就默认跳过，应优先判断是否能通过 `Game.unity` 独立 UI、`ResourcesLocal` 独立 UI Prefab、Editor 菜单工具或运行时代码安全落地。
   - 如果候选的核心体验依赖 UI，但无法安全生成 UI，应至少实现业务逻辑 + UI Binder / ViewModel / Editor 生成工具，并在任务卡中标记为 `[PARTIAL]` 或说明降级原因。

5. 自动选择一个最适合立即开发的游戏体验升级候选：
   - 只能从 `[TODO]` 状态的候选中选择。
   - 不得重复选择历史记录中已经 `[DONE]` 的候选。
   - 不得与 Agent/Reports/feature_discovery.md 和 Agent/Reports/efficiency_discovery.md 中已完成的候选实质重复。
   - 优先选择 P0。
   - 其次选择 P1。
   - 必须满足：中风险或高风险但有明确边界、可在一个任务卡内完成核心功能、不会完全破坏项目可运行性。
   - 允许涉及 Scene、Prefab、ScriptableObject、StreamingAssets 的修改，但必须有明确的回滚方案。
   - 如果候选涉及 UI、HUD、面板、弹窗、浮动文字、引导提示、结算界面，应优先选择能在 `Game.unity` 或 `ResourcesLocal` 中产生可见结果的候选。
   - 优先选择能通过“新增独立系统 + 最小化侵入已有系统 + 可见 UI 表现”方式完成的中大型功能。
   - 如果候选涉及 Photon 同步逻辑或 AssetBundle 配置的深度修改，则需特别评估，风险过高则跳过并将状态更新为 `[SKIPPED]`。
   - 如果候选需要人工确认的复杂场景/UI布局调整，可以先实现核心业务逻辑，并优先提供 `Game.unity` 示例节点、`ResourcesLocal` 预制体或 Editor 生成工具；如果这些都不可行，才在任务卡中明确后续人工接入方式。

6. 生成本次任务目录：
   - 首先确保日期目录存在：
     - Agent/Reports/<今天日期>/
   - 然后在该日期目录下创建本次任务的独立目录：
     - Agent/Reports/<今天日期>/ambitious_<候选ID>_<功能名安全短名>/
   - 如果候选ID尚未确定，则先创建：
     - Agent/Reports/<今天日期>/ambitious_run_<HHmmss>/
   - 选定候选后，如果当前目录仍是 ambitious_run_<HHmmss>，可继续使用该目录。
   - 如果需要更清晰区分任务，可将目录调整为：
     - Agent/Reports/<今天日期>/ambitious_<候选ID>_<功能名安全短名>/
   - 如果发生目录调整，必须保证任务卡、验证记录、回滚方案和后续补充文件都位于最终的 `<TASK_DIR>` 中。
   - 不允许把多个任务的输出混写到同一个任务目录中。
   - 不允许在 `<TASK_DIR>` 中创建新的 ambitious_discovery.md。
   - 以下路径统一用 `<TASK_DIR>` 表示本次任务目录。

7. 为选中的候选生成任务卡：
   - <TASK_DIR>/task_ambitious_<候选ID>_<功能名安全短名>.md

   任务卡必须包含：
   - 候选ID
   - 原始候选
   - 当前状态
   - 本次任务目录
   - 全局候选报告路径：Agent/Reports/ambitious_discovery.md
   - 任务分类（游戏体验升级）
   - 游戏业务类型
   - 玩家价值
   - 开发价值
   - 预计影响范围
   - 负责 Agent
   - 需要的 Skill
   - 影响路径（明确列出所有将被修改、新增的目录和文件类型）
   - 不应触碰路径（明确列出不可修改的路径，如 Photon 同步核心、AssetBundle 配置等）
   - 风险等级
   - 功能边界（明确列出本次实现包含的子模块和不包含的子模块）
   - 业务规则说明（详细描述每个子模块的业务逻辑）
   - 数据流说明（描述各子模块之间的数据流转关系）
   - UI 接入策略
   - Scene / Prefab / ResourcesLocal 生成策略
   - 资源修改清单（列出需要修改或新增的 Scene/Prefab/ScriptableObject/StreamingAssets 及其修改方式）
   - 执行步骤（分步骤详细列出，每个步骤包含：目标、涉及文件、操作方式、完成标准）
   - 验证步骤
   - 回滚方案（详细描述如何撤销对 Scene/Prefab/ScriptableObject/UI Prefab 等的修改，恢复到修改前状态）
   - 结果区

8. 按任务卡实现该游戏体验升级功能：
   - 严格按任务卡执行步骤实施。
   - 可新增独立脚本、业务管理器、数据结构、事件监听器、UI 组件、Editor 菜单、调试输出。
   - 如果包含 UI，优先在 `Game.unity` 中新增独立 UI 节点。
   - 如果不能安全修改 `Game.unity`，优先在 `ResourcesLocal` 下创建独立 UI Prefab。
   - 如果不能创建 Prefab，优先创建 Editor 菜单工具用于生成 UI 或 Prefab。
   - 如果 Editor 工具也不可行，再使用运行时代码动态创建 UI。
   - 最后才退回为纯业务逻辑、ViewModel、数据层或人工接入说明。
   - 允许修改 Scene、Prefab、ScriptableObject、StreamingAssets，但必须：
     - 修改前在任务卡中记录修改前状态。
     - 保留或同步更新 `.meta` 文件。
     - 尽量通过新增而非修改已有内容的方式实现。
     - 对已有字段和节点只做追加，不做破坏性改动。
     - 对已有 UI 层级只追加独立节点，不直接重排或删除已有节点。
   - 保持现有项目命名、目录结构和代码风格。
   - 不做与任务无关的重构。
   - 不修改用户已有的无关改动。
   - **新增代码应具备清晰的中文注释，说明用途、接入方式、子模块关系和风险边界。**
   - **修改已有代码时应添加中文注释标注修改原因和范围。**
   - 子模块之间通过事件或接口解耦，降低相互依赖。

9. 完成后运行可行的验证：
   - 至少做静态检查或编译相关检查。
   - 如果不能运行 Unity 编译或 Play Mode，要在任务卡中明确写出未验证原因。
   - 如果新增运行时业务脚本，要验证类名、命名空间、Unity API 使用、脚本路径和基础逻辑。
   - 如果新增 Editor 工具或报告工具，要验证菜单路径、输出路径和基本扫描逻辑。
   - 如果新增数据模型或管理器，要验证默认值、空引用保护和调用边界。
   - 如果修改了 Scene/Prefab/ScriptableObject，要验证：
     - `.meta` 文件是否同步更新。
     - 已有字段值是否保留。
     - 新增内容是否正确挂载。
     - 回滚路径是否仍然有效。
   - 如果新增 UI 到 `Game.unity`，要验证：
     - `Game.unity` 路径是否正确。
     - 新增 UI 根节点命名是否清晰。
     - Canvas 层级是否合理。
     - 锚点、布局、字体引用和默认显示状态是否合理。
     - 是否避免破坏已有 UI。
   - 如果新增 UI Prefab 到 `ResourcesLocal`，要验证：
     - Prefab 路径是否符合项目规范。
     - `.meta` 文件是否存在。
     - UI 层级是否完整。
     - 脚本引用是否正确。
     - 后续接入方式是否明确。
   - 如果使用运行时代码动态创建 UI，要验证：
     - Canvas / EventSystem 检查逻辑。
     - 默认启用策略。
     - 空引用保护。
     - UI 销毁或隐藏逻辑。
   - 验证记录必须写入：
     - <TASK_DIR>/validation_ambitious_<候选ID>.md

10. 更新任务卡结果区，写入：
    - 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`
    - 已完成内容（列出每个子模块的完成情况）
    - 修改的文件（明确区分新增文件和修改文件）
    - 新增的游戏体验能力
    - 玩家侧效果（描述玩家能感知到的变化）
    - UI 生成位置：
      - 是否已写入 `Game.unity`
      - 是否已创建 `ResourcesLocal` Prefab
      - 是否改用 Editor 工具
      - 是否改用运行时代码动态创建
      - 哪些 UI 部分仍需人工接入
    - 开发侧接入方式（描述后续如何进一步扩展或接入）
    - 验证结果
    - 验证记录路径
    - 回滚方案验证（是否验证了回滚路径仍然有效）
    - 未完成项
    - 剩余风险
    - 后续建议（包括是否需要人工调整 UI 布局、平衡数值、添加美术资源等）

11. 回写全局功能发现报告：
    - 打开 Agent/Reports/ambitious_discovery.md。
    - 找到本次实现的候选ID。
    - 将该候选状态从 `[TODO]` 更新为：
      - `[DONE]`：功能已实现且完成可行验证
      - `[PARTIAL]`：功能核心已完成但有部分子模块未完成
      - `[BLOCKED]`：因环境、依赖或权限问题未能完成
    - 在该候选的“处理说明”中补充：
      - 本次任务目录路径
      - 对应任务卡路径
      - 验证记录路径
      - 修改文件清单
      - 新增游戏体验能力摘要
      - UI 生成方式摘要
      - 各子模块完成状态
      - 验证结果摘要
      - 是否仍有剩余风险
      - 后续是否需要人工接入 UI、Scene、Prefab 或配置
      - 回滚方案是否已验证
    - 对自动跳过的候选，将状态更新为 `[SKIPPED]`，并写明跳过原因。
    - 不要把状态回写到任务目录下的 ambitious_discovery.md，因为该文件不应再存在于任务目录中。

12. 最终回复只需要简洁汇总：
    - 全局候选报告路径
    - 本次任务目录
    - 自动选择了哪个游戏体验升级功能
    - 候选ID
    - 游戏业务类型
    - 最终状态
    - 修改了哪些文件（区分新增和修改）
    - 新增了什么游戏体验能力（按子模块列出）
    - 玩家侧效果
    - UI 是否已生成到 Game.unity
    - UI 是否已生成到 ResourcesLocal Prefab
    - 如果没有生成 UI，说明采用了哪种降级方案
    - 是否需要后续人工接入
    - 任务卡路径
    - 验证记录路径
    - 验证结果
    - 回滚方案是否已验证
    - 剩余风险
