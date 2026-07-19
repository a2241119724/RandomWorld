# Unity 游戏体验升级功能自动开发 Prompt

请基于 `./Agent` 中的 Agent 体系，自动完成一次完整流程：

> 发现游戏体验升级机会 → 选择最具改造价值候选 → 生成任务卡 → 开发大规模新功能 → 验证记录 → 更新候选完成状态

---

## 1. 总目标

- 自动为当前 Unity 游戏项目发现一个能显著提升体验的中大型新功能。
- 自动选择一个中等风险或高风险但边界明确、能显著丰富玩法或提升玩家体验的候选。
- 允许较大幅度改动：新增多个关联脚本，修改 Scene / Prefab / ScriptableObject（必须有回滚方案），新增 UI 组件，扩展游戏系统。
- 一次只开发一个游戏体验升级功能，但可包含多个关联子模块。
- 允许跨 `Scripts/2D`、`Resources`、`ResourcesLocal`、Prefab、Scene 等路径协调修改。
- 若功能包含 UI / HUD / 弹窗 / 面板 / 浮动文字 / 引导 / 结算 / 奖励 / 成长 / 关卡选择等表现内容，应尽量生成可直接在 Unity 使用的 UI 资源，而非只写纯代码或说明。

---

## 2. 全局硬性要求

- 不向我提问，不等待我确认。
- 遇到边界完全不可控、可能破坏项目可运行性的候选，自动跳过并选择下一个边界明确候选。
- 一次只实现一个游戏体验升级功能，可包含多个子模块。
- 所有生成代码注释必须使用中文。
- 候选功能总表唯一写入：`Agent/Reports/ambitious_discovery.md`；不得在任务目录重复创建该文件。
- 每次任务必须在 `Agent/Reports/<今天日期>/` 下创建独立任务目录，任务卡、验证记录、回滚方案、补充报告等均放入该目录，避免同日任务混淆。
- 游戏体验升级任务目录固定前缀：`ambitious_`，用于和 `Prompt_Feature.md` 的 `feature_`、`Prompt_Efficiency.md` 的 `efficiency_` 隔离。
- 推荐任务目录：`Agent/Reports/<今天日期>/ambitious_<候选ID>_<功能名安全短名>/`；候选未确定时先用 `Agent/Reports/<今天日期>/ambitious_run_<HHmmss>/`，选定后可继续使用或重命名。
- 同日多次执行不得覆盖已有目录；目录存在时自动追加时间戳或序号。
- 优先选择能显著改变游戏体验、丰富玩法深度、提升沉浸感、具有“业务逻辑完整闭环 + UI / 资源可见接入”的中大型功能。
- 若候选含 UI 表现层，不得只实现数据层或 Manager，应优先把 UI 落到 `Game.unity` 或 `ResourcesLocal` 预制体中。
- 允许修改 Scene / Prefab / ScriptableObject / StreamingAssets，但必须满足：
  - 修改前在任务卡记录当前状态和回滚方案；
  - Unity 资源修改必须保留或同步更新 `.meta`；
  - Scene 修改尽量新增 GameObject / Canvas 子节点 / 新脚本，避免改已有节点核心属性；
  - Prefab 修改尽量新增组件、子节点或独立新 Prefab，避免破坏已有 Prefab 变体层级；
  - ScriptableObject 修改保留已有字段值，仅追加新字段或新实例；
  - StreamingAssets 只允许纯新增文件，不覆盖已有配置；
  - Photon 同步逻辑、AssetBundle 配置仍属高风险，需特别谨慎并在回滚方案写明保护措施。
- 若所有候选均不可控高风险，则降级为 `Prompt_Feature.md` 风格低侵入实现，并在任务卡写明降级原因。
- 候选编号必须唯一，前缀为 `A`，如 `A001`、`A002`，并记录完成状态，区分 feature_ / efficiency_ 候选。
- 完成功能实现与验证后，必须回写 `Agent/Reports/ambitious_discovery.md` 对应候选状态。
- 扫描历史时必须递归检查 `Agent/Reports/` 下所有日期目录及子任务目录中的 `task_*.md`、`validation_*.md`。
- 必须读取 `Agent/Reports/ambitious_discovery.md`，避免重复实现 `[DONE]` 候选。
- 历史任务目录中旧版 `ambitious_discovery.md` 需兼容读取，但新的候选总表只允许写入 `Agent/Reports/ambitious_discovery.md`。

---

## 3. 公共代码优先与分层规则

### 3.1 `Scripts/2D/Tool` 公共工具类优先

开发前必须扫描并复用 `Scripts/2D/Tool`。已有可复用方法必须优先调用，不得重复实现。多个子模块共享的公共能力应优先沉淀到 `Scripts/2D/Tool`，包括但不限于：UI 创建辅助，Canvas / EventSystem / Panel / Text / Button / Image / ScrollView 安全创建或查找，GameObject 创建/命名/查找/挂载组件，Component 安全获取，空引用保护，默认值处理，Resources / ResourcesLocal 路径拼接与规范化，资源加载，Prefab 实例化，父节点挂载，层级命名，数值/时间/分数/奖励文本格式化，伤害/经验/金币/星级/评分展示文本生成，通用事件分发、状态通知、消息广播、日志、Debug 文本、运行时状态摘要，列表/字典/配置安全访问，以及可被战斗、奖励、关卡、成长、UI 复用的计算逻辑。

约束：
- 业务脚本只保留强相关业务规则、状态流转、事件响应、UI Binder、数据模型、表现控制。
- `Scripts/2D/Tool` 必须低耦合，不强依赖具体 Scene / Prefab / ScriptableObject / 存档 / Photon / AssetBundle / 单一系统。
- 工具类命名遵循项目风格；无风格时可用 `XxxTool`、`XxxUtility`、`XxxHelper`、`XxxRuntimeTool`、`XxxUITool`、`XxxFormatTool`、`XxxGameplayTool`。
- 工具方法必须有中文注释，说明用途、参数、返回值、边界、风险限制。
- 原则上 `Scripts/2D/Tool` 不直接引用 `UnityEditor`，避免正式构建报错；涉及 `UnityEditor` 时拆分为运行时工具 `Scripts/2D/Tool` 与 Editor 菜单/专用逻辑 `Scripts/2D/Editor`。
- 工具类尽量使用静态方法或低状态对象，避免全局副作用。
- 修改已有工具类必须兼容已有调用方，不破坏签名和行为。
- 若存在重复逻辑但暂不抽取，必须在任务卡/验证记录说明原因。

### 3.2 `Scripts/2D/Enum` 枚举优先

开发前必须扫描并复用 `Scripts/2D/Enum`。已有语义一致或可扩展复用的枚举必须优先使用，不得在 Gameplay / UI / Level / Rewards / Progression / Editor / Ambitious 脚本中重复定义相似枚举。跨子模块复用的状态类型、结果类型、奖励类型、UI 类型、关卡类型、战斗反馈类型、成长类型、事件类型等必须优先放入 `Scripts/2D/Enum`。私有且仅服务单类极小内部状态的枚举可保留在类内，但任务卡必须说明不抽取原因。

常见公共枚举包括但不限于：游戏流程状态、战斗反馈类型、伤害显示/文字类型、奖励类型、奖励领取状态、关卡结果类型、关卡星级/星级结果类型、成长节点类型、技能解锁状态、HUD 显示状态、UI 面板类型、弹窗类型、引导步骤状态、成就状态、任务目标类型、资源收集类型、Ambitious 功能安装状态、验证结果状态。

命名示例：`BattleFeedbackType`、`DamageTextType`、`RewardState`、`RewardType`、`LevelResultType`、`LevelStarType`、`SkillUnlockState`、`GrowthNodeType`、`HudDisplayState`、`AmbitiousInstallState`、`ValidationResultType`。枚举成员遵循项目风格；无风格时优先 PascalCase。

约束：
- 枚举文件必须有中文注释，说明用途、每个值含义、使用场景、是否允许扩展。
- 修改已有枚举必须兼容：不得删除、重命名、改变显式数值含义；仅允许追加新值并说明原因。
- 若已有枚举命名或语义冲突，优先复用最贴近现有业务体系者；无法安全判断时新增更明确枚举，并在任务卡说明。
- 若存在重复枚举但暂不抽取，必须说明原因。

### 3.3 `Scripts/2D/Constant` 常量优先

开发前必须扫描并复用 `Scripts/2D/Constant`。已有语义一致常量必须优先使用，不得在 Gameplay / UI / Level / Rewards / Progression / Editor / Ambitious 脚本中重复写魔法数字、魔法字符串、固定路径、默认文案、默认阈值、菜单路径、资源名、节点名等。跨子模块复用的路径、默认文案、UI 节点名、Prefab 名、Resources / ResourcesLocal 路径、默认数值、阈值、事件名、日志前缀等必须优先放入 `Scripts/2D/Constant`。仅服务类内部且不会复用的常量可保留为 `private const` 或 `private static readonly`，但任务卡必须说明不抽取原因。

常见公共常量包括但不限于：`Agent/Reports/ambitious_discovery.md` 路径、`ambitious_` 任务目录前缀、任务卡/验证记录/回滚方案文件名前缀、Editor 菜单路径、UI 默认文案、HUD / Panel / Popup / DamageText 节点名、Prefab 名、Resources / ResourcesLocal 路径、默认分数/奖励数量/经验值/金币数/冷却时间/显示时长/星级阈值/评分阈值、Tag / Layer 名、PlayerPrefs Key、事件名、日志前缀、空结果提示、错误提示。

命名示例：`AmbitiousConstant`、`AmbitiousConstants`、`GameplayConstant`、`BattleConstant`、`RewardConstant`、`LevelConstant`、`HudConstant`、`UIConstant`、`ResourcePathConstant`、`EditorMenuConstant`。字段命名遵循项目风格；无风格时公共常量 PascalCase，私有常量 camelCase 或项目私有字段风格。

约束：
- 常量按业务语义分组，避免无关常量塞入巨大类。
- 新增常量必须有中文注释，说明用途、场景、默认值含义、修改风险。
- 修改已有常量必须兼容：不得随意改值、删除、重命名；若需新增替代常量，保留旧常量并说明兼容关系。
- 路径类常量应优先配合 `Scripts/2D/Tool` 路径处理工具使用，避免硬编码拼接散落。
- 若存在重复常量或魔法值但暂不抽取，必须说明原因。

### 3.4 公共代码分层优先级

1. 公共枚举 → `Scripts/2D/Enum`：稳定表达游戏状态、战斗反馈、奖励状态、关卡结果、UI 类型、成长状态、验证状态等。  
2. 公共常量 → `Scripts/2D/Constant`：稳定表达字符串、路径、菜单、文件名、文案、阈值、Key、节点名、Prefab 名、事件名等。  
3. 公共函数/辅助逻辑 → `Scripts/2D/Tool`：UI 创建、路径处理、资源加载、格式化、事件分发、日志、安全访问、数值计算等。  
4. 具体业务脚本 → `Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Character/Player`、`Scripts/2D/Domain` 或项目已有目录，只保留业务规则、状态流转、事件响应、UI Binder、ViewModel、表现控制、功能入口。  
5. Editor 专用逻辑 → `Scripts/2D/Editor` 或已有 Editor 目录，不得让运行时代码直接依赖 `UnityEditor`。  

不得将公共枚举、公共常量、公共工具函数混写到单个大型业务脚本中。中大型功能必须拆分为公共工具层、公共枚举层、公共常量层、数据层、业务层、表现层、Editor 安装层；业务层可调用公共层，表现层可调用公共层和业务层接口，Editor 安装层可调用公共层但不得让公共工具层反向依赖 Editor API；子模块通过事件、接口、数据模型或工具函数解耦，避免互相硬引用；避免所有逻辑堆入大型 Manager。

---

## 4. UI / Scene / Prefab 生成规则

当功能包含 HUD、血量条、技能冷却、连击计数、波次进度、击杀反馈、伤害数字、浮动文字、结算面板、星级面板、奖励面板、技能升级、成长树、装备、任务、成就、图鉴、新手引导、交互提示、失败原因、主菜单、关卡选择、设置、商店、角色配置等 UI 时，必须按以下优先级实现。

### 4.1 优先在 `Game.unity` 中生成 UI

若存在 `Game.unity`，优先尝试安全生成 UI：
- 先搜索真实路径，不得凭空假定；可能路径包括 `Assets/Scenes/Game.unity`、`Assets/Game.unity`、`Assets/Resources/Scenes/Game.unity` 等。
- 若可安全修改，应优先新增独立 UI 根节点，如 `Canvas`、`EventSystem`、`Ambitious_<候选ID>_<功能名>_Root`、`HUD_Root`、`SettlementPanel`、`RewardPopup`、`SkillTreePanel`、`DamageTextLayer`、`GuideTipLayer`。
- 已有 Canvas 时优先在其下新增独立子节点；无 Canvas 时可新增独立 Canvas，但不得影响摄像机、渲染顺序、输入系统、场景流程。
- 新增 UI 节点必须独立，不破坏已有 UI 层级、引用、脚本绑定、对象命名；命名带 `Ambitious_`、候选ID或功能短名，便于定位和回滚。
- UI 节点名、默认文案、资源路径、Prefab 名、默认显示时长等优先使用 `Scripts/2D/Constant`；UI 显示状态、面板类型、弹窗类型、引导状态、伤害文本类型、奖励状态等优先使用 `Scripts/2D/Enum`。
- 挂载脚本只使用本次新增脚本或明确兼容的已有脚本；可新增 UI Binder、UI Controller、ViewModel、Runtime Manager，并保持表现层与业务逻辑解耦。
- 不直接修改已有核心 UI 节点字段，除非必要且边界清晰，并在任务卡说明原因和回滚。
- 若无法确认 Scene YAML 可安全修改，不手写大段 Scene YAML，改为 Editor 菜单工具或 ResourcesLocal 预制体方案。

### 4.2 其次在 `ResourcesLocal` 下创建 UI Prefab

无法安全改 `Game.unity` 或项目更适合 Prefab 接入时，优先在 `ResourcesLocal` 下创建 UI 预制体。优先路径：
- `Assets/ResourcesLocal/UI/<功能名安全短名>/`
- `Assets/ResourcesLocal/Prefabs/UI/<功能名安全短名>/`
- `Assets/ResourcesLocal/HUD/<功能名安全短名>/`
- `Assets/ResourcesLocal/Popup/<功能名安全短名>/`
- `Assets/ResourcesLocal/Panels/<功能名安全短名>/`
- `Assets/ResourcesLocal/<已有项目约定目录>/<功能名安全短名>/`

要求：
- 创建独立 UI Prefab，如 `Ambitious_<候选ID>_<功能名安全短名>HUD.prefab`、`Panel.prefab`、`Popup.prefab`、`SettlementView.prefab`、`SkillTreeView.prefab`。
- 优先遵循项目已有 ResourcesLocal UI / Prefab 目录规范。
- 新增 Prefab 必须保留 `.meta`，不得覆盖已有 Prefab。
- Prefab 尽量包含完整 UI 层级、默认文案、基础样式、挂载脚本和必要组件。
- Prefab 路径、名称、默认文案、节点名优先沉淀到 `Scripts/2D/Constant`；显示/交互/奖励/引导/伤害文本类型优先沉淀到 `Scripts/2D/Enum`。
- 运行时脚本放入合适路径，如 `Scripts/2D/UI`、`Gameplay`、`Player`、`Level`、`Rewards`、`Progression`。
- 图片、字体、材质等引用无法安全判断时，使用 Unity 默认 UI 组件或已有可安全引用资源。
- 若无法生成真实 Prefab，则生成 Editor 菜单工具，由开发者在 Unity 中点击菜单自动创建 Prefab。

### 4.3 再次生成 Editor 菜单工具

若不能安全改 `Game.unity` 且不能可靠创建 Prefab，则生成 Editor 菜单工具。建议菜单：
- `Tools/Agent/Ambitious/Create <功能名> UI In Game Scene`
- `Tools/Agent/Ambitious/Create <功能名> Prefab In ResourcesLocal`
- `Tools/Agent/Ambitious/Install <功能名> System`
- `Tools/Agent/Ambitious/Rebuild <功能名> UI`

要求：
- 放在 Editor 目录或已有 Editor 工具目录。
- 菜单命名清晰，菜单路径优先使用 `Scripts/2D/Constant`；执行状态、安装状态、生成类型、验证状态优先使用 `Scripts/2D/Enum`。
- 执行时检查 `Game.unity` 是否存在、目标目录是否存在，不存在则创建。
- 不覆盖已有 Prefab 或场景对象；重复执行应安全退出或生成带序号新对象。
- 工具生成的 UI 节点、Prefab、配置文件必须命名清晰并有回滚说明。
- 工具代码注释必须中文。

### 4.4 然后用运行时代码动态创建 UI

Editor 工具也不可行时，新增运行时 UI 管理器或可选挂载组件：
- 自动检查 Canvas 和 EventSystem；缺失时创建独立节点。
- 动态 UI 独立命名，不污染已有 UI；默认状态可控，避免进入游戏破坏原流程。
- 调试型或示例型 UI 提供开关，避免影响正式体验。
- 必须说明接入方式与风险边界。
- 节点名、默认文案、尺寸、显示时长、显示开关 Key 优先使用 `Scripts/2D/Constant`；显示状态、面板类型、奖励/引导/安装状态优先使用 `Scripts/2D/Enum`。

### 4.5 最后才退回纯代码或说明

仅在找不到或无法安全修改 `Game.unity`、无法确认 Scene / Prefab YAML、当前环境无法运行 Unity Editor 且不能可靠生成 Prefab、项目缺少必要 UI 包或组件、资源路径/字体/图片/Canvas 结构无法确定、自动修改可能破坏引用时，才允许退回代码或说明。任务卡必须写明：
- 为什么未直接生成到 `Game.unity`；
- 为什么未创建 `ResourcesLocal` 预制体；
- 是否提供 Editor 菜单工具；
- 是否提供运行时代码动态创建 UI；
- 哪些 UI 仍需人工接入；
- 后续挂载到哪个场景对象、Canvas 或 Prefab；
- 如何验证 UI 接入成功。

UI 优先级总结：`Game.unity` 独立 UI 节点 → `ResourcesLocal` 独立 UI Prefab → Editor 菜单工具 → 运行时代码动态 UI → 纯业务逻辑 / ViewModel / 数据层 / 人工接入说明。若使用第 3/4/5 种方式，必须在任务卡、回滚方案、验证记录说明原因。

---

## 5. 候选状态与列表格式

状态：
- `[TODO]`：待处理。
- `[DONE]`：已实现并完成可行验证。
- `[SKIPPED]`：因边界不可控、破坏性过大或不可逆修改而跳过。
- `[BLOCKED]`：因环境、依赖、权限或无法验证而受阻。
- `[PARTIAL]`：核心能力已实现但仍有未完成项，如 UI 接入、资源微调。

候选表必须包含：

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 预计影响范围 | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

示例候选必须保留语义：
- `A001` `[TODO]` 完整击杀奖励反馈系统（连击 HUD + 击杀弹窗 + 经验浮动 + 收集统计）：战斗与奖励反馈；来源为有击杀事件但缺少多层奖励反馈链路；提升战斗爽感、即时反馈和成长获得感；可为成就/任务/排行榜提供基础；中风险、高成本、P0；推荐 `GameplayFeatureAgent` + `RuntimeFeatureSkill`；影响 `Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Game.unity`、`ResourcesLocal/UI`；优先在 `Game.unity` 生成 HUD 和击杀反馈 UI，不能安全改场景时创建 ResourcesLocal UI Prefab。
- `A002` `[TODO]` 关卡流程完整重构（波次 → Boss → 结算 → 星级评定）：关卡与玩法；有胜负判定但缺少波次递进、Boss 阶段和星级反馈；提升关卡节奏和重玩价值；为关卡编辑器提供模板；高风险、高成本、P0；推荐 `LevelFeatureAgent` + `DataModelSkill`；影响 `Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Domain/Wave`、`Game.unity`、`ResourcesLocal/Prefabs/UI`；需详细回滚。
- `A003` `[SKIPPED]` 实时多人 PvP 对战系统：多人玩法；项目有 Photon 但无 PvP；玩家/开发价值极高，风险/成本极高，P2；推荐 `MultiplayerAgent` + `NetworkSkill`；影响全局；涉及 Photon 深度改造，边界不可控，自动跳过。
- `A004` `[DONE]` 玩家角色成长树与技能解锁系统：成长与养成；玩家属性缺少长期成长链路；提升长期游玩动力；为任务/成就提供解锁条件；中风险、高成本、P0；推荐 `GameplayFeatureAgent` + `RuntimeFeatureSkill`；影响 `Scripts/2D/Gameplay`、`Scripts/2D/Character/Player`、`Scripts/2D/UI`、`Resources/SO`、`ResourcesLocal/UI`；处理说明包含任务卡和验证记录路径，如 `Agent/Reports/2026-04-30/ambitious_A004_Skill_Tree/task_ambitious_A004_Skill_Tree.md`、`validation_ambitious_A004.md`。

---

## 6. 游戏体验升级候选重点方向

1. 战斗体验升级：完整击杀反馈链（连击计数 → 连击特效 → 连击语音 → 击杀弹窗 → 经验结算 → 收集统计）、主动技能 + 冷却 + 特效 + 升级、受击反馈（屏幕震动 + 受击特效 + 无敌帧视觉 + 血量警告）、战斗评分（连击分 + 时间分 + 受击扣分 + 最终评级）、BUFF / DEBUFF（增益、减益、持续伤害、状态图标）、弹幕 / BOSS 特殊攻击模式。
2. 成长与养成：角色成长树（多分支属性 + 技能解锁 + 资源消耗 + 重置）、装备系统（槽位 + 属性 + 稀有度 + 掉落 + 对比）、皮肤 / 外观（解锁 + 切换 + 战斗显示）、局外养成（金币 → 永久升级 → 跨局继承）、天赋 / 符文（局前配置 → 战斗生效 → 多套方案）。
3. 关卡与玩法：完整波次管理（配置 → 过渡 → Boss 波 → 奖励波 → 无尽模式）、关卡星级评定（时间 + 击杀 + 受击 + 收集 → 星级 → 奖励倍率）、多目标（生存、击杀、收集、护送、限时）、随机事件（BUFF / 陷阱 / 宝箱 / 精英怪）、关卡选择 / 解锁地图（节点 → 星级解锁 → 路径选择 → Boss）。
4. UI 与表现：完整 HUD（血量 + 技能冷却 + 连击 + 波次 + 分数 + 小地图）、结算面板（星级动画 + 统计 + 奖励 + 重试/下一关）、主菜单 / 大厅（关卡选择 + 角色配置 + 商店 + 设置 + 成就）、过渡动画（加载 / 波次 / 结算 / 死亡）、伤害数字 / 浮动文字（伤害、暴击、治疗、状态）。
5. 资源与收集：局内金币 / 资源掉落（生成 + 拾取 + 磁铁 + 统计）、宝箱 / 奖励房间（宝箱 → 动画 → 随机奖励 → 稀有度）、收集品图鉴（定义 → UI → 进度 → 奖励）、每日 / 每周任务（刷新 → 进度 → 奖励 → 红点）、成就系统（条件 → 解锁通知 → 列表 → 点数）。
6. 叙事与氛围：剧情对话（触发 → UI → 选项分支 → 记录）、环境叙事（可交互叙事物件 + 文字提示 + 收集品描述）、音乐 / 音效管理（BGM 切换 + 音效优先级 + 音量控制 + 音频混合）、天气 / 环境效果（天气变化 → 视觉特效 → 游戏性影响）。

---

## 7. 执行步骤

### 7.1 读取并理解 Agent 体系文件

读取：
- `Agent/README.md`
- `Agent/Docs/ImplementationRoadmap.md`
- `Agent/Docs/SkillCatalog.md`
- `Agent/Config/agent_registry.json`
- `Agent/Config/task_router.json`
- `Agent/Templates/agent_task_card.md`

### 7.2 读取/创建全局候选发现报告

读取 `Agent/Reports/ambitious_discovery.md`；不存在则自动创建。若已存在，必须读取所有候选，尤其 `[DONE]`、`[SKIPPED]`、`[BLOCKED]`、`[PARTIAL]`，避免重复生成或重复实现。

### 7.3 只读扫描项目上下文

重点检查：
- README、Agent 文档、历史任务卡后续建议。
- `Game.unity` 真实路径及现有 UI 层级。
- `ResourcesLocal` 下 UI、Prefab、HUD、Popup、Panel、Guide、Settlement、Reward、Skill、Achievement 等目录结构。
- `Scripts/2D` 中业务脚本、管理器、玩家、敌人、关卡、UI、奖励、任务逻辑。
- `Scripts/2D/Tool` 工具类、公共函数、辅助方法、命名空间、代码风格、可复用能力；包括 UI 创建、Canvas/EventSystem 检查、GameObject/Component 安全获取、Resources/ResourcesLocal 路径、格式化、事件分发、消息通知、状态提示、空引用保护、默认值、安全集合访问、资源加载、Prefab 实例化、对象命名、调试日志、运行时状态输出、报告文本生成等。
- `Scripts/2D/Enum` 枚举、命名、成员风格、用途、可复用业务状态；重点查战斗反馈、奖励、关卡结果、UI 面板、HUD 状态、引导、成长、验证状态等。
- `Scripts/2D/Constant` 常量类、命名、分组、用途、可复用固定值；重点查路径、UI 文案、节点名、Prefab 名、默认奖励值、显示时长、评分阈值、星级阈值、事件名、日志前缀等。
- 当前项目重复的 UI 创建、数值计算、奖励结算、路径处理、组件查找、事件通知、格式化逻辑；若本次会使用，优先复用或抽取到 `Tool`。
- 当前项目重复的状态/奖励/UI 类型/关卡结果/成长类型枚举；若本次会使用，优先复用或抽取到 `Enum`。
- 当前项目重复的魔法字符串、魔法数字、固定路径、文案、节点名、Prefab 名、阈值、事件名；若本次会使用，优先复用或抽取到 `Constant`。
- `Scripts/2D` 中 TODO、FIXME、空方法、临时实现、重复模式。
- 已有 Feature / Efficiency 任务卡和验证记录，了解已实现/跳过功能。
- 半完成链路：有数据无 UI、有 UI 无行为、有行为无验证、有资源无检查、有事件无反馈、有结果无统计、有系统无深度。
- 玩家输入、战斗、关卡、资源、奖励、任务、成就、UI、音效、动画等可大幅扩展机会。
- `Resources/SO`、`Resources/Tilemap`、`Resources/Images`、`ResourcesLocal`、Prefab、Scene 中的业务配置和资源信号。
- 存档、Photon、AssetBundle、资源引用等高风险区域仅做了解性只读检查。
- `Agent/Reports/ambitious_discovery.md`、`feature_discovery.md`、`efficiency_discovery.md` 的候选状态，作为去重参考。
- `Agent/Reports/` 下所有历史日期目录及子任务目录中的 `task_*.md`、`validation_*.md`。
- 历史 `[DONE]` 候选，避免重复；旧版任务目录 `ambitious_discovery.md` 仅兼容读取，不作为新写入目标。

### 7.4 生成或更新全局功能发现报告

更新 `Agent/Reports/ambitious_discovery.md`，必须包含：
- 全局候选功能列表、扫描范围。
- 候选唯一 ID（`A001` 等）、状态（`[TODO]` / `[DONE]` / `[SKIPPED]` / `[BLOCKED]` / `[PARTIAL]`）。
- 业务类型、来源信号、玩家价值、开发价值、风险、成本、优先级、推荐 Agent、推荐 Skill、预计影响范围。
- 推荐优先开发的 1–3 个游戏体验升级功能。
- 被跳过高风险候选及原因。
- 已完成候选任务卡路径、修改文件、验证记录摘要。
- 历史已完成候选去重依据。
- 已发现的可复用工具类、公共枚举、公共常量。
- 本次候选可能需要复用或新增的 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`。

注意：
- 不覆盖已有候选状态；已有 `[DONE]` 不得改回 `[TODO]`。
- 相似候选合并到已有处理说明，不重复分配新 ID。
- 新增 ID 延续已有编号，如已有 A001–A003，则从 A004 开始。
- 候选必须以游戏体验升级为主，不选纯工程工具、纯扫描器、纯报告生成器。
- 功能范围明显大于 `Prompt_Feature.md`，具备“大刀阔斧”特征。
- UI 类候选不得因涉及 Scene / Prefab 默认跳过，应优先判断能否通过 `Game.unity` 独立 UI、`ResourcesLocal` 独立 UI Prefab、Editor 菜单或运行时代码安全落地。
- 核心体验依赖 UI 但无法安全生成 UI 时，至少实现业务逻辑 + UI Binder / ViewModel / Editor 生成工具，并在任务卡标记 `[PARTIAL]` 或说明降级。
- 不因需要新增公共枚举/常量就散落在具体业务脚本中，应优先规划到 `Enum` / `Constant`。

### 7.5 自动选择一个立即开发候选

选择规则：
- 只能从 `[TODO]` 选择。
- 不得重复选择历史 `[DONE]`，不得与 `feature_discovery.md`、`efficiency_discovery.md` 中已完成候选实质重复。
- 优先 P0，其次 P1。
- 必须中风险或高风险但边界明确、能在一个任务卡内完成核心功能、不会完全破坏项目可运行性。
- 允许涉及 Scene、Prefab、ScriptableObject、StreamingAssets，但必须有明确回滚方案。
- 涉及 UI / HUD / 面板 / 弹窗 / 浮动文字 / 引导 / 结算时，优先选择能在 `Game.unity` 或 `ResourcesLocal` 产生可见结果的候选。
- 优先“新增独立系统 + 最小侵入已有系统 + 可见 UI 表现”的中大型功能。
- Photon 同步逻辑或 AssetBundle 配置深度修改需特别评估；风险过高则跳过并标记 `[SKIPPED]`。
- 需要人工确认的复杂场景 / UI 布局，可先实现核心业务逻辑，并优先提供 `Game.unity` 示例节点、`ResourcesLocal` 预制体或 Editor 生成工具；若都不可行，任务卡说明后续人工接入方式。
- 若需新增公共函数、枚举、常量，必须优先规划到 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`。

### 7.6 生成本次任务目录

确保 `Agent/Reports/<今天日期>/` 存在，然后创建：
- 已选候选：`Agent/Reports/<今天日期>/ambitious_<候选ID>_<功能名安全短名>/`
- 未确定候选：`Agent/Reports/<今天日期>/ambitious_run_<HHmmss>/`

选定后可继续使用 run 目录或重命名为正式目录。若目录调整，任务卡、验证记录、回滚方案、补充文件必须全部位于最终 `<TASK_DIR>`。不得混写多个任务输出；不得在 `<TASK_DIR>` 创建 `ambitious_discovery.md`。以下统一用 `<TASK_DIR>` 表示本次任务目录。

### 7.7 生成任务卡

任务卡路径：`<TASK_DIR>/task_ambitious_<候选ID>_<功能名安全短名>.md`

任务卡必须包含：
- 候选ID、原始候选、当前状态、本次任务目录、全局候选报告路径 `Agent/Reports/ambitious_discovery.md`。
- 任务分类：游戏体验升级；游戏业务类型；玩家价值；开发价值；预计影响范围；负责 Agent；需要 Skill。
- 影响路径：所有将新增/修改的目录和文件类型。
- 不应触碰路径：如 Photon 同步核心、AssetBundle 配置等。
- 风险等级；功能边界（本次包含/不包含子模块）。
- 业务规则说明、数据流说明。
- UI 接入策略；Scene / Prefab / ResourcesLocal 生成策略。
- 资源修改清单：需修改或新增的 Scene / Prefab / ScriptableObject / StreamingAssets 及方式。
- 执行步骤：每步含目标、涉及文件、操作方式、完成标准。
- 验证步骤。
- 回滚方案：如何撤销 Scene / Prefab / ScriptableObject / UI Prefab 等修改并恢复。
- 工具类复用策略：已检查 `Scripts/2D/Tool`、计划复用的工具类和方法、计划新增/扩展公共工具函数、子模块共享能力、哪些逻辑放入 `Tool`。
- 枚举复用策略：已检查 `Scripts/2D/Enum`、计划复用枚举、计划新增/扩展公共枚举、共享状态/类型/结果/奖励/UI/关卡/成长枚举、哪些枚举放入 `Enum`。
- 常量复用策略：已检查 `Scripts/2D/Constant`、计划复用常量、计划新增/扩展公共常量、共享路径/文案/节点名/Prefab 名/默认值/阈值/事件名、哪些常量放入 `Constant`。
- 哪些逻辑保留在 Gameplay / UI / Level / Rewards / Progression 等业务脚本。
- 是否涉及 `UnityEditor` API；若涉及，如何避免污染 `Scripts/2D/Tool` 运行时代码。
- 若未使用 `Tool` / `Enum` / `Constant`，必须说明原因。
- 结果区。

### 7.8 按任务卡实现

- 严格按任务卡执行。
- 可新增脚本、业务管理器、数据结构、事件监听器、UI 组件、Editor 菜单、调试输出。
- 公共函数和重复逻辑优先入 `Scripts/2D/Tool`；公共枚举优先入 `Scripts/2D/Enum`；公共常量优先入 `Scripts/2D/Constant`。
- 战斗、关卡、奖励、成长、任务、成就等业务逻辑放对应业务目录。
- UI Controller / UI Binder / ViewModel / Panel 控制放 `Scripts/2D/UI` 或已有 UI 目录。
- Editor 菜单、安装器、Prefab 生成器放 `Scripts/2D/Editor` 或已有 Editor 目录。
- UI 实现按优先级：`Game.unity` → `ResourcesLocal` Prefab → Editor 工具 → 运行时代码动态创建 → 纯业务逻辑/ViewModel/数据层/人工接入说明。
- 修改 Scene / Prefab / ScriptableObject / StreamingAssets 前，在任务卡记录修改前状态；保留/同步 `.meta`；尽量新增而非修改；已有字段/节点只追加不破坏；已有 UI 层级只追加独立节点，不重排或删除。
- 保持现有命名、目录结构、代码风格；不做无关重构；不修改用户已有无关改动。
- 新增代码中文注释必须说明用途、接入方式、子模块关系、风险边界；修改已有代码时用中文注释标注修改原因和范围。
- 子模块通过事件或接口解耦，降低依赖。

### 7.9 完成后验证

至少做静态检查或编译相关检查；不能运行 Unity 编译或 Play Mode 时，在任务卡写明未验证原因。验证记录写入：

`<TASK_DIR>/validation_ambitious_<候选ID>.md`

验证必须覆盖：
- 新增运行时业务脚本：类名、命名空间、Unity API、脚本路径、基础逻辑。
- 新增 Editor / 报告工具：菜单路径、输出路径、基本扫描逻辑。
- 新增数据模型/管理器：默认值、空引用保护、调用边界。
- 修改 Scene / Prefab / ScriptableObject：`.meta` 是否同步、已有字段是否保留、新增内容是否挂载、回滚路径是否有效。
- 新增 UI 到 `Game.unity`：路径、根节点命名、Canvas 层级、锚点/布局/字体/默认显示状态、是否避免破坏已有 UI。
- 新增 UI Prefab 到 `ResourcesLocal`：路径规范、`.meta`、UI 层级、脚本引用、接入方式。
- 运行时代码动态 UI：Canvas / EventSystem 检查、默认启用策略、空引用保护、销毁/隐藏逻辑。
- `Scripts/2D/Tool` 新增/修改：路径、命名空间、是否错误引用 `UnityEditor`、是否影响运行时构建、是否破坏调用方、空引用保护、异常保护/失败返回、是否适用于多个子模块、中文注释。
- `Scripts/2D/Enum` 新增/修改：路径、命名、语义、是否重复/冲突、是否错误修改/删除/重命名/改变显式值、中文注释、各模块是否正确引用而非重复定义、多个子模块是否复用。
- `Scripts/2D/Constant` 新增/修改：路径、命名、分组、是否重复/冲突、是否错误改值/删除/重命名、中文注释、各模块是否正确引用而非硬编码、多子模块是否复用。
- Editor 工具调用 `Tool` 时，验证 Editor 专用逻辑与运行时公共逻辑已分离。
- 若本次未使用 `Tool` / `Enum` / `Constant`，验证记录说明原因。
- 若存在重复逻辑/枚举/常量但未抽取，说明暂不抽取原因。
- 若包含多个子模块，验证没有重复实现相同公共逻辑、枚举或常量。

### 7.10 更新任务卡结果区

写入：
- 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`。
- 已完成内容：按子模块列出。
- 修改文件：区分新增文件和修改文件。
- 新增游戏体验能力。
- 玩家侧效果。
- UI 生成位置：是否写入 `Game.unity`、是否创建 `ResourcesLocal` Prefab、是否改用 Editor 工具、是否改用运行时代码动态创建、哪些 UI 仍需人工接入。
- 开发侧接入方式、验证结果、验证记录路径。
- 回滚方案验证：是否验证回滚路径有效。
- 未完成项、剩余风险、后续建议（人工调整 UI 布局、平衡数值、添加美术资源等）。
- `Scripts/2D/Tool`：是否复用、是否新增/修改、工具类/函数路径、用途、哪些子模块使用、是否涉及 `UnityEditor`、是否完成 Editor 与运行时逻辑拆分、是否存在未抽取重复逻辑。
- `Scripts/2D/Enum`：是否复用、是否新增/修改、枚举路径、用途、哪些子模块引用、是否存在未抽取重复枚举。
- `Scripts/2D/Constant`：是否复用、是否新增/修改、常量路径、用途、哪些子模块引用、是否存在未抽取重复常量或魔法值。

### 7.11 回写全局功能发现报告

打开 `Agent/Reports/ambitious_discovery.md`，找到本次候选ID，将 `[TODO]` 更新为：
- `[DONE]`：功能已实现且完成可行验证。
- `[PARTIAL]`：核心已完成但有子模块未完成。
- `[BLOCKED]`：因环境、依赖或权限未完成。

在“处理说明”补充：
- 本次任务目录、任务卡路径、验证记录路径、修改文件清单。
- 新增游戏体验能力摘要、UI 生成方式摘要、各子模块完成状态、验证结果摘要。
- 是否复用 `Scripts/2D/Tool`，新增/修改公共工具类路径，新增公共函数摘要，各子模块如何调用。
- 是否复用 `Scripts/2D/Enum`，新增/修改公共枚举路径，新增公共枚举摘要，各子模块如何引用。
- 是否复用 `Scripts/2D/Constant`，新增/修改公共常量路径，新增公共常量摘要，各子模块如何引用。
- 是否存在后续可继续抽取的公共逻辑/枚举/常量。
- 是否存在因大功能边界暂未抽取的重复逻辑/枚举/常量。
- 是否仍有剩余风险；是否需要人工接入 UI / Scene / Prefab / 配置；回滚方案是否已验证。

自动跳过候选时，状态更新为 `[SKIPPED]` 并写明原因。不得把状态回写到任务目录下的 `ambitious_discovery.md`。

---

## 8. 最终回复要求

最终回复只需简洁汇总：
- 全局候选报告路径。
- 本次任务目录。
- 自动选择的游戏体验升级功能。
- 候选ID。
- 游戏业务类型。
- 最终状态。
- 修改文件：区分新增和修改。
- 新增游戏体验能力：按子模块列出。
- 玩家侧效果。
- UI 是否已生成到 `Game.unity`。
- UI 是否已生成到 `ResourcesLocal` Prefab。
- 若未生成 UI，说明采用的降级方案。
- 是否需要后续人工接入。
- 是否复用 `Scripts/2D/Tool`。
- 是否新增或修改 `Scripts/2D/Tool`。
- 是否复用 `Scripts/2D/Enum`。
- 是否新增或修改 `Scripts/2D/Enum`。
- 是否复用 `Scripts/2D/Constant`。
- 是否新增或修改 `Scripts/2D/Constant`。
- 任务卡路径。
- 验证记录路径。
- 验证结果。
- 回滚方案是否已验证。
- 剩余风险。
