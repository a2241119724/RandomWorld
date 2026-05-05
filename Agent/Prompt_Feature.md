请基于 ./Agent 中的 Agent 体系，自动完成一次“发现游戏业务功能缺口 -> 选择最适合的业务候选 -> 生成任务卡 -> 开发新功能 -> 验证记录 -> 更新候选完成状态”的完整流程。

本次任务目标：
- 自动为当前 Unity 游戏项目发现一个可落地的游戏业务相关新功能。
- 自动选择一个低风险或中风险、边界清晰、能在一次任务中完成的候选。
- 自动完成该游戏业务新功能的代码实现、基础验证、任务记录和候选状态回写。
- 一次只开发一个新功能，不做多个功能并行开发。
- 如果本次功能包含 UI 展示、UI 反馈、提示面板、结算面板、奖励弹窗、任务提示、交互提示、状态提示等内容，应尽量生成可直接接入 Unity 的 UI 资源，而不是只写纯代码说明。

重要要求：
- 不要向我提问。
- 不要等待我确认。
- 如果遇到需要人工确认的高风险候选，请自动跳过它，选择下一个低风险或中风险候选。
- 一次只实现一个游戏业务新功能。
- 候选功能总表必须统一维护在：
  - Agent/Reports/feature_discovery.md
- 不要在每个任务目录下重复创建 feature_discovery.md。
- 每次任务必须在 Agent/Reports/<今天日期>/ 下创建独立任务文件夹，任务卡、验证记录、补充报告等本次任务相关输出都必须放入该文件夹中，避免和同一天其他任务混在一起。
- 游戏业务功能任务的输出目录必须使用固定前缀 `feature_`，用于和 Prompt_Efficiency.md 的 `efficiency_` 输出隔离，避免同一天同候选ID或同短名任务发生路径冲突。
- 独立任务文件夹命名格式建议为：
  - Agent/Reports/<今天日期>/feature_<候选ID>_<功能名安全短名>/
  - 如果在选择候选前无法确定候选ID，则先使用：
    - Agent/Reports/<今天日期>/feature_run_<HHmmss>/
    - 选定候选后，可继续使用该目录，也可重命名为 `feature_<候选ID>_<功能名安全短名>`。
- 同一天多次执行时，不得覆盖已有任务目录；如目录已存在，自动追加时间戳或序号。
- 优先选择与游戏核心体验、玩家成长、关卡反馈、奖励反馈、交互体验、战斗/操作反馈、任务目标、成就统计、资源收集、引导提示等游戏业务相关的功能。
- 优先选择低风险、高价值、边界清晰、不破坏现有资源和存档结构的新功能。
- 不要优先选择纯工程类工具，例如只读扫描器、通用报告生成器、模板生成器、资源完整性检查器，除非当前没有任何安全可实现的游戏业务候选。
- 不要修改存档结构、Photon 同步逻辑、AssetBundle 配置、StreamingAssets 中的运行时资源结构。
- 不要删除、重命名或覆盖已有 Scene、Prefab、ScriptableObject、材质、图片、动画、音效或配置资源。
- **所有生成的代码注释必须使用中文。**

## UI / Scene / Prefab 生成规则

当候选功能包含 UI 相关内容时，必须优先按照以下顺序实现：

### 1. 优先在 Game.unity 场景中生成 UI

如果项目中存在 `Game.unity` 场景，应优先尝试在该场景中生成本功能需要的 UI 内容。

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
  - Feature_<候选ID>_<功能名>_Root
  - 对应的 Panel、Text、Button、Image、提示条、弹窗等 UI 节点
- 新增 UI 节点必须尽量独立，不得破坏已有 UI 层级、已有引用、已有脚本绑定和已有对象命名。
- 如果场景中已经存在 Canvas，应优先在已有 Canvas 下新增独立子节点。
- 如果场景中不存在 Canvas，可新增一个独立 Canvas，但必须避免影响已有摄像机、渲染、输入系统和场景流程。
- 新增 UI 对象的命名必须清晰，并带有候选ID或功能短名，便于回滚。
- 如果需要挂载脚本，应只挂载本次新增的脚本或明确兼容的已有脚本。
- 不得直接修改已有核心 UI 节点的字段，除非该修改是低风险且必要的，并且必须在任务卡中说明原因。
- 如果无法确认 YAML 场景文件可安全修改，不要直接手写大段 Scene YAML；应优先通过 Editor 菜单工具或 Prefab 生成方式实现。

### 2. 其次在 ResourcesLocal 下创建 UI 预制体

如果无法安全修改 `Game.unity`，或者项目更适合通过预制体接入 UI，则应在 `ResourcesLocal` 下创建对应位置的 UI 预制体。

优先路径建议：
- Assets/ResourcesLocal/UI/<功能名安全短名>/
- Assets/ResourcesLocal/Prefabs/UI/<功能名安全短名>/
- Assets/ResourcesLocal/<已有项目约定目录>/<功能名安全短名>/

要求：
- 创建独立 UI Prefab，例如：
  - Feature_<候选ID>_<功能名安全短名>Panel.prefab
  - Feature_<候选ID>_<功能名安全短名>Toast.prefab
  - Feature_<候选ID>_<功能名安全短名>Popup.prefab
- 如果项目已有 ResourcesLocal 的 UI/Prefab 目录规范，必须优先遵循已有规范。
- 新增 Prefab 必须保留 `.meta` 文件。
- 不得覆盖已有 Prefab。
- Prefab 应尽量包含完整的 UI 层级、默认文案、基础样式、挂载脚本和必要组件。
- Prefab 需要的运行时脚本应放在合适的 Scripts 路径下，例如：
  - Scripts/2D/UI/
  - Scripts/2D/Gameplay/
  - Scripts/2D/Feature/
- 如果 Prefab 需要图片、字体、材质等引用，但无法安全判断项目资源，应使用 Unity 默认 UI 组件或已有可安全引用资源。
- 如果无法生成真实 Prefab 文件，应改为生成 Editor 菜单工具，由开发者在 Unity 中点击菜单后自动创建 Prefab。

### 3. 最后才退回纯代码实现

只有在以下情况才允许把 UI 无法生成的部分退回到代码或说明中：
- 找不到 `Game.unity`。
- 无法安全修改 `Game.unity`。
- 无法确认 Unity Scene YAML / Prefab YAML 的结构。
- 当前环境无法运行 Unity Editor，也无法可靠生成 Prefab。
- 项目缺少必要 UI 包或组件。
- 资源路径、字体、图片、Canvas 结构无法确定。
- 自动修改可能破坏已有资源引用。

退回代码实现时，必须优先提供以下低侵入方案之一：
- 新增运行时 UI 管理器，在运行时自动创建 Canvas / Panel / Text / Button。
- 新增可选挂载组件，由开发者挂到 GameObject 后自动生成 UI。
- 新增 Editor 菜单工具，例如：
  - Tools/Game Features/Create <功能名> UI In Game Scene
  - Tools/Game Features/Create <功能名> Prefab In ResourcesLocal
- 新增 ViewModel / UI 数据源 / UI Binder，让后续 UI 接入更容易。
- 在任务卡中明确写出：
  - 为什么没有直接生成到 `Game.unity`
  - 为什么没有创建 `ResourcesLocal` 预制体
  - 后续如何手动或通过菜单工具接入
  - 需要挂载到哪个场景对象或 Canvas 下

### UI 实现优先级总结

UI 相关功能的实现优先级必须是：

1. 能安全修改 `Game.unity` 时，优先在 `Game.unity` 中创建独立 UI 节点。
2. 不能安全改 `Game.unity` 时，优先在 `ResourcesLocal` 下创建独立 UI Prefab。
3. 不能直接创建 Prefab 时，生成 Editor 菜单工具，用于在 Unity 中自动创建 UI 或 Prefab。
4. 以上都不可行时，才用运行时代码动态创建 UI。
5. 最后才允许只写数据层、ViewModel 或人工接入说明。

如果选择了第 3、4、5 种方式，必须在任务卡和验证记录中写明原因。

## 候选状态规则

- `[TODO]`：待处理，尚未实现。
- `[DONE]`：已完成，已实现并完成可行验证。
- `[SKIPPED]`：已跳过，通常因为高风险、边界不清晰、需要人工确认或涉及敏感资源修改。
- `[BLOCKED]`：受阻，已分析但因缺少环境、依赖、权限或无法验证而未完成。
- `[PARTIAL]`：部分完成，已实现部分能力，但仍存在明确未完成项。

候选功能列表格式必须包含：

| 状态 | 候选ID | 功能名称 | 业务类型 | 来源信号 | 玩家价值 | 开发价值 | 风险 | 成本 | 优先级 | 推荐 Agent | 推荐 Skill | 处理说明 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|

游戏业务候选重点方向：

1. 玩家体验类
   - 连击反馈
   - 受击反馈
   - 拾取反馈
   - 任务目标提示
   - 新手引导提示
   - 关卡完成反馈
   - 失败原因提示
   - 交互提示
   - 冷却提示
   - 状态变化提示

2. 成长与奖励类
   - 经验值统计
   - 局内积分统计
   - 简易成就条件
   - 奖励领取记录
   - 关卡星级计算
   - 战斗评分计算
   - 资源收集统计
   - 每日目标数据层
   - 任务完成条件判断

3. 关卡与玩法类
   - 关卡目标管理器
   - 波次进度统计
   - 敌人击杀计数
   - 存活时间统计
   - 玩家死亡原因记录
   - 关卡结果数据结构
   - 可配置玩法参数读取
   - 游戏状态流转辅助逻辑

4. UI 数据与表现类
   - 结算面板
   - 奖励弹窗
   - 任务目标提示面板
   - 玩家状态展示 UI
   - 战斗反馈浮字
   - 拾取提示条
   - 交互提示框
   - 冷却提示 UI
   - 新手引导提示框
   - UI 文案配置读取
   - 红点状态计算逻辑
   - 面板 ViewModel 与 Binder

5. 低风险业务辅助类
   - 业务事件总线
   - 游戏内事件统计器
   - 任务条件检查器
   - 奖励配置只读校验
   - 关卡配置只读校验
   - 玩家行为日志
   - Debug 面板数据输出
   - 运行时状态报告

## 执行步骤

1. 读取并理解以下文件：
   - Agent/README.md
   - Agent/Docs/ImplementationRoadmap.md
   - Agent/Docs/SkillCatalog.md
   - Agent/Config/agent_registry.json
   - Agent/Config/task_router.json
   - Agent/Templates/agent_task_card.md

2. 读取全局候选功能发现报告：
   - Agent/Reports/feature_discovery.md

   如果该文件不存在，则自动创建。
   如果该文件已存在，则必须读取其中已有候选，尤其是 `[DONE]`、`[SKIPPED]`、`[BLOCKED]` 和 `[PARTIAL]` 状态，避免重复生成或重复实现同一功能。

3. 只读扫描项目上下文，重点检查：
   - README、Agent 文档、历史任务卡中的后续建议
   - `Game.unity` 的真实路径及其现有 UI 层级
   - `ResourcesLocal` 下已有 UI、Prefab、Panel、Popup、Toast、HUD 等目录结构
   - Scripts/2D 中已有的游戏业务脚本、管理器、玩家控制、敌人逻辑、关卡逻辑、UI 逻辑、奖励逻辑、任务逻辑
   - Scripts/2D 中的 TODO、FIXME、空方法、临时实现、重复模式
   - 已有系统中“有数据但无 UI / 有 UI 但无行为 / 有行为但无验证 / 有资源但无检查 / 有事件但无反馈 / 有结果但无统计”的半完成业务链路
   - 玩家输入、战斗、关卡、资源、奖励、任务、成就、UI、音效、动画等模块中可低侵入补充的新功能机会
   - Resources/SO、Resources/Tilemap、Resources/Images 中的业务配置和资源使用信号，但只做只读分析，不直接修改敏感资源
   - 存档、Photon、AssetBundle、资源引用等高风险区域的只读检查机会
   - Agent/Reports/feature_discovery.md 中已有的候选功能状态
   - Agent/Reports/ 下所有历史日期目录及其子任务目录中的 task_*.md 和 validation_*.md
   - 历史记录中已经标记为 `[DONE]` 的候选，避免重复实现同一功能
   - 历史任务目录中遗留的旧版 feature_discovery.md，仅作为兼容读取依据，不再作为新的写入目标

4. 生成或更新全局功能发现报告：
   - Agent/Reports/feature_discovery.md

   报告必须包含：
   - 全局候选功能列表
   - 扫描范围
   - 每个候选的唯一候选ID
   - 每个候选的状态标记：`[TODO]`、`[DONE]`、`[SKIPPED]`、`[BLOCKED]` 或 `[PARTIAL]`
   - 每个候选的业务类型、来源信号、玩家价值、开发价值、风险、成本、优先级、推荐 Agent、推荐 Skill
   - 推荐优先开发的 1-3 个游戏业务功能
   - 被跳过的高风险候选及原因
   - 已完成候选的任务卡路径、修改文件和验证记录摘要
   - 历史已完成候选的去重依据

   注意：
   - 不要覆盖已有候选状态。
   - 对已有 `[DONE]` 候选不得重新改为 `[TODO]`。
   - 如果发现相似候选，应合并到已有候选的处理说明中，不要重复分配新候选ID。
   - 新增候选时，应延续已有候选ID编号，例如已有 F001-F006，则新增从 F007 开始。
   - 候选必须以游戏业务功能为主，不要把普通代码扫描器、文档生成器、模板工具作为首选候选。
   - UI 类候选不得因为涉及 UI 就默认跳过，应优先判断是否能通过 `Game.unity` 独立新增 UI、`ResourcesLocal` 预制体、Editor 菜单工具或运行时代码安全实现。
   - 只有当没有安全可实现的业务功能时，才允许选择业务辅助类或只读分析类功能。

5. 自动选择一个最适合立即开发的游戏业务候选功能：
   - 只能从 `[TODO]` 状态的候选中选择
   - 不得重复选择历史记录中已经 `[DONE]` 的候选
   - 优先选择 P0
   - 其次选择 P1
   - 必须满足：低风险或中风险、边界清晰、可在一个任务卡内完成、不会破坏业务数据或 Unity 资源引用
   - 优先选择能通过新增脚本、数据模型、管理器、事件监听器、ViewModel、Editor 菜单、Debug 输出、Game.unity 独立 UI 节点或 ResourcesLocal 独立 UI Prefab 完成的功能
   - 如果候选涉及存档结构、Photon 同步逻辑、AssetBundle 配置、StreamingAssets 运行时资源结构的直接修改，则跳过，并将状态更新为 `[SKIPPED]`
   - 如果候选涉及 UI、Scene 或 Prefab，应先尝试低风险实现，而不是直接跳过
   - 如果 UI、Scene、Prefab 无法自动安全接入，则可以先实现底层业务逻辑、运行时 UI 创建逻辑或 Editor 创建工具，并在任务卡中明确后续人工接入方式

6. 生成本次任务目录：
   - 首先确保日期目录存在：
     - Agent/Reports/<今天日期>/
   - 然后在该日期目录下创建本次任务的独立目录：
     - Agent/Reports/<今天日期>/feature_<候选ID>_<功能名安全短名>/
   - 如果候选ID尚未确定，则先创建：
     - Agent/Reports/<今天日期>/feature_run_<HHmmss>/
   - 不允许把多个任务的输出混写到同一个任务目录中。
   - 不允许在 `<TASK_DIR>` 中创建新的 feature_discovery.md。
   - 以下路径统一用 `<TASK_DIR>` 表示本次任务目录。

7. 为选中的候选生成任务卡：
   - <TASK_DIR>/task_feature_<候选ID>_<功能名安全短名>.md

   任务卡必须包含：
   - 候选ID
   - 原始候选
   - 当前状态
   - 本次任务目录
   - 全局候选报告路径：Agent/Reports/feature_discovery.md
   - 任务分类
   - 游戏业务类型
   - 玩家价值
   - 开发价值
   - 负责 Agent
   - 需要的 Skill
   - 影响路径
   - 不应触碰路径
   - 风险等级
   - 功能边界
   - 业务规则说明
   - 数据流说明
   - UI 接入策略
   - Scene / Prefab / ResourcesLocal 生成策略
   - 执行步骤
   - 验证步骤
   - 回滚方案
   - 结果区

8. 按任务卡实现该游戏业务新功能：
   - 只修改和该任务直接相关的文件
   - 优先放在 Scripts/2D、Scripts/2D/Gameplay、Scripts/2D/UI、Scripts/2D/Editor、Agent 或其他低侵入路径
   - 如果包含 UI，优先在 `Game.unity` 中新增独立 UI 节点
   - 如果不能安全修改 `Game.unity`，优先在 `ResourcesLocal` 下创建独立 UI Prefab
   - 如果不能创建 Prefab，优先创建 Editor 菜单工具用于生成 UI 或 Prefab
   - 如果 Editor 工具也不可行，再使用运行时代码动态创建 UI
   - 最后才退回为纯数据层、ViewModel 或人工接入说明
   - 可以新增独立脚本、业务管理器、数据结构、事件监听器、Editor 菜单、调试输出或只读报告
   - 保持现有项目命名、目录结构和代码风格
   - 不做无关重构
   - 不修改用户已有的无关改动
   - 不删除、不重命名、不覆盖已有 Unity 资源
   - Unity 资源相关修改必须保留 `.meta`
   - 如果无法安全处理 `.meta`，则不要直接修改该资源，改为生成 Editor 工具或运行时代码方案
   - 新增代码应具备清晰的中文注释，说明用途、接入方式和风险边界

9. 完成后运行可行的验证：
   - 至少做静态检查或编译相关检查
   - 如果不能运行 Unity 编译或 Play Mode，要在任务卡中明确写出未验证原因
   - 如果新增运行时业务脚本，要验证类名、命名空间、Unity API 使用、脚本路径和基础逻辑
   - 如果新增 UI 场景节点，要验证 `Game.unity` 路径、对象命名、Canvas 层级、脚本挂载、回滚方式
   - 如果新增 UI Prefab，要验证 Prefab 路径、`.meta` 文件、组件层级、脚本引用和 ResourcesLocal 路径
   - 如果新增 Editor 工具，要验证菜单路径、输出路径和基本生成逻辑
   - 如果新增数据模型或管理器，要验证默认值、空引用保护和调用边界
   - 验证记录必须写入：
     - <TASK_DIR>/validation_feature_<候选ID>.md

10. 更新任务卡结果区，写入：
   - 最终状态：`[DONE]`、`[PARTIAL]` 或 `[BLOCKED]`
   - 已完成内容
   - 修改的文件
   - 新增的游戏业务能力
   - 玩家侧效果
   - UI 生成位置：
     - 是否已写入 `Game.unity`
     - 是否已创建 `ResourcesLocal` Prefab
     - 是否改用 Editor 工具
     - 是否改用运行时代码动态创建
   - 开发侧接入方式
   - 验证结果
   - 验证记录路径
   - 未完成项
   - 剩余风险
   - 后续建议

11. 回写全局功能发现报告：
   - 打开 Agent/Reports/feature_discovery.md
   - 找到本次实现的候选ID
   - 将该候选状态从 `[TODO]` 更新为：
     - `[DONE]`：功能已实现且完成可行验证
     - `[PARTIAL]`：功能部分完成
     - `[BLOCKED]`：因环境、依赖或权限问题未能完成
   - 在该候选的“处理说明”中补充：
     - 本次任务目录路径
     - 对应任务卡路径
     - 验证记录路径
     - 修改文件
     - 新增业务能力摘要
     - UI 生成方式摘要
     - 验证结果摘要
     - 是否仍有剩余风险
     - 后续是否需要人工接入 UI、Scene、Prefab 或配置
   - 对自动跳过的候选，将状态更新为 `[SKIPPED]`，并写明跳过原因
   - 不要把状态回写到任务目录下的 feature_discovery.md，因为该文件不应再存在于任务目录中。

12. 最终回复只需要简洁汇总：
   - 全局候选报告路径
   - 本次任务目录
   - 自动选择了哪个游戏业务新功能
   - 候选ID
   - 游戏业务类型
   - 最终状态
   - 修改了哪些文件
   - 新增了什么业务能力
   - UI 是否已生成到 Game.unity
   - UI 是否已生成到 ResourcesLocal Prefab
   - 如果没有生成 UI，说明采用了哪种降级方案
   - 是否需要后续人工接入
   - 任务卡路径
   - 验证记录路径
   - 验证结果
   - 剩余风险