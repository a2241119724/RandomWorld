# Unity 效率 / 工具类功能自动开发 Prompt

请基于 `./Agent` 中的 Agent 体系，自动完成一次完整流程：

> 发现功能缺口 → 选择最适合的候选 → 生成任务卡 → 实现 → 验证记录 → 更新候选完成状态

---

## 1. 总原则

- 不要向我提问，不要等待我确认。
- 一次只实现一个功能；如遇需要人工确认的高风险候选，自动跳过并选择下一个低风险或中风险候选。
- 所有生成代码的注释必须使用中文。
- 优先选择低风险、高价值、边界清晰、可提升后续开发效率的能力，例如：只读扫描器、Editor 工具、报告生成器、模板生成器、资源完整性检查器、调试面板、可视化报告入口、开发辅助 UI。
- 不优先选择会直接修改已有 `Scene`、已有 `Prefab`、已有 `ScriptableObject`、`StreamingAssets`、存档结构、Photon 同步或 AssetBundle 的功能。
- 如果候选涉及 UI 展示、调试面板、报告查看面板、开发者工具面板、运行时状态面板、资源扫描结果面板等，不要默认跳过；应优先尝试低风险独立 UI 方式实现。
- 如果所有候选均为高风险，只实现一个不改业务资产的只读分析 / 报告工具。
- 新增或修改代码时，必须遵守公共代码分层：公共枚举进 `Scripts/2D/Enum`，公共常量进 `Scripts/2D/Constant`，公共函数与辅助逻辑进 `Scripts/2D/Tool`，具体流程保留在对应功能脚本 / Editor 工具 / 报告生成器 / UI 脚本中。
- 不做无关重构，不修改用户已有的无关改动，不删除、不重命名、不覆盖已有 Unity 资源；Unity 资源相关修改必须保留 `.meta`。无法安全处理 `.meta` 时，不直接改资源，改为 Editor 工具或运行时代码方案。

---

## 2. 全局候选报告与任务目录

### 2.1 全局候选总表

- 全局候选功能总表固定为：`Agent/Reports/efficiency_discovery.md`
- 不要在每个任务目录下重复创建 `efficiency_discovery.md`。
- 如果 `Agent/Reports/efficiency_discovery.md` 不存在，自动创建；如果存在，必须读取已有 `[DONE]`、`[SKIPPED]`、`[BLOCKED]`、`[PARTIAL]` 和 `[TODO]` 状态，避免重复生成或实现。
- 扫描历史时，必须递归检查 `Agent/Reports/` 下所有日期目录及子任务目录中的 `task_*.md`、`validation_*.md`。
- 历史任务目录中若遗留旧版 `efficiency_discovery.md`，需要兼容读取，但新的候选总表只允许写入 `Agent/Reports/efficiency_discovery.md`。
- 生成候选列表时，必须为每个候选分配唯一编号和完成状态；实现与验证后，必须回写对应候选状态。

### 2.2 任务目录

- 每次任务必须在 `Agent/Reports/<今天日期>/` 下创建独立任务文件夹，任务卡、验证记录、补充报告等本次任务输出均放入该目录，避免同一天多个任务混写。
- 效率 / 工具类任务目录必须使用固定前缀 `efficiency_`，用于和 `Prompt_Feature.md` 的 `feature_` 输出隔离，避免同一天同候选 ID 或同短名路径冲突。
- 推荐目录格式：
  - `Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/`
  - 候选 ID 未确定时，先用：`Agent/Reports/<今天日期>/efficiency_run_<HHmmss>/`
  - 选定候选后，可继续使用临时目录，也可重命名为 `efficiency_<候选ID>_<功能名安全短名>`。
- 同一天多次执行时不得覆盖已有任务目录；如目录已存在，自动追加时间戳或序号。
- 以下统一以 `<TASK_DIR>` 表示本次任务目录。

---

## 3. 候选状态与候选表格式

### 3.1 状态规则

- `[TODO]`：待处理，尚未实现。
- `[DONE]`：已完成，已实现并完成可行验证。
- `[SKIPPED]`：已跳过，通常因为高风险、边界不清晰、需要人工确认或涉及敏感资源修改。
- `[BLOCKED]`：受阻，已分析但因缺少环境、依赖、权限或无法验证而未完成。
- `[PARTIAL]`：部分完成，已实现部分能力，但仍存在明确未完成项。

### 3.2 候选功能列表格式

必须包含以下表头：

| 状态 | 候选ID | 功能名称 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|

示例：

| 状态 | 候选ID | 功能名称 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|
| [TODO] | F001 | 资源引用完整性只读扫描器 | Resources/SO 中存在未校验引用 | 高 | 低 | 中 | P0 | ResourceAuditAgent | ReadOnlyScanSkill | 推荐优先实现 |
| [TODO] | F002 | 资源扫描结果调试面板 | 已有资源扫描结果但缺少可视化查看入口 | 中 | 低 | 中 | P1 | ToolUIAgent | EditorUISkill | 优先生成到 Game.unity；如不可行则创建 ResourcesLocal UI Prefab |
| [SKIPPED] | F003 | 自动修复 Prefab 缺失绑定 | Prefab 存在 Missing Reference | 高 | 高 | 高 | P1 | ResourceFixAgent | PrefabModifySkill | 涉及已有 Prefab 直接修改，跳过 |
| [DONE] | F004 | TODO/FIXME 报告生成器 | Scripts/2D 存在 TODO/FIXME | 中 | 低 | 低 | P1 | CodeAuditAgent | ReportGenerateSkill | 已完成；任务卡：Agent/Reports/2026-04-26/efficiency_F004_TODO_FIXME_Report/task_efficiency_F004_TODO_FIXME_Report.md；验证记录：Agent/Reports/2026-04-26/efficiency_F004_TODO_FIXME_Report/validation_efficiency_F004.md |

---

## 4. 必读 Agent 体系文件

执行前读取并理解：

- `Agent/README.md`
- `Agent/Docs/ImplementationRoadmap.md`
- `Agent/Docs/SkillCatalog.md`
- `Agent/Config/agent_registry.json`
- `Agent/Config/task_router.json`
- `Agent/Templates/agent_task_card.md`

---

## 5. 只读扫描范围

只读扫描项目上下文，重点检查：

- README、Agent 文档、历史任务卡中的后续建议。
- `Agent/Reports/efficiency_discovery.md` 中已有候选状态。
- `Agent/Reports/` 下所有历史日期目录及子任务目录中的 `task_*.md`、`validation_*.md`，以及旧版 `efficiency_discovery.md`。
- 历史 `[DONE]` 候选，避免重复实现。
- `Game.unity` 的真实路径及现有 UI 层级，不要凭空假定路径。
- `ResourcesLocal` 下已有 UI、Prefab、Panel、Popup、Debug、Tool、Report 等目录结构。
- `Scripts/2D` 中的 TODO、FIXME、空方法、临时实现、重复模式。
- `Scripts/2D/Editor`、`Scripts/2D/UI`、`Scripts/2D/Tool` 中已有工具、UI、调试逻辑。
- `Scripts/2D/Tool` 下已有工具类、公共函数、辅助方法、命名空间、代码风格和可复用能力，尤其是文件扫描、路径拼接、报告生成、Markdown 表格、JSON / 配置读取、日志、空引用保护、GameObject / Component 安全获取（FindChildComponent/GetComponentInChildren）、Resources / ResourcesLocal 路径、UI 节点查找等。
- `Scripts/2D/Enum` 下已有枚举、命名、成员风格、用途和可复用状态，包括扫描结果、报告类型、执行状态、严重级别、验证状态、调试面板类型等。
- `Scripts/2D/Constant` 下已有常量类、命名、分组、用途和可复用固定值，包括 Agent 报告路径、任务目录前缀、报告文件名、Editor 菜单路径、UI 文案、节点名、Prefab 名称、扫描扩展名、忽略目录、日志前缀等。
- 当前项目中重复的扫描逻辑、报告格式化逻辑、路径处理逻辑、文件读写逻辑、UI 节点查找逻辑；本次功能会继续使用类似逻辑时，优先复用或抽取到 `Scripts/2D/Tool`。
- 当前项目中重复的工具状态枚举、扫描结果枚举、报告类型枚举、严重级别枚举；本次功能会继续使用类似枚举时，优先复用或抽取到 `Scripts/2D/Enum`。
- 当前项目中重复的路径字符串、菜单路径、报告文件名、UI 文案、默认阈值、日志前缀、扩展名列表等魔法值；本次功能会继续使用类似固定值时，优先复用或抽取到 `Scripts/2D/Constant`。
- `Resources/SO`、`Resources/Tilemap`、`Resources/Images` 中的资源绑定缺口。
- 已有系统中“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查”的半完成链路。
- 已有报告、扫描器、Editor 工具是否缺少可视化入口或统一输出路径。
- 存档、Photon、AssetBundle、资源引用等高风险区域的只读检查机会。

---

## 6. 更新全局功能发现报告

更新 `Agent/Reports/efficiency_discovery.md`，必须包含：

- 全局候选功能列表、扫描范围、唯一候选 ID。
- 每个候选的状态：`[TODO]`、`[DONE]`、`[SKIPPED]`、`[BLOCKED]`、`[PARTIAL]`。
- 每个候选的来源信号、价值、风险、成本、优先级、推荐 Agent、推荐 Skill。
- 推荐优先开发的 1-3 个功能。
- 被跳过高风险候选及原因。
- 已完成候选的任务卡路径、修改文件、验证记录摘要。
- 历史已完成候选的去重依据。
- 已发现的可复用工具类、公共枚举、公共常量。
- 本次候选可能需要复用或新增的 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`。

更新约束：

- 不覆盖已有候选状态；已有 `[DONE]` 不得重新改为 `[TODO]`。
- 相似候选合并到已有候选处理说明中，不重复分配新 ID。
- 新增候选延续已有编号，例如已有 `F001-F006`，新增从 `F007` 开始。
- UI 类效率工具候选不得因涉及 UI 就默认跳过；先判断能否通过 `Game.unity` 独立 UI 节点、`ResourcesLocal` 独立 UI Prefab、Editor 菜单工具或运行时代码安全实现。
- 只有涉及已有核心 Scene、已有 Prefab、已有 ScriptableObject、StreamingAssets、存档结构、Photon 同步、AssetBundle 配置等破坏性修改时，才跳过。
- 不得因为需要新增公共枚举或公共常量就将定义散落在具体工具脚本中，应优先规划到 `Scripts/2D/Enum` 与 `Scripts/2D/Constant`。

---

## 7. 自动选择候选

只能从 `[TODO]` 中选择，且不得选择历史已 `[DONE]` 的候选。优先级与条件如下：

1. 优先 P0，其次 P1。
2. 必须低风险或中风险、边界清晰、可在一个任务卡内完成、不会直接破坏业务数据或 Unity 资源引用。
3. 优先 Agent 脚本、只读扫描器、Editor 工具、报告生成器、模板生成器、资源检查器、调试面板、可视化报告入口。
4. 候选涉及 UI、Scene、Prefab 时，先尝试低风险独立新增，不直接跳过。
5. 候选涉及已有 Scene、已有 Prefab、ScriptableObject、StreamingAssets、存档、Photon、AssetBundle 的直接修改时，跳过并标记 `[SKIPPED]`。
6. UI、Scene、Prefab 无法自动安全接入时，先实现底层工具逻辑、Editor 生成工具、运行时 UI 节点查找逻辑或报告输出逻辑，并在任务卡写明人工接入方式。
7. 如需新增公共函数、公共枚举或公共常量，必须优先规划到 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`。

---

## 8. Tool / Enum / Constant 优先规则

### 8.1 `Scripts/2D/Tool` 公共工具类

在新增或修改效率工具、Editor 工具、扫描器、报告生成器、调试面板或开发辅助 UI 前，必须优先扫描并复用 `Scripts/2D/Tool`。

- 可复用公共函数、通用辅助逻辑、路径处理、日志输出、文件读写、扫描结果格式化、UI 创建辅助、组件安全获取等公共能力不得散落在具体脚本中。
- 已有可复用方法必须优先调用，不重复实现。
- 本次功能需要新增通用能力时，优先在 `Scripts/2D/Tool` 新增或扩展工具类，再由具体扫描器、Editor 工具、报告生成器或 UI 面板调用。
- 具体工具脚本只保留强相关调度逻辑、菜单入口、扫描流程、报告生成流程、UI 绑定逻辑。
- 工具类保持低耦合，不得强依赖具体 Scene、Prefab、ScriptableObject、存档结构、Photon、AssetBundle 或单一业务模块。
- 命名遵循项目风格；无明确风格时建议：`XxxTool`、`XxxUtility`、`XxxHelper`、`XxxRuntimeTool`、`XxxReportTool`、`XxxPathTool`。
- 方法必须有中文注释，说明用途、参数、返回值、使用边界和风险限制。
- 原则上 `Scripts/2D/Tool` 不直接引用 `UnityEditor`；涉及 `UnityEditor` API 时，拆分为运行时通用工具 `Scripts/2D/Tool` 与 Editor 专用逻辑 `Scripts/2D/Editor`，避免正式构建报错。
- 推荐静态方法或低状态对象，避免全局副作用；修改已有工具类时保持兼容，不破坏已有方法签名和行为。

必须优先放入或复用 `Scripts/2D/Tool` 的逻辑包括：通用文件扫描、目录递归、扩展名过滤、路径拼接 / 规范化 / 相对路径转换、报告文件命名与输出路径生成、Markdown 文本 / 表格 / 摘要生成、JSON / YAML / 文本安全读取、日志输出、调试信息格式化、空引用保护、默认值处理、安全集合访问、GameObject / Component / Canvas / EventSystem 安全查找（FindChildComponent/GetComponentInChildren）、Resources / ResourcesLocal 路径辅助、UI Panel / Text / Button / Image / ScrollView 通用查找、调试面板通用刷新、多个效率工具可复用计算逻辑。

如果逻辑只服务本次功能但未来明显可复用，也优先设计为工具方法；若强依赖本次候选、具体报告结构、具体菜单入口或具体 UI 面板，则保留在本功能脚本中，不强行抽取。

### 8.2 `Scripts/2D/Enum` 公共枚举

在新增或修改效率工具、Editor 工具、扫描器、报告生成器、调试面板或开发辅助 UI 前，必须优先扫描并复用 `Scripts/2D/Enum`。

- 已有语义一致或可扩展复用枚举必须优先使用，不在具体工具、Editor、扫描器、报告或 UI 脚本中重复定义相似枚举。
- 可被多个工具、扫描器、报告生成器、Editor 工具、调试面板、UI 面板或 Agent 流程复用的枚举，必须优先放入 `Scripts/2D/Enum`。
- 只服务某个类极小内部状态且不外部复用的枚举，可作为类内部私有枚举，但必须在任务卡说明不抽取原因。
- 常见公共枚举包括但不限于：扫描结果类型、扫描范围类型、文件匹配类型、报告输出类型、报告排序方式、工具执行状态、检查项严重级别、检查结果状态、验证结果状态、UI 面板显示状态、调试面板类型、开发辅助 UI 类型、Editor 工具模式、资源检查类型、Markdown 报告区块类型、候选处理状态。
- 新增枚举命名清晰，如：`EfficiencyTaskStatus`、`ScanResultType`、`ReportOutputType`、`ToolExecutionState`、`CheckSeverityType`、`ResourceAuditType`、`DeveloperPanelType`、`ValidationResultType`。
- 枚举成员遵循项目风格；无明确风格时使用 PascalCase。
- 枚举文件必须包含中文注释，说明用途、每个枚举值含义、使用场景、是否允许扩展。
- 修改已有枚举必须兼容：不得删除、重命名已有枚举值，不得改变已有显式数值含义；必要扩展时只允许追加新值并说明原因。
- 如已有枚举命名或语义冲突，优先复用最贴近现有工具体系的枚举；无法安全判断时不强改旧枚举，可新增更明确枚举并在任务卡说明。

### 8.3 `Scripts/2D/Constant` 公共常量

在新增或修改效率工具、Editor 工具、扫描器、报告生成器、调试面板或开发辅助 UI 前，必须优先扫描并复用 `Scripts/2D/Constant`。

- 已有语义一致常量必须优先使用，不在具体工具、Editor、扫描器、报告或 UI 脚本中重复写魔法数字、魔法字符串、固定路径、默认文案、默认阈值、菜单路径或文件名。
- 可被多个效率工具、Editor 工具、扫描器、报告生成器、调试面板、UI 面板或 Agent 流程复用的常量，必须优先放入 `Scripts/2D/Constant`。
- 只服务类内部且不复用的常量，可保留为 `private const` 或 `private static readonly`，但必须在任务卡说明不抽取原因。
- 常见公共常量包括但不限于：`Agent/Reports/efficiency_discovery.md` 路径、`efficiency_` 任务目录前缀、任务卡 / 验证记录 / 报告文件名前缀、报告输出目录、Markdown 标题 / 表头、Editor 菜单路径、UI 默认文案、UI 节点 / 面板名称、Prefab 名称、Resources / ResourcesLocal 路径、扫描文件扩展名、忽略目录、日志前缀、默认扫描深度、默认报告数量限制、默认刷新间隔、默认颜色名或样式名、PlayerPrefs Key、工具开关 Key、错误提示文案、空结果提示文案。
- 常量类命名遵循项目风格；无明确风格时建议：`EfficiencyConstant`、`EfficiencyConstants`、`AgentReportConstant`、`EditorMenuConstant`、`ReportPathConstant`、`ScanConstant`、`ResourceAuditConstant`。
- 常量按业务语义分组，不要把无关常量全部塞入一个巨大类。
- 字段命名遵循项目风格；无明确风格时公共常量用 PascalCase，私有常量用 camelCase 或项目私有字段风格。
- 新增常量必须有中文注释，说明用途、使用场景、默认值含义、修改风险。
- 修改已有常量必须兼容：不得随意改变公共常量值、删除或重命名公共常量；如新增替代常量，应保留旧常量并说明兼容关系。
- 路径类常量应优先配合 `Scripts/2D/Tool` 的路径处理工具使用，避免硬编码路径拼接散落。

---

## 9. 公共代码分层

新增或修改代码时按以下优先级组织：

1. 公共枚举：放入 `Scripts/2D/Enum`，用于稳定工具状态、扫描结果类型、报告类型、检查严重级别、验证状态、UI 面板类型等。
2. 公共常量：放入 `Scripts/2D/Constant`，用于稳定字符串、路径、菜单路径、默认文件名、默认文案、默认阈值、Key、节点名、报告模板字段等。
3. 公共函数与辅助逻辑：放入 `Scripts/2D/Tool`，用于扫描、路径处理、文件读写、报告生成、格式化、日志、UI 节点查找、安全访问等。
4. 具体效率 / 工具脚本：放入 `Agent`、`Scripts/2D/Editor`、`Scripts/2D/UI` 或项目已有对应目录，只保留调度流程、菜单入口、扫描流程、报告生成流程、UI 绑定和功能入口。
5. Editor 专用逻辑：放入 Editor 专用目录，不得让运行时代码直接依赖 `UnityEditor`。

不得将公共枚举、公共常量、公共工具函数混写在单个工具脚本中。

---

## 10. UI / Scene / Prefab 生成规则

当效率 / 工具类功能涉及 UI，包括开发者调试面板、扫描结果展示面板、资源检查结果面板、TODO/FIXME 可视化报告面板、配置检查结果面板、运行时状态查看面板、Editor 工具入口对应运行时预览 UI、开发辅助按钮、提示框、报告弹窗、调试浮层等，按以下优先级实现。

### 10.1 优先：在 `Game.unity` 场景生成 UI

- 先搜索 `Game.unity` 真实路径，不凭空假定。常见路径包括 `Assets/Scenes/Game.unity`、`Assets/Game.unity`、`Assets/Resources/Scenes/Game.unity` 或项目实际路径。
- 若可安全修改 `Game.unity`，允许新增独立 UI 根节点，如 `Canvas`、`EventSystem`、`Efficiency_<候选ID>_<功能名>_Root` 及对应 Panel、Text、Button、Image、ScrollView、报告列表、调试按钮等。
- 已存在 Canvas 时优先在已有 Canvas 下新增独立子节点；无 Canvas 时可新增独立 Canvas，但不得影响已有摄像机、输入系统、渲染顺序和场景流程。
- 新增 UI 节点保持独立，不破坏已有 UI 层级、引用、脚本绑定和对象命名；对象名必须带 `Efficiency_`、候选 ID 或功能短名，便于定位和回滚。
- UI 节点名、默认文案、报告标题、按钮文案、Prefab 名称等固定值优先使用 `Scripts/2D/Constant`；UI 状态、面板类型、扫描结果类型、报告显示类型等优先使用 `Scripts/2D/Enum`。
- 如需挂脚本，只挂本次新增脚本或明确兼容的已有脚本。
- 不直接修改已有核心 UI 节点字段，除非低风险且必要，并在任务卡说明。
- 无法确认 Scene YAML 可安全修改时，不手写大段 YAML，改用 Editor 菜单工具或 Prefab 生成方式。

### 10.2 其次：在 `ResourcesLocal` 下创建 UI 预制体

无法安全修改 `Game.unity` 或项目更适合 Prefab 接入时，在 `ResourcesLocal` 创建独立 UI Prefab。优先路径：

- `Assets/ResourcesLocal/UI/<功能名安全短名>/`
- `Assets/ResourcesLocal/Prefabs/UI/<功能名安全短名>/`
- `Assets/ResourcesLocal/DeveloperTools/<功能名安全短名>/`
- `Assets/ResourcesLocal/Debug/<功能名安全短名>/`
- `Assets/ResourcesLocal/<已有项目约定目录>/<功能名安全短名>/`

要求：

- Prefab 命名示例：`Efficiency_<候选ID>_<功能名安全短名>Panel.prefab`、`Efficiency_<候选ID>_<功能名安全短名>ReportView.prefab`、`Efficiency_<候选ID>_<功能名安全短名>DebugWindow.prefab`。
- 遵循项目已有 `ResourcesLocal` 的 UI / Prefab 目录规范。
- 新增 Prefab 必须保留 `.meta`，不得覆盖已有 Prefab。
- Prefab 尽量包含完整 UI 层级、默认文案、基础样式、挂载脚本和必要组件。
- Prefab 路径、名称、默认文案、节点名称优先沉淀到 `Scripts/2D/Constant`；显示状态、扫描结果状态、报告类型、调试面板类型优先沉淀到 `Scripts/2D/Enum`。
- 运行时脚本放在合适路径，如 `Scripts/2D/UI/`、`Scripts/2D/Editor/`、`Scripts/2D/Tool/`、`Agent/`。
- 图片、字体、材质无法安全判断时，使用 Unity 默认 UI 组件或已有可安全引用资源。
- 无法生成真实 Prefab 文件时，生成 Editor 菜单工具，由开发者在 Unity 中点击菜单自动创建 Prefab。

### 10.3 再次：生成 Editor 菜单工具

无法安全修改 `Game.unity` 且不能可靠创建 Prefab 时，生成 Editor 菜单工具。菜单路径建议：

- `Tools/Agent/Efficiency/Create <功能名> UI In Game Scene`
- `Tools/Agent/Efficiency/Create <功能名> Prefab In ResourcesLocal`
- `Tools/Agent/Efficiency/Generate <功能名> Report`
- `Tools/Agent/Efficiency/Open <功能名> Panel`

要求：

- 放入 Editor 目录或项目已有 Editor 工具目录，菜单命名清晰。
- 菜单路径优先使用 `Scripts/2D/Constant`，执行状态、输出类型、扫描结果类型优先使用 `Scripts/2D/Enum`。
- 执行时检查 `Game.unity` 是否存在、目标目录是否存在，不存在则创建。
- 不覆盖已有 Prefab 或场景对象；重复执行时安全退出或生成带序号的新对象。
- 代码注释必须中文；生成的 UI 节点、Prefab、报告文件必须有清晰命名和回滚说明。

### 10.4 然后：运行时代码查找已有节点

Editor 工具也不可行时，可新增运行时 UI 管理器或可选挂载组件。

- **重要变更（2026-07）**：运行时代码**不得自动创建** UI 节点、Canvas、EventSystem。只能通过 `FindChildComponent`、`FindChildTransform`、`GetComponentInChildren` 等方法查找场景中已手动创建的节点。
- 节点不存在时，输出警告日志提示开发者手动创建，不得自动生成。
- 自动检查 Canvas 和 EventSystem 是否存在；缺失时输出警告，不自动创建。
- UI 必须独立命名，避免污染已有 UI。
- 必须提供启用 / 禁用入口，避免默认影响正式游戏流程。
- 开发辅助 UI 默认只在 Debug、Development Build 或明确开关开启时显示。
- 说明接入方式和风险边界。
- 动态 UI 的节点名称、默认文案、默认尺寸、显示开关 Key 优先使用 `Scripts/2D/Constant`；显示状态、面板类型、报告状态优先使用 `Scripts/2D/Enum`。

### 10.5 最后：退回纯代码或说明

仅在以下情况允许把 UI 无法生成部分退回到代码或说明：找不到 `Game.unity`；无法安全修改 `Game.unity`；无法确认 Unity Scene YAML / Prefab YAML 结构；当前环境无法运行 Unity Editor 且无法可靠生成 Prefab；项目缺少必要 UI 包或组件；资源路径、字体、图片、Canvas 结构无法确定；自动修改可能破坏已有资源引用。

退回代码实现时，必须在任务卡中写明：为什么没有直接生成到 `Game.unity`；为什么没有创建 `ResourcesLocal` 预制体；是否提供 Editor 菜单工具；是否提供运行时查找已有节点的代码；后续如何手动接入；需要挂载到哪个场景对象、Canvas 或工具入口下。

### 10.6 UI 优先级总结

1. 能安全修改 `Game.unity` 时，优先在 `Game.unity` 创建独立 UI 节点。
2. 不能安全改 `Game.unity` 时，优先在 `ResourcesLocal` 创建独立 UI Prefab。
3. 不能直接创建 Prefab 时，生成 Editor 菜单工具自动创建 UI 或 Prefab。
4. 以上都不可行时，使用运行时代码查找已有节点（不得自动创建），缺失时输出警告。
5. 最后才允许只写数据层、报告生成逻辑或人工接入说明。

选择第 3、4、5 种方式时，必须在任务卡和验证记录中写明原因。

---

## 11. 生成任务卡

任务卡路径：

- `<TASK_DIR>/task_efficiency_<候选ID>_<功能名安全短名>.md`

任务卡必须包含：

- 候选 ID、原始候选、当前状态、本次任务目录、全局候选报告路径 `Agent/Reports/efficiency_discovery.md`。
- 任务分类、负责 Agent、需要的 Skill、影响路径、不应触碰路径、风险等级、功能边界。
- UI 接入策略、Scene / Prefab / ResourcesLocal 生成策略。
- 执行步骤、验证步骤、回滚方案、结果区。
- 工具类复用策略：已检查的 `Scripts/2D/Tool` 工具类；计划复用的工具类和方法；计划新增或扩展的公共工具函数；哪些逻辑属于公共逻辑应放入 `Scripts/2D/Tool`；哪些逻辑属于具体工具流程应保留在扫描器、Editor 工具、报告生成器或 UI 脚本中。
- 枚举复用策略：已检查的 `Scripts/2D/Enum` 枚举；计划复用的枚举；计划新增或扩展的公共枚举；哪些状态、类型、结果、级别定义应放入 `Scripts/2D/Enum`。
- 常量复用策略：已检查的 `Scripts/2D/Constant` 常量类；计划复用的常量；计划新增或扩展的公共常量；哪些路径、菜单、文案、文件名、阈值、Key、日志前缀应放入 `Scripts/2D/Constant`。
- 是否涉及 `UnityEditor` API；若涉及，如何避免污染 `Scripts/2D/Tool` 运行时代码。
- 若没有使用 `Scripts/2D/Tool`、`Scripts/2D/Enum` 或 `Scripts/2D/Constant`，必须分别说明原因。

---

## 12. 实现规则

- 只修改和本任务直接相关的文件。
- 优先放在 `Agent`、`Scripts/2D/Editor`、`Scripts/2D/UI`、`Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant` 或其他低侵入路径。
- 公共函数、通用辅助方法、重复逻辑放入 `Scripts/2D/Tool`；公共枚举、工具状态类型、扫描结果类型、报告类型、检查级别、验证状态、UI 面板类型放入 `Scripts/2D/Enum`；公共常量、路径、菜单路径、默认文案、文件名、目录前缀、阈值、Key、日志前缀、节点名放入 `Scripts/2D/Constant`。
- 具体扫描器、报告生成器、Editor 菜单入口、调试面板绑定逻辑放入对应功能目录。
- UI 实现顺序：`Game.unity` 独立 UI 节点 → `ResourcesLocal` 独立 UI Prefab → Editor 菜单工具 → 运行时代码查找已有节点（不得自动创建）→ 纯报告 / 数据层 / 人工接入说明。
- 可新增独立脚本、Editor 工具、报告生成器、数据结构、调试输出、扫描器、模板文件或只读分析逻辑。
- 保持现有项目命名、目录结构和代码风格。
- 新增代码必须有清晰中文注释，说明用途、接入方式和风险边界。

---

## 13. Editor 专用逻辑要求

如需新增 Editor 专用公共逻辑：

- 必须避免运行时代码引用 `UnityEditor`。
- 运行时通用部分放入 `Scripts/2D/Tool`、`Scripts/2D/Enum`、`Scripts/2D/Constant`。
- Editor 菜单入口放入 Editor 专用目录。
- Editor 工具的菜单路径、输出路径、默认文件名优先使用 `Scripts/2D/Constant`。
- Editor 工具的生成类型、处理状态、扫描状态、严重级别优先使用 `Scripts/2D/Enum`。
- 推荐拆分：
  - `Scripts/2D/Tool`：运行时安全的路径处理、文本生成、数据格式化、UI 通用创建辅助。
  - `Scripts/2D/Enum`：运行时安全公共枚举。
  - `Scripts/2D/Constant`：运行时安全公共常量。
  - `Scripts/2D/Editor`：Editor 菜单入口、`AssetDatabase`、`PrefabUtility`、`EditorSceneManager` 等 Editor 专用逻辑。

---

## 14. 验证要求

完成后至少做静态检查或编译相关检查；不能运行 Unity 编译或 Play Mode 时，必须在任务卡写明未验证原因。

验证记录路径：

- `<TASK_DIR>/validation_efficiency_<候选ID>.md`

### 14.1 通用验证

- 新增 Editor 工具或报告工具：验证脚本路径、菜单路径、输出路径和基本扫描逻辑。
- 新增 UI 场景节点：验证 `Game.unity` 路径、对象命名、Canvas 层级、脚本挂载、回滚方式。
- 新增 UI Prefab：验证 Prefab 路径、`.meta` 文件、组件层级、脚本引用、ResourcesLocal 路径。
- 新增运行时 UI 管理器：验证默认禁用策略、空引用保护、Canvas 检查逻辑、调用边界。

### 14.2 `Scripts/2D/Tool` 验证

若新增或修改工具类，必须验证：路径正确；命名空间符合项目风格；未错误引用 `UnityEditor`；不影响运行时构建；不破坏已有调用方；公共函数具备空引用保护、异常保护或失败返回；中文注释完整。

若本次未使用 `Scripts/2D/Tool`，必须在验证记录说明原因。若存在重复逻辑但未抽取，也必须说明为什么暂不抽取。

### 14.3 `Scripts/2D/Enum` 验证

若新增或修改枚举，必须验证：路径正确；命名符合项目风格；语义清晰；不与已有枚举重复或冲突；未错误修改、删除、重命名已有枚举值；未改变已有显式枚举值含义；中文注释完整；工具脚本、Editor 脚本、报告脚本或 UI 脚本正确引用该枚举而不是重复定义。

若本次未使用 `Scripts/2D/Enum`，必须在验证记录说明原因。若存在重复枚举但未抽取，也必须说明为什么暂不抽取。

### 14.4 `Scripts/2D/Constant` 验证

若新增或修改常量，必须验证：路径正确；常量类命名符合项目风格；分组合理；不与已有常量重复或冲突；未错误修改、删除、重命名已有公共常量；未改变已有公共常量值导致兼容风险；中文注释完整；工具脚本、Editor 脚本、报告脚本或 UI 脚本正确引用常量，而不是继续硬编码魔法数字、魔法字符串、菜单路径、报告路径或文件名。

若本次未使用 `Scripts/2D/Constant`，必须在验证记录说明原因。若存在重复常量或魔法值但未抽取，也必须说明为什么暂不抽取。

### 14.5 Editor / Runtime 分离验证

若新增 Editor 工具调用了 `Scripts/2D/Tool`，必须验证 Editor 专用逻辑和运行时公共逻辑已经分离。

---

## 15. 更新任务卡结果区

在任务卡结果区写入：

- 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`。
- 已完成内容、修改文件、新增效率 / 工具能力、验证结果、验证记录路径、未完成项、剩余风险、后续建议。
- UI 生成位置：是否写入 `Game.unity`；是否创建 `ResourcesLocal` Prefab；是否改用 Editor 工具；是否改用运行时查找已有节点。
- `Scripts/2D/Tool` 情况：是否复用、复用了哪些工具类和方法、是否新增或扩展工具类、公共函数路径、用途说明、是否涉及 `UnityEditor` API、是否完成 Editor 专用逻辑与运行时工具逻辑拆分、是否存在未抽取重复逻辑。
- `Scripts/2D/Enum` 情况：是否复用、复用了哪些枚举、是否新增或扩展枚举、新增公共枚举路径、用途说明、是否存在未抽取重复枚举。
- `Scripts/2D/Constant` 情况：是否复用、复用了哪些常量类和字段、是否新增或扩展常量类、新增公共常量路径、用途说明、是否存在未抽取重复常量或魔法值。

---

## 16. 回写全局功能发现报告

打开 `Agent/Reports/efficiency_discovery.md`，找到本次候选 ID，将状态从 `[TODO]` 更新为：

- `[DONE]`：功能已实现且完成可行验证。
- `[PARTIAL]`：功能部分完成。
- `[BLOCKED]`：因环境、依赖或权限问题未能完成。

在“处理说明”补充：

- 本次任务目录路径、任务卡路径、验证记录路径。
- 修改文件、新增效率 / 工具能力摘要。
- UI 生成方式摘要、验证结果摘要。
- 是否复用了 `Scripts/2D/Tool`，新增或修改的公共工具类路径，新增公共函数摘要，具体功能脚本如何调用公共工具类，是否存在后续可继续抽取的公共逻辑。
- 是否复用了 `Scripts/2D/Enum`，新增或修改的公共枚举路径，新增公共枚举摘要，具体功能脚本如何引用公共枚举，是否存在后续可继续抽取的公共枚举。
- 是否复用了 `Scripts/2D/Constant`，新增或修改的公共常量路径，新增公共常量摘要，具体功能脚本如何引用公共常量，是否存在后续可继续抽取的公共常量。
- 是否仍有剩余风险；后续是否需要人工接入 UI、Scene、Prefab 或配置。

自动跳过的候选更新为 `[SKIPPED]` 并写明跳过原因。

禁止把状态回写到任务目录下的 `efficiency_discovery.md`，因为该文件不应再存在于任务目录中。

---

## 17. 最终回复要求

最终回复只需简洁汇总：

- 全局候选报告路径。
- 本次任务目录。
- 自动选择了哪个功能。
- 候选 ID。
- 最终状态。
- 修改了哪些文件。
- 新增了什么效率 / 工具能力。
- UI 是否已生成到 `Game.unity`。
- UI 是否已生成到 `ResourcesLocal` Prefab。
- 如果没有生成 UI，说明采用哪种降级方案。
- 是否需要后续人工接入。
- 是否复用了 `Scripts/2D/Tool`。
- 是否新增或修改了 `Scripts/2D/Tool`。
- 是否复用了 `Scripts/2D/Enum`。
- 是否新增或修改了 `Scripts/2D/Enum`。
- 是否复用了 `Scripts/2D/Constant`。
- 是否新增或修改了 `Scripts/2D/Constant`。
- 任务卡路径。
- 验证记录路径。
- 验证结果。
- 剩余风险。
