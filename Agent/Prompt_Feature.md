# Unity 游戏业务功能自动开发 Prompt

请基于 `./Agent` 中的 Agent 体系，自动完成一次完整流程：

> 发现游戏业务功能缺口 → 选择最适合的业务候选 → 生成任务卡 → 开发新功能 → 验证记录 → 更新候选完成状态

## 1. 任务目标

- 自动为当前 Unity 游戏项目发现一个可落地的游戏业务相关新功能。
- 自动选择一个低风险或中风险、边界清晰、能在一次任务中完成的候选。
- 自动完成代码实现、基础验证、任务记录和候选状态回写。
- 一次只开发一个游戏业务新功能，不并行开发多个功能。
- 若功能包含 UI 展示、UI 反馈、提示面板、结算面板、奖励弹窗、任务提示、交互提示、状态提示等，应尽量生成可直接接入 Unity 的 UI 资源，而不是只写纯代码说明。

## 2. 总体强约束

- 不要向我提问，不要等待我确认。
- 遇到需要人工确认的高风险候选，自动跳过并选择下一个低风险或中风险候选。
- 候选功能总表统一维护在 `Agent/Reports/feature_discovery.md`；不要在每个任务目录下重复创建该文件。
- 每次任务必须在 `Agent/Reports/<今天日期>/` 下创建独立任务文件夹，任务卡、验证记录、补充报告等都放入其中，避免混写。
- 游戏业务功能任务输出目录必须使用固定前缀 `feature_`，避免与其他 Prompt 输出或同日同候选 ID / 短名任务冲突。
- 任务目录命名：`Agent/Reports/<今天日期>/feature_<候选ID>_<功能名安全短名>/`；候选 ID 未确定时先用 `Agent/Reports/<今天日期>/feature_run_<HHmmss>/`，选定后可继续使用或重命名。
- 同一天多次执行不得覆盖已有任务目录；若目录已存在，自动追加时间戳或序号。
- 优先选择与游戏核心体验、玩家成长、关卡反馈、奖励反馈、交互体验、战斗/操作反馈、任务目标、成就统计、资源收集、引导提示等相关的业务功能。
- 优先选择低风险、高价值、边界清晰、不破坏现有资源和存档结构的新功能。
- 不要优先选择纯工程类工具，如只读扫描器、通用报告生成器、模板生成器、资源完整性检查器，除非没有任何安全可实现的游戏业务候选。
- 不得修改存档结构、Photon 同步逻辑、AssetBundle 配置、StreamingAssets 中的运行时资源结构。
- 不得删除、重命名或覆盖已有 Scene、Prefab、ScriptableObject、材质、图片、动画、音效或配置资源。
- 所有生成代码的注释必须使用中文。

## 3. Scripts/2D 公共代码优先规则

新增或修改任何业务脚本前，必须先扫描并复用以下目录中的已有能力，避免重复定义、硬编码和公共逻辑散落。

### 3.1 `Scripts/2D/Tool` 工具类优先规则

- 已有可复用工具类或方法必须优先调用，不重复实现相同逻辑。
- 可复用公共函数、通用逻辑、辅助方法应优先沉淀到 `Scripts/2D/Tool`，不得散落在业务脚本、UI 脚本、Manager 脚本或 Editor 工具脚本中。
- 新增通用能力时，优先在 `Scripts/2D/Tool` 新增或扩展工具类，再由业务脚本调用。
- 业务脚本只保留强相关业务流程、状态管理、事件响应和 UI 绑定逻辑。
- 工具类保持低耦合，不得强依赖具体 Scene、Prefab、ScriptableObject、存档结构、Photon、AssetBundle 或单一业务模块。
- 命名遵循项目风格；无明确风格时可用 `XxxTool`、`XxxUtility`、`XxxHelper`、`XxxRuntimeTool`。
- 工具方法必须有中文注释，说明用途、参数、返回值、使用边界和风险限制。
- 涉及 `UnityEditor` API 的公共逻辑不得污染运行时代码；优先放入 Editor 专用目录，或拆分为运行时工具类 + Editor 调用层，避免打包报错。

优先放入或复用 `Scripts/2D/Tool` 的逻辑包括但不限于：通用 UI 创建辅助，Canvas / Panel / Text / Button / Image 查找或创建，GameObject 查找/创建/挂载组件，组件安全获取、空引用保护、默认值处理，Resources / ResourcesLocal 路径辅助，数值/时间/分数/奖励文本格式化，事件分发、消息通知、状态提示，日志输出、Debug 文本生成，列表/字典/配置读取安全访问，以及可被多个业务功能复用的计算逻辑。

若公共逻辑只服务本次功能但未来明显可复用，也应优先设计为工具方法；若强依赖本次功能状态、具体业务流程或具体 UI 面板，则保留在本功能脚本中。不得为了复用让 `Scripts/2D/Tool` 反向依赖具体业务模块。工具类尽量使用静态方法或低状态对象，避免全局副作用。修改已有工具类必须兼容已有调用方，不破坏原有方法签名和行为。

### 3.2 `Scripts/2D/Enum` 枚举优先规则

- 已有语义一致或可扩展复用的枚举必须优先使用，不在业务脚本中重复定义相似枚举。
- 可被多个业务模块、UI 模块、Manager、数据结构、事件系统或配置读取逻辑复用的枚举，必须优先放到 `Scripts/2D/Enum`。
- 不得在业务脚本、UI 脚本、Manager 脚本或 Editor 工具脚本中随意内嵌公共枚举。
- 新增公共枚举时，在 `Scripts/2D/Enum` 下创建独立枚举文件。
- 仅服务某个类极小内部状态且不会外部复用的枚举，可保留为类内部私有枚举，但必须在任务卡中说明不抽取原因。
- 新增枚举命名需清晰表达业务语义，例如 `GameResultType`、`RewardState`、`FeatureDisplayState`、`InteractionPromptType`、`LevelGoalType`、`BattleFeedbackType`。
- 枚举成员遵循项目风格；无明确风格时使用 PascalCase。
- 枚举文件必须有中文注释，说明用途、每个枚举值含义、使用场景和是否允许后续扩展。
- 修改已有枚举必须兼容：不得删除、重命名、改变已有显式数值含义；必须扩展时只追加新枚举值，并在注释中说明新增原因。
- 若已有枚举命名或语义冲突，优先复用最贴近现有业务的枚举；无法安全判断时不强改旧枚举，可新增更明确枚举并在任务卡说明。

优先放入或复用 `Scripts/2D/Enum` 的定义包括但不限于：游戏结果、关卡状态、玩家状态、奖励状态、成就状态、任务状态、任务目标、UI 显示状态、UI 面板、提示消息、交互提示、战斗反馈、拾取反馈、冷却状态、业务事件、统计数据、功能模块等类型。不得在多个脚本中重复定义语义相同或近似的枚举。

### 3.3 `Scripts/2D/Constant` 常量优先规则

- 已有语义一致常量必须优先使用，不在业务脚本中重复写魔法数字、魔法字符串、固定路径、默认文案、默认时间、默认阈值或固定资源名。
- 可被多个业务模块、UI 模块、Manager、工具类、配置读取逻辑或 Editor 工具复用的常量，必须优先放到 `Scripts/2D/Constant`。
- 不得在业务脚本、UI 脚本、Manager 脚本或 Editor 工具脚本中散落公共常量。
- 新增公共常量时，优先在 `Scripts/2D/Constant` 下创建或扩展常量类。
- 仅服务某个类内部且不会复用的常量，可保留为类内 `private const` 或 `private static readonly`，但必须在任务卡中说明不抽取原因。
- 常量包括但不限于：UI 默认文案、UI 节点名、UI 面板名、Prefab 名称、Resources / ResourcesLocal 路径、菜单路径、Agent 报告路径、默认数值、时间/分数阈值、奖励倍率、Tag / Layer 名称、PlayerPrefs Key、事件名、日志前缀、配置 Key、默认颜色名或样式名。
- 常量类命名遵循项目风格；无明确风格时可用 `XxxConstant`、`XxxConstants`、`GameConstant`、`UIConstant`、`FeatureConstant`、`ResourcePathConstant`。
- 常量按业务语义分组，不要将无关常量塞入巨大类。
- 字段命名遵循项目风格；无明确风格时，公共常量用 PascalCase，私有常量用 camelCase 或项目私有字段风格。
- 新增常量必须有中文注释，说明用途、使用场景、默认值含义和修改风险。
- 修改已有常量必须兼容：不得随意改变公共常量值、删除或重命名公共常量；若新增替代常量，应保留旧常量并说明兼容关系。
- 路径类常量优先配合 `Scripts/2D/Tool` 中的路径处理工具使用，避免硬编码路径拼接逻辑散落。
- 不得在多个脚本中重复硬编码相同字符串、路径、数值阈值或 Key。

## 4. 公共代码分层原则

新增或修改代码时按以下优先级组织：

1. 公共枚举：放入 `Scripts/2D/Enum`，表达稳定业务类型、状态、结果、提示、奖励等。
2. 公共常量：放入 `Scripts/2D/Constant`，表达稳定字符串、路径、默认值、阈值、Key、节点名、菜单路径等。
3. 公共函数与辅助逻辑：放入 `Scripts/2D/Tool`，表达可复用计算、查找、创建、安全访问、格式化、路径处理、日志、事件辅助等。
4. 具体业务脚本：放入 `Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Domain` 或项目已有目录，只保留业务流程、状态管理、事件响应、UI 绑定和功能入口。
5. Editor 专用逻辑：放入 Editor 专用目录，不得让运行时代码直接依赖 `UnityEditor`。

不得将公共枚举、公共常量、公共工具函数混写在单个业务脚本中。

## 5. UI / Scene / Prefab 生成规则

候选功能包含 UI 时，必须按以下顺序实现，并在任务卡与验证记录中说明选择原因。

### 5.1 优先在 `Game.unity` 场景中生成 UI

- 先搜索 `Game.unity` 真实路径，不凭空假定；常见路径包括 `Assets/Scenes/Game.unity`、`Assets/Game.unity`、`Assets/Resources/Scenes/Game.unity` 或项目实际路径。
- 若可安全修改 `Game.unity`，允许新增独立 UI 根节点，如 `Canvas`、`EventSystem`、`Feature_<候选ID>_<功能名>_Root`、Panel、Text、Button、Image、提示条、弹窗等。
- 新增 UI 节点必须独立，不破坏已有 UI 层级、引用、脚本绑定和对象命名。
- 若已有 Canvas，优先在已有 Canvas 下新增独立子节点；若无 Canvas，可新增独立 Canvas，但必须避免影响摄像机、渲染、输入系统和场景流程。
- UI 对象命名清晰并带候选 ID 或功能短名，便于回滚。
- UI 节点名称、默认文案、资源路径、Prefab 名称优先使用 `Scripts/2D/Constant`；UI 状态、提示类型、显示类型优先使用 `Scripts/2D/Enum`。
- 只挂载本次新增脚本或明确兼容的已有脚本。
- 不得直接修改已有核心 UI 节点字段，除非低风险且必要，并在任务卡说明原因。
- 若无法确认 YAML 场景文件可安全修改，不要手写大段 Scene YAML，优先改用 Editor 菜单工具或 Prefab 生成方式。

### 5.2 其次在 `ResourcesLocal` 下创建 UI 预制体

- 无法安全修改 `Game.unity` 或项目更适合 Prefab 接入时，在 `ResourcesLocal` 下创建 UI Prefab。
- 优先路径：`Assets/ResourcesLocal/UI/<功能名安全短名>/`、`Assets/ResourcesLocal/Prefabs/UI/<功能名安全短名>/`、`Assets/ResourcesLocal/<已有项目约定目录>/<功能名安全短名>/`。
- 创建独立 Prefab，例如 `Feature_<候选ID>_<功能名安全短名>Panel.prefab`、`Toast.prefab`、`Popup.prefab`。
- 必须遵循项目已有 ResourcesLocal 的 UI / Prefab 目录规范。
- 新增 Prefab 必须保留 `.meta`，不得覆盖已有 Prefab。
- Prefab 尽量包含完整 UI 层级、默认文案、基础样式、挂载脚本和必要组件。
- Prefab 路径、名称、默认文案、节点名称优先沉淀到 `Scripts/2D/Constant`；显示状态、交互状态、奖励状态、提示类型优先沉淀到 `Scripts/2D/Enum`。
- Prefab 运行时脚本放入合适路径，如 `Scripts/2D/UI/`、`Scripts/2D/Gameplay/`。
- 若图片、字体、材质等引用无法安全判断，使用 Unity 默认 UI 组件或已有可安全引用资源。
- 若无法生成真实 Prefab 文件，改为生成 Editor 菜单工具，由开发者在 Unity 中点击菜单自动创建 Prefab。

### 5.3 最后才退回代码或说明

只有在找不到或无法安全修改 `Game.unity`、无法确认 Scene / Prefab YAML、当前环境无法运行 Unity Editor 或可靠生成 Prefab、缺少 UI 包或组件、资源路径/字体/图片/Canvas 结构无法确定、自动修改可能破坏已有引用时，才允许退回低侵入方案：

- 新增运行时 UI 管理器，在运行时自动创建 Canvas / Panel / Text / Button。
- 新增可选挂载组件，由开发者挂到 GameObject 后自动生成 UI。
- 新增 Editor 菜单工具，如 `Tools/Game Features/Create <功能名> UI In Game Scene` 或 `Tools/Game Features/Create <功能名> Prefab In ResourcesLocal`。
- 新增 ViewModel / UI 数据源 / UI Binder，方便后续接入。
- 在任务卡写明：为什么没有直接生成到 `Game.unity`，为什么没有创建 `ResourcesLocal` 预制体，后续如何手动或通过菜单工具接入，需要挂载到哪个场景对象或 Canvas 下。

UI 实现优先级固定为：安全修改 `Game.unity` → `ResourcesLocal` 独立 UI Prefab → Editor 菜单工具 → 运行时代码动态创建 UI → 仅数据层 / ViewModel / 人工接入说明。选择第 3、4、5 种方式时，必须在任务卡和验证记录中写明原因。

## 6. 候选状态与候选表

候选状态：

- `[TODO]`：待处理，尚未实现。
- `[DONE]`：已完成，已实现并完成可行验证。
- `[SKIPPED]`：已跳过，通常因为高风险、边界不清晰、需要人工确认或涉及敏感资源修改。
- `[BLOCKED]`：受阻，已分析但因缺少环境、依赖、权限或无法验证而未完成。
- `[PARTIAL]`：部分完成，已实现部分能力，但仍有明确未完成项。

候选功能列表必须使用以下格式：

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|

## 7. 游戏业务候选重点方向

- 玩家体验类：连击反馈、受击反馈、拾取反馈、任务目标提示、新手引导提示、关卡完成反馈、失败原因提示、交互提示、冷却提示、状态变化提示。
- 成长与奖励类：经验值统计、局内积分统计、简易成就条件、奖励领取记录、关卡星级计算、战斗评分计算、资源收集统计、每日目标数据层、任务完成条件判断。
- 关卡与玩法类：关卡目标管理器、波次进度统计、敌人击杀计数、存活时间统计、玩家死亡原因记录、关卡结果数据结构、可配置玩法参数读取、游戏状态流转辅助逻辑。
- UI 数据与表现类：结算面板、奖励弹窗、任务目标提示面板、玩家状态展示 UI、战斗反馈浮字、拾取提示条、交互提示框、冷却提示 UI、新手引导提示框、UI 文案配置读取、红点状态计算逻辑、面板 ViewModel 与 Binder。
- 低风险业务辅助类：业务事件总线、游戏内事件统计器、任务条件检查器、奖励配置只读校验、关卡配置只读校验、玩家行为日志、Debug 面板数据输出、运行时状态报告。

## 8. 执行步骤

### 8.1 读取 Agent 体系文件

读取并理解：

- `Agent/README.md`
- `Agent/Docs/ImplementationRoadmap.md`
- `Agent/Docs/SkillCatalog.md`
- `Agent/Config/agent_registry.json`
- `Agent/Config/task_router.json`
- `Agent/Templates/agent_task_card.md`

### 8.2 读取全局候选功能发现报告

读取 `Agent/Reports/feature_discovery.md`；若不存在则自动创建。若已存在，必须读取已有候选，尤其是 `[DONE]`、`[SKIPPED]`、`[BLOCKED]`、`[PARTIAL]`，避免重复生成或重复实现。

### 8.3 只读扫描项目上下文

重点检查：README、Agent 文档、历史任务卡后续建议；`Game.unity` 真实路径及 UI 层级；`ResourcesLocal` 下 UI / Prefab / Panel / Popup / Toast / HUD 目录；`Scripts/2D` 中业务脚本、管理器、玩家控制、敌人逻辑、关卡逻辑、UI 逻辑、奖励逻辑、任务逻辑；`Scripts/2D/Tool` 工具类、公共函数、辅助方法、命名空间、代码风格、可复用 UI 创建/对象查找/组件获取/资源加载/事件分发/数值计算/格式化/路径处理/日志输出/空引用保护能力；`Scripts/2D/Enum` 已有状态、类型、结果、提示、奖励、交互、关卡等枚举及风格；`Scripts/2D/Constant` 已有路径、UI 文案、节点名、Prefab 名、默认值、阈值、事件名等常量及风格；项目中重复公共逻辑、重复枚举、魔法字符串/数字/路径/文案，必要时优先抽取或复用到 Tool / Enum / Constant；`Scripts/2D` 中 TODO、FIXME、空方法、临时实现、重复模式；“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查 / 有事件但无反馈 / 有结果但无统计”的半完成业务链路；玩家输入、战斗、关卡、资源、奖励、任务、成就、UI、音效、动画等模块中的低侵入新功能机会；`Resources/SO`、`Resources/Tilemap`、`Resources/Images` 中的业务配置和资源使用信号但只读分析，不直接修改敏感资源；存档、Photon、AssetBundle、资源引用等高风险区域的只读检查机会；`Agent/Reports/feature_discovery.md` 现有状态；`Agent/Reports/` 下所有历史日期目录及子任务目录中的 `task_*.md` 和 `validation_*.md`；历史 `[DONE]` 候选避免重复；历史任务目录遗留旧版 `feature_discovery.md` 仅兼容读取，不再作为新写入目标。

### 8.4 生成或更新全局功能发现报告

更新 `Agent/Reports/feature_discovery.md`，必须包含：全局候选功能列表、扫描范围、唯一候选 ID、状态标记、业务类型、来源信号、玩家价值、开发价值、风险、成本、优先级、推荐 Agent、推荐 Skill、推荐优先开发的 1-3 个游戏业务功能、被跳过高风险候选及原因、已完成候选的任务卡路径/修改文件/验证摘要、历史已完成候选去重依据、已发现的可复用工具类/公共枚举/公共常量、本次候选可能需要复用或新增的 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`。

更新时不得覆盖已有候选状态；不得把已有 `[DONE]` 改回 `[TODO]`；相似候选合并到已有处理说明，不重复分配 ID；新增候选延续已有编号，如已有 F001-F006，则从 F007 开始；候选以游戏业务功能为主，不把普通代码扫描器、文档生成器、模板工具作为首选；UI 类候选不得因涉及 UI 默认跳过，应优先判断能否通过 `Game.unity` 独立 UI、`ResourcesLocal` Prefab、Editor 菜单工具或运行时代码安全实现；只有没有安全可实现业务功能时，才允许选择业务辅助类或只读分析类。

### 8.5 自动选择一个最适合立即开发的候选

只能从 `[TODO]` 中选择，不得重复选择历史 `[DONE]`；优先 P0，其次 P1；必须低/中风险、边界清晰、可在一个任务卡内完成、不会破坏业务数据或 Unity 资源引用；优先通过新增脚本、数据模型、管理器、事件监听器、ViewModel、Editor 菜单、Debug 输出、`Game.unity` 独立 UI 节点或 `ResourcesLocal` 独立 UI Prefab 完成；若涉及直接修改存档结构、Photon 同步、AssetBundle 配置、StreamingAssets 运行时资源结构，则跳过并标记 `[SKIPPED]`；UI / Scene / Prefab 候选应先尝试低风险实现，不直接跳过；若无法自动安全接入 UI / Scene / Prefab，可先实现底层逻辑、运行时 UI 创建逻辑或 Editor 创建工具，并在任务卡明确人工接入方式；需要公共枚举或常量时必须优先规划放入 `Scripts/2D/Enum`、`Scripts/2D/Constant`，不得散落在业务脚本中。

### 8.6 生成本次任务目录

确保 `Agent/Reports/<今天日期>/` 存在；创建 `Agent/Reports/<今天日期>/feature_<候选ID>_<功能名安全短名>/`。若候选 ID 未确定，先创建 `Agent/Reports/<今天日期>/feature_run_<HHmmss>/`。不得多个任务混写同一目录，不得在 `<TASK_DIR>` 中创建新的 `feature_discovery.md`。以下统一用 `<TASK_DIR>` 表示本次任务目录。

### 8.7 生成任务卡

任务卡路径：`<TASK_DIR>/task_feature_<候选ID>_<功能名安全短名>.md`。

任务卡必须包含：候选 ID、原始候选、当前状态、本次任务目录、全局候选报告路径 `Agent/Reports/feature_discovery.md`、任务分类、游戏业务类型、玩家价值、开发价值、负责 Agent、需要的 Skill、影响路径、不应触碰路径、风险等级、功能边界、业务规则说明、数据流说明、UI 接入策略、Scene / Prefab / ResourcesLocal 生成策略、执行步骤、验证步骤、回滚方案、工具类复用策略、已检查的 `Scripts/2D/Tool` 工具类、本次计划复用的工具类和方法、本次计划新增或扩展的公共工具函数、枚举复用策略、已检查的 `Scripts/2D/Enum` 枚举、本次计划复用的枚举、本次计划新增或扩展的公共枚举、常量复用策略、已检查的 `Scripts/2D/Constant` 常量类、本次计划复用的常量、本次计划新增或扩展的公共常量、哪些逻辑保留在业务脚本中、哪些公共函数沉淀到 `Scripts/2D/Tool`、哪些公共枚举沉淀到 `Scripts/2D/Enum`、哪些公共常量沉淀到 `Scripts/2D/Constant`、若未使用 Tool / Enum / Constant 必须分别说明原因、结果区。

### 8.8 实现游戏业务新功能

实现时只修改与任务直接相关的文件，优先放在 `Scripts/2D`、`Scripts/2D/Gameplay`、`Scripts/2D/UI`、`Scripts/2D/Domain`、`Scripts/2D/Editor`、`Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`、`Agent` 或其他低侵入路径。公共函数/辅助逻辑放入 Tool；公共枚举/状态/类型/提示/奖励/结果放入 Enum；公共常量/路径/文案/节点名/Prefab 名/菜单路径/默认值/阈值/Key/事件名放入 Constant；具体业务流程、状态管理、事件响应、UI Binder、ViewModel 放入对应业务目录。

若包含 UI，按 `Game.unity` 独立 UI 节点 → `ResourcesLocal` 独立 UI Prefab → Editor 菜单工具 → 运行时代码动态创建 UI → 纯数据层/ViewModel/人工接入说明的顺序处理。可以新增独立脚本、业务管理器、数据结构、事件监听器、Editor 菜单、调试输出或只读报告。必须保持项目命名、目录结构和代码风格；不做无关重构；不修改用户已有无关改动；不删除、重命名、覆盖已有 Unity 资源；Unity 资源相关修改必须保留 `.meta`；若无法安全处理 `.meta`，不要直接修改资源，改为 Editor 工具或运行时代码方案。新增代码必须有清晰中文注释，说明用途、接入方式和风险边界。

### 8.9 Editor 专用逻辑要求

若新增 Editor 专用公共逻辑，必须避免运行时代码引用 `UnityEditor`。运行时通用部分放入 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`；Editor 菜单入口放入 Editor 专用目录。菜单路径、输出路径、默认文件名优先使用 Constant；生成类型、处理状态等优先使用 Enum。

### 8.10 完成后运行可行验证

至少做静态检查或编译相关检查；若不能运行 Unity 编译或 Play Mode，必须在任务卡说明未验证原因。验证记录写入 `<TASK_DIR>/validation_feature_<候选ID>.md`。

验证内容必须包含：运行时业务脚本的类名、命名空间、Unity API 使用、脚本路径、基础逻辑；UI 场景节点的 `Game.unity` 路径、对象命名、Canvas 层级、脚本挂载、回滚方式；UI Prefab 的路径、`.meta`、组件层级、脚本引用、ResourcesLocal 路径；Editor 工具的菜单路径、输出路径、基本生成逻辑；数据模型或管理器的默认值、空引用保护、调用边界。

若新增或修改 Tool，验证路径、命名空间、是否误引 `UnityEditor`、是否影响运行时构建、是否破坏已有调用方、公共函数是否有空引用保护、中文注释是否完整。若新增或修改 Enum，验证路径、命名、语义、是否重复或冲突、是否错误修改/删除/重命名已有值、是否改变显式值含义、中文注释、业务脚本是否正确引用而非重复定义。若新增或修改 Constant，验证路径、类命名、分组、是否重复或冲突、是否错误修改/删除/重命名公共常量、是否改变公共值导致兼容风险、中文注释、业务脚本是否引用常量而非继续硬编码。

若本次未使用 Tool / Enum / Constant，必须分别在验证记录中说明原因。若存在重复逻辑、重复枚举、重复常量或魔法值但未抽取，必须分别说明暂不抽取原因。

### 8.11 更新任务卡结果区

结果区必须写入：最终状态 `[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`；已完成内容；修改文件；新增游戏业务能力；玩家侧效果；UI 生成位置，包括是否写入 `Game.unity`、是否创建 `ResourcesLocal` Prefab、是否改用 Editor 工具、是否改用运行时代码动态创建；开发侧接入方式；验证结果；验证记录路径；未完成项；剩余风险；是否复用 `Scripts/2D/Tool`；是否新增或修改 Tool；新增公共工具类或函数路径及用途；是否复用 `Scripts/2D/Enum`；是否新增或修改 Enum；新增公共枚举路径及用途；是否复用 `Scripts/2D/Constant`；是否新增或修改 Constant；新增公共常量路径及用途；后续建议；是否存在未抽取的重复逻辑、重复枚举、重复常量或魔法值。

### 8.12 回写全局功能发现报告

打开 `Agent/Reports/feature_discovery.md`，将本次候选从 `[TODO]` 更新为 `[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`。在“处理说明”补充：本次任务目录、任务卡路径、验证记录路径、修改文件、新增业务能力摘要、UI 生成方式摘要、验证结果摘要、是否复用 Tool、公共工具类路径、新增公共函数摘要、功能脚本如何调用工具类、是否复用 Enum、公共枚举路径、新增公共枚举摘要、功能脚本如何引用枚举、是否复用 Constant、公共常量路径、新增公共常量摘要、功能脚本如何引用常量、后续可继续抽取的公共逻辑/枚举/常量、剩余风险、后续是否需要人工接入 UI / Scene / Prefab / 配置。

自动跳过的候选标记为 `[SKIPPED]` 并写明原因。不要把状态回写到任务目录下的 `feature_discovery.md`，因为该文件不应再存在于任务目录中。

## 9. 最终回复要求

最终回复只需简洁汇总：

- 全局候选报告路径
- 本次任务目录
- 自动选择的游戏业务新功能
- 候选 ID
- 游戏业务类型
- 最终状态
- 修改文件
- 新增业务能力
- UI 是否已生成到 `Game.unity`
- UI 是否已生成到 `ResourcesLocal` Prefab
- 若未生成 UI，说明采用的降级方案
- 是否需要后续人工接入
- 是否复用 `Scripts/2D/Tool`
- 是否新增或修改 `Scripts/2D/Tool`
- 是否复用 `Scripts/2D/Enum`
- 是否新增或修改 `Scripts/2D/Enum`
- 是否复用 `Scripts/2D/Constant`
- 是否新增或修改 `Scripts/2D/Constant`
- 任务卡路径
- 验证记录路径
- 验证结果
- 剩余风险
