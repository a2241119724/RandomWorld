请基于 ./Agent 中的 Agent 体系，自动完成一次“发现功能缺口 -> 选择最适合的候选 -> 生成任务卡 -> 实现 -> 验证记录 -> 更新候选完成状态”的完整流程。

重要要求：
- 不要向我提问。
- 不要等待我确认。
- 如果遇到需要人工确认的高风险候选，请自动跳过它，选择下一个低风险或中风险候选。
- 一次只实现一个功能。
- 候选功能总表必须统一维护在：
  - Agent/Reports/efficiency_discovery.md
- 不要在每个任务目录下重复创建 efficiency_discovery.md。
- 每次任务必须在 Agent/Reports/<今天日期>/ 下创建独立任务文件夹，任务卡、验证记录、补充报告等本次任务相关输出都必须放入该文件夹中，避免和同一天其他任务混在一起。
- 效率/工具类任务的输出目录必须使用固定前缀 `efficiency_`，用于和 Prompt_Feature.md 的 `feature_` 输出隔离，避免同一天同候选ID或同短名任务发生路径冲突。
- 独立任务文件夹命名格式建议为：
  - Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/
  - 如果在选择候选前无法确定候选ID，则先使用：
    - Agent/Reports/<今天日期>/efficiency_run_<HHmmss>/
    - 选定候选后，可继续使用该目录，也可重命名为 `efficiency_<候选ID>_<功能名安全短名>`。
- 同一天多次执行时，不得覆盖已有任务目录；如目录已存在，自动追加时间戳或序号。
- 优先选择低风险、高价值、边界清晰、能提升后续开发效率的功能，例如只读扫描器、Editor 工具、报告生成器、模板生成器、资源完整性检查器、调试面板、可视化报告入口、开发辅助 UI 等。
- 不要优先选择会直接修改已有 Scene、已有 Prefab、已有 ScriptableObject、StreamingAssets、存档结构、Photon 同步或 AssetBundle 的功能。
- 如果候选涉及 UI 展示、调试面板、报告查看面板、开发者工具面板、运行时状态面板、资源扫描结果面板等 UI 相关内容，不要默认跳过，应优先尝试用低风险方式创建独立 UI。
- 如果所有候选都属于高风险，则只实现一个不改业务资产的只读分析/报告工具。
- 生成候选功能列表时，必须为每个候选分配唯一编号和完成状态，便于后续判断该候选是否已经处理完成。
- 完成功能实现与验证后，必须回写 Agent/Reports/efficiency_discovery.md 中对应候选的状态标记。
- 扫描历史记录时，必须递归检查 Agent/Reports/ 下所有日期目录及其子任务目录中的 task_*.md 和 validation_*.md。
- 同时必须读取 Agent/Reports/efficiency_discovery.md，避免重复实现已经 `[DONE]` 的候选。
- 如果历史任务目录中遗留存在旧版 efficiency_discovery.md，也需要兼容读取，但新的候选总表只允许写入 Agent/Reports/efficiency_discovery.md。
- **所有生成的代码注释必须使用中文。**

## UI / Scene / Prefab 生成规则

当效率/工具类功能涉及 UI 相关内容时，包括但不限于：
- 开发者调试面板
- 扫描结果展示面板
- 资源检查结果面板
- TODO/FIXME 可视化报告面板
- 配置检查结果面板
- 运行时状态查看面板
- Editor 工具入口对应的运行时预览 UI
- 开发辅助按钮、提示框、报告弹窗、调试浮层

必须优先按照以下顺序实现：

### 1. 优先在 Game.unity 场景中生成 UI

如果项目中存在 `Game.unity` 场景，应优先尝试在该场景中生成本功能需要的独立 UI 内容。

要求：
- 先在项目中搜索 `Game.unity` 的真实路径，不要凭空假定路径。
- 常见路径可能包括但不限于：
  - Assets/Scenes/Game.unity
  - Assets/Game.unity
  - Assets/Resources/Scenes/Game.unity
  - 其他项目实际使用路径
- 如果可以安全修改 `Game.unity`，允许新增独立 UI 根节点，例如：
  - Canvas
  - EventSystem
  - Efficiency_<候选ID>_<功能名>_Root
  - 对应的 Panel、Text、Button、Image、ScrollView、报告列表、调试按钮等 UI 节点
- 如果场景中已经存在 Canvas，应优先在已有 Canvas 下新增独立子节点。
- 如果场景中不存在 Canvas，可新增独立 Canvas，但必须避免影响已有摄像机、输入系统、渲染顺序和场景流程。
- 新增 UI 节点必须尽量独立，不得破坏已有 UI 层级、已有引用、已有脚本绑定和已有对象命名。
- 新增 UI 对象命名必须带有 `Efficiency_`、候选ID或功能短名，便于定位和回滚。
- 如果需要挂载脚本，应只挂载本次新增脚本或明确兼容的已有脚本。
- 不得直接修改已有核心 UI 节点字段，除非该修改是低风险且必要的，并且必须在任务卡中说明原因。
- 如果无法确认 YAML 场景文件可安全修改，不要直接手写大段 Scene YAML，应优先改用 Editor 菜单工具或 Prefab 生成方式。

### 2. 其次在 ResourcesLocal 下创建 UI 预制体

如果无法安全修改 `Game.unity`，或者项目更适合通过预制体接入 UI，则应在 `ResourcesLocal` 下创建对应位置的 UI 预制体。

优先路径建议：
- Assets/ResourcesLocal/UI/<功能名安全短名>/
- Assets/ResourcesLocal/Prefabs/UI/<功能名安全短名>/
- Assets/ResourcesLocal/DeveloperTools/<功能名安全短名>/
- Assets/ResourcesLocal/Debug/<功能名安全短名>/
- Assets/ResourcesLocal/<已有项目约定目录>/<功能名安全短名>/

要求：
- 创建独立 UI Prefab，例如：
  - Efficiency_<候选ID>_<功能名安全短名>Panel.prefab
  - Efficiency_<候选ID>_<功能名安全短名>ReportView.prefab
  - Efficiency_<候选ID>_<功能名安全短名>DebugWindow.prefab
- 如果项目已有 ResourcesLocal 的 UI/Prefab 目录规范，必须优先遵循已有规范。
- 新增 Prefab 必须保留 `.meta` 文件。
- 不得覆盖已有 Prefab。
- Prefab 应尽量包含完整 UI 层级、默认文案、基础样式、挂载脚本和必要组件。
- Prefab 需要的运行时脚本应放在合适路径，例如：
  - Scripts/2D/UI/
  - Scripts/2D/Editor/
  - Scripts/2D/Debug/
  - Scripts/2D/Tools/
  - Agent/
- 如果 Prefab 需要图片、字体、材质等引用，但无法安全判断项目资源，应使用 Unity 默认 UI 组件或已有可安全引用资源。
- 如果无法生成真实 Prefab 文件，应改为生成 Editor 菜单工具，由开发者在 Unity 中点击菜单后自动创建 Prefab。

### 3. 再次生成 Editor 菜单工具

如果不能直接安全修改 `Game.unity`，也不能可靠创建 Prefab，应优先生成 Editor 菜单工具。

菜单路径建议：
- Tools/Agent/Efficiency/Create <功能名> UI In Game Scene
- Tools/Agent/Efficiency/Create <功能名> Prefab In ResourcesLocal
- Tools/Agent/Efficiency/Generate <功能名> Report
- Tools/Agent/Efficiency/Open <功能名> Panel

Editor 工具要求：
- 放在 Editor 目录或项目已有 Editor 工具目录下。
- 菜单命名必须清晰。
- 工具执行时应检查 `Game.unity` 是否存在。
- 工具执行时应检查目标目录是否存在，不存在则自动创建。
- 工具不得覆盖已有 Prefab 或已有场景对象。
- 工具应尽量支持重复执行时安全退出或生成带序号的新对象。
- 工具代码注释必须使用中文。
- 工具生成的 UI 节点、Prefab、报告文件必须有清晰命名和回滚说明。

### 4. 然后使用运行时代码动态创建 UI

如果 Editor 工具也不可行，可新增运行时代码动态创建 UI。

要求：
- 新增运行时 UI 管理器或可选挂载组件。
- 运行时自动检查 Canvas 和 EventSystem。
- 如果不存在必要 UI 根节点，可动态创建独立节点。
- 动态创建的 UI 必须使用独立命名，避免污染已有 UI。
- 必须提供启用/禁用入口，避免默认影响正式游戏流程。
- 如果是开发辅助 UI，应默认只在 Debug、Development Build 或明确开关开启时显示。
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
- 后续如何手动接入。
- 需要挂载到哪个场景对象、Canvas 或工具入口下。

### UI 实现优先级总结

UI 相关效率/工具类功能的实现优先级必须是：

1. 能安全修改 `Game.unity` 时，优先在 `Game.unity` 中创建独立 UI 节点。
2. 不能安全改 `Game.unity` 时，优先在 `ResourcesLocal` 下创建独立 UI Prefab。
3. 不能直接创建 Prefab 时，生成 Editor 菜单工具，用于在 Unity 中自动创建 UI 或 Prefab。
4. 以上都不可行时，才用运行时代码动态创建 UI。
5. 最后才允许只写数据层、报告生成逻辑或人工接入说明。

如果选择了第 3、4、5 种方式，必须在任务卡和验证记录中写明原因。

## 候选状态规则

- `[TODO]`：待处理，尚未实现。
- `[DONE]`：已完成，已实现并完成可行验证。
- `[SKIPPED]`：已跳过，通常因为高风险、边界不清晰、需要人工确认或涉及敏感资源修改。
- `[BLOCKED]`：受阻，已分析但因缺少环境、依赖、权限或无法验证而未完成。
- `[PARTIAL]`：部分完成，已实现部分能力，但仍存在明确未完成项。

候选功能列表格式必须包含：

| 状态 | 候选ID | 功能名称 | 来源信号 | 价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|

示例：

| [TODO] | F001 | 资源引用完整性只读扫描器 | Resources/SO 中存在未校验引用 | 高 | 低 | 中 | P0 | ResourceAuditAgent | ReadOnlyScanSkill | 推荐优先实现 |
| [TODO] | F002 | 资源扫描结果调试面板 | 已有资源扫描结果但缺少可视化查看入口 | 中 | 低 | 中 | P1 | ToolUIAgent | EditorUISkill | 优先生成到 Game.unity；如不可行则创建 ResourcesLocal UI Prefab |
| [SKIPPED] | F003 | 自动修复 Prefab 缺失绑定 | Prefab 存在 Missing Reference | 高 | 高 | 高 | P1 | ResourceFixAgent | PrefabModifySkill | 涉及已有 Prefab 直接修改，跳过 |
| [DONE] | F004 | TODO/FIXME 报告生成器 | Scripts/2D 存在 TODO/FIXME | 中 | 低 | 低 | P1 | CodeAuditAgent | ReportGenerateSkill | 已完成；任务卡：Agent/Reports/2026-04-26/efficiency_F004_TODO_FIXME_Report/task_efficiency_F004_TODO_FIXME_Report.md；验证记录：Agent/Reports/2026-04-26/efficiency_F004_TODO_FIXME_Report/validation_efficiency_F004.md |

## 执行步骤

1. 读取并理解以下文件：
   - Agent/README.md
   - Agent/Docs/ImplementationRoadmap.md
   - Agent/Docs/SkillCatalog.md
   - Agent/Config/agent_registry.json
   - Agent/Config/task_router.json
   - Agent/Templates/agent_task_card.md

2. 读取全局候选功能发现报告：
   - Agent/Reports/efficiency_discovery.md

   如果该文件不存在，则自动创建。
   如果该文件已存在，则必须读取其中已有候选，尤其是 `[DONE]`、`[SKIPPED]`、`[BLOCKED]` 和 `[PARTIAL]` 状态，避免重复生成或重复实现同一功能。

3. 只读扫描项目上下文，重点检查：
   - README、Agent 文档、历史任务卡中的后续建议
   - `Game.unity` 的真实路径及其现有 UI 层级
   - `ResourcesLocal` 下已有 UI、Prefab、Panel、Popup、Debug、Tool、Report 等目录结构
   - Scripts/2D 中的 TODO、FIXME、空方法、临时实现、重复模式
   - Scripts/2D/Editor、Scripts/2D/UI、Scripts/2D/Debug、Scripts/2D/Tools 中已有工具、UI 和调试逻辑
   - Resources/SO、Resources/Tilemap、Resources/Images 中的资源绑定缺口
   - 已有系统中“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查”的半完成链路
   - 已有报告、扫描器、Editor 工具是否缺少可视化入口或统一输出路径
   - 存档、Photon、AssetBundle、资源引用等高风险区域的只读检查机会
   - Agent/Reports/efficiency_discovery.md 中已有的候选功能状态
   - Agent/Reports/ 下所有历史日期目录及其子任务目录中的 task_*.md 和 validation_*.md
   - 历史记录中已经标记为 `[DONE]` 的候选，避免重复实现同一功能
   - 历史任务目录中遗留的旧版 efficiency_discovery.md，仅作为兼容读取依据，不再作为新的写入目标

4. 生成或更新全局功能发现报告：
   - Agent/Reports/efficiency_discovery.md

   报告必须包含：
   - 全局候选功能列表
   - 扫描范围
   - 每个候选的唯一候选ID
   - 每个候选的状态标记：`[TODO]`、`[DONE]`、`[SKIPPED]`、`[BLOCKED]` 或 `[PARTIAL]`
   - 每个候选的来源信号、价值、风险、成本、优先级、推荐 Agent、推荐 Skill
   - 推荐优先开发的 1-3 个功能
   - 被跳过的高风险候选及原因
   - 已完成候选的任务卡路径、修改文件和验证记录摘要
   - 历史已完成候选的去重依据

   注意：
   - 不要覆盖已有候选状态。
   - 对已有 `[DONE]` 候选不得重新改为 `[TODO]`。
   - 如果发现相似候选，应合并到已有候选的处理说明中，不要重复分配新候选ID。
   - 新增候选时，应延续已有候选ID编号，例如已有 F001-F006，则新增从 F007 开始。
   - UI 类效率工具候选不得因为涉及 UI 就默认跳过，应先判断是否能通过 `Game.unity` 独立 UI 节点、`ResourcesLocal` 独立 UI Prefab、Editor 菜单工具或运行时代码安全实现。
   - 只有涉及已有核心 Scene、已有 Prefab、已有 ScriptableObject、StreamingAssets、存档结构、Photon 同步或 AssetBundle 配置的破坏性修改时，才应跳过。

5. 自动选择一个最适合立即开发的候选功能：
   - 只能从 `[TODO]` 状态的候选中选择。
   - 不得重复选择历史记录中已经 `[DONE]` 的候选。
   - 优先选择 P0。
   - 其次选择 P1。
   - 必须满足：低风险或中风险、边界清晰、可在一个任务卡内完成、不会直接破坏业务数据或 Unity 资源引用。
   - 优先选择能通过 Agent 脚本、只读扫描器、Editor 工具、报告生成器、模板生成器、资源检查器、调试面板、可视化报告入口完成的功能。
   - 如果候选涉及 UI、Scene 或 Prefab，应先尝试低风险独立新增，而不是直接跳过。
   - 如果候选涉及已有 Scene、已有 Prefab、ScriptableObject、StreamingAssets、存档、Photon 或 AssetBundle 的直接修改，则跳过，并将状态更新为 `[SKIPPED]`。
   - 如果 UI、Scene、Prefab 无法自动安全接入，则可以先实现底层工具逻辑、Editor 生成工具、运行时 UI 创建逻辑或报告输出逻辑，并在任务卡中明确后续人工接入方式。

6. 生成本次任务目录：
   - 首先确保日期目录存在：
     - Agent/Reports/<今天日期>/
   - 然后在该日期目录下创建本次任务的独立目录：
     - Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/
   - 如果候选ID尚未确定，则先创建：
     - Agent/Reports/<今天日期>/efficiency_run_<HHmmss>/
   - 选定候选后，如果当前目录仍是 efficiency_run_<HHmmss>，可继续使用该目录。
   - 如果需要更清晰区分任务，可将目录调整为：
     - Agent/Reports/<今天日期>/efficiency_<候选ID>_<功能名安全短名>/
   - 如果发生目录调整，必须保证任务卡、验证记录和后续补充文件都位于最终的 `<TASK_DIR>` 中。
   - 不允许把多个任务的输出混写到同一个任务目录中。
   - 不允许在 `<TASK_DIR>` 中创建新的 efficiency_discovery.md。
   - 以下路径统一用 `<TASK_DIR>` 表示本次任务目录。

7. 为选中的候选生成任务卡：
   - <TASK_DIR>/task_efficiency_<候选ID>_<功能名安全短名>.md

   任务卡必须包含：
   - 候选ID
   - 原始候选
   - 当前状态
   - 本次任务目录
   - 全局候选报告路径：Agent/Reports/efficiency_discovery.md
   - 任务分类
   - 负责 Agent
   - 需要的 Skill
   - 影响路径
   - 不应触碰路径
   - 风险等级
   - 功能边界
   - UI 接入策略
   - Scene / Prefab / ResourcesLocal 生成策略
   - 执行步骤
   - 验证步骤
   - 回滚方案
   - 结果区

8. 按任务卡实现该功能：
   - 只修改和该任务直接相关的文件。
   - 优先放在 Agent、Scripts/2D/Editor、Scripts/2D/UI、Scripts/2D/Debug、Scripts/2D/Tools 或其他低侵入路径。
   - 如果包含 UI，优先在 `Game.unity` 中新增独立 UI 节点。
   - 如果不能安全修改 `Game.unity`，优先在 `ResourcesLocal` 下创建独立 UI Prefab。
   - 如果不能创建 Prefab，优先创建 Editor 菜单工具用于生成 UI 或 Prefab。
   - 如果 Editor 工具也不可行，再使用运行时代码动态创建 UI。
   - 最后才退回为纯报告生成逻辑、数据层或人工接入说明。
   - 可以新增独立脚本、Editor 工具、报告生成器、数据结构、调试输出、扫描器、模板文件或只读分析逻辑。
   - 保持现有项目命名、目录结构和代码风格。
   - 不做无关重构。
   - 不修改用户已有的无关改动。
   - 不删除、不重命名、不覆盖已有 Unity 资源。
   - Unity 资源相关修改必须保留 `.meta`。
   - 如果无法安全处理 `.meta`，则不要直接修改该资源，改为生成 Editor 工具或运行时代码方案。
   - 新增代码应具备清晰的中文注释，说明用途、接入方式和风险边界。

9. 完成后运行可行的验证：
   - 至少做静态检查或编译相关检查。
   - 如果不能运行 Unity 编译或 Play Mode，要在任务卡中明确写出未验证原因。
   - 如果新增 Editor 工具或报告工具，要验证脚本路径、菜单路径、输出路径和基本扫描逻辑。
   - 如果新增 UI 场景节点，要验证 `Game.unity` 路径、对象命名、Canvas 层级、脚本挂载、回滚方式。
   - 如果新增 UI Prefab，要验证 Prefab 路径、`.meta` 文件、组件层级、脚本引用和 ResourcesLocal 路径。
   - 如果新增运行时 UI 管理器，要验证默认禁用策略、空引用保护、Canvas 检查逻辑和调用边界。
   - 验证记录必须写入：
     - <TASK_DIR>/validation_efficiency_<候选ID>.md

10. 更新任务卡结果区，写入：
   - 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`
   - 已完成内容
   - 修改的文件
   - 新增的效率/工具能力
   - UI 生成位置：
     - 是否已写入 `Game.unity`
     - 是否已创建 `ResourcesLocal` Prefab
     - 是否改用 Editor 工具
     - 是否改用运行时代码动态创建
   - 验证结果
   - 验证记录路径
   - 未完成项
   - 剩余风险
   - 后续建议

11. 回写全局功能发现报告：
   - 打开 Agent/Reports/efficiency_discovery.md。
   - 找到本次实现的候选ID。
   - 将该候选状态从 `[TODO]` 更新为：
     - `[DONE]`：功能已实现且完成可行验证
     - `[PARTIAL]`：功能部分完成
     - `[BLOCKED]`：因环境、依赖或权限问题未能完成
   - 在该候选的“处理说明”中补充：
     - 本次任务目录路径
     - 对应任务卡路径
     - 验证记录路径
     - 修改文件
     - 新增效率/工具能力摘要
     - UI 生成方式摘要
     - 验证结果摘要
     - 是否仍有剩余风险
     - 后续是否需要人工接入 UI、Scene、Prefab 或配置
   - 对自动跳过的候选，将状态更新为 `[SKIPPED]`，并写明跳过原因。
   - 不要把状态回写到任务目录下的 efficiency_discovery.md，因为该文件不应再存在于任务目录中。

12. 最终回复只需要简洁汇总：
   - 全局候选报告路径
   - 本次任务目录
   - 自动选择了哪个功能
   - 候选ID
   - 最终状态
   - 修改了哪些文件
   - 新增了什么效率/工具能力
   - UI 是否已生成到 Game.unity
   - UI 是否已生成到 ResourcesLocal Prefab
   - 如果没有生成 UI，说明采用了哪种降级方案
   - 是否需要后续人工接入
   - 任务卡路径
   - 验证记录路径
   - 验证结果
   - 剩余风险